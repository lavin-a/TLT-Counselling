using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Facebook.WitAi;
using Facebook.WitAi.Lib;
using UnityGoogleDrive;
using UnityEditor;

#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace Oculus.Voice.Demo
{
    /// <summary>
    /// Add this component to a GameObject to Record Mic Input 
    /// </summary>
    [RequireComponent(typeof(AudioSource), typeof(EventTrigger))]
    public class Recorder : MonoBehaviour
    {
        #region Constants &  Static Variables
        [SerializeField] private AppVoiceExperience appVoiceExperience;
        [SerializeField] private Text textArea;

        /// <summary>
        /// Audio Source to store Microphone Input, An AudioSource Component is required by default
        /// </summary>
        static AudioSource audioSource;
        /// <summary>
        /// The samples are floats ranging from -1.0f to 1.0f, representing the data in the audio clip
        /// </summary>
        static float[] samplesData;
        /// <summary>
        /// WAV file header size
        /// </summary>
        const int HEADER_SIZE = 44;

        #endregion

        #region Private Variables

        /// <summary>
        /// Is Recording
        /// </summary>
        public static bool isRecording = false;
        /// <summary>
        /// Recording Time
        /// </summary>
        private float recordingTime = 0f;
        /// <summary>
        /// Recording Time Minute and Seconds
        /// </summary>
        private int minute = 0, second = 0;
        private int timeToRecord = 3599;
        private GoogleDriveFiles.CreateRequest request;
        private string result;
        StreamWriter writer;

        #endregion

        #region MonoBehaviour Callbacks

        void Start()
        {
            // Request iOS Microphone permission
            Application.RequestUserAuthorization(UserAuthorization.Microphone);

            // Check iOS Microphone permission
            if (Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Debug.Log("Microphone found");
            }
            else
            {
                Debug.Log("Microphone not found");
            }

            // Get the AudioSource component
            audioSource = GetComponent<AudioSource>();

            isRecording = false;

        }

        private void Update()
        {
           

            if (recordingTime >= timeToRecord)
            {
                SaveRecording();
            }

            if (isRecording)
            {
                recordingTime += Time.deltaTime;

                minute = (int)(recordingTime / 60);
                second = (int)(recordingTime % 60);

            }      
        }

        #endregion

        #region Other Functions


        IEnumerator ScaleOverTime(GameObject button, float scaleFactor)
        {
            Vector3 originalScale = button.transform.localScale;
            Vector3 destinationScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

            float currentTime = 0.0f;

            do
            {
                button.transform.localScale = Vector3.Lerp(originalScale, destinationScale, currentTime / 0.5f);
                currentTime += Time.deltaTime;
                yield return null;
            }
            while (currentTime <= 1f);
        }

        #endregion
        
        #region Recorder Functions

        public void StartRecording()
        {
            recordingTime = 0f;
            isRecording = true;

            Debug.Log("Recording Started");
            Microphone.End(Microphone.devices[0]);
            audioSource.clip = Microphone.Start(Microphone.devices[0], false, timeToRecord, 44100);
        }

        public void SaveRecording(string fileName = "Audio")
        {
            if (isRecording)
            {

                while (!(Microphone.GetPosition(null) > 0)) { }
                samplesData = new float[audioSource.clip.samples * audioSource.clip.channels];
                audioSource.clip.GetData(samplesData, 0);

                // Trim the silence at the end of the recording
                var samples = samplesData.ToList();
                int recordedSamples = (int)(samplesData.Length * (recordingTime / (float)timeToRecord));

                if (recordedSamples < samplesData.Length - 1)
                {
                    samples.RemoveRange(recordedSamples, samplesData.Length - recordedSamples);
                    samplesData = samples.ToArray();
                }

                // Create the audio file after removing the silence
                AudioClip audioClip = AudioClip.Create(fileName, samplesData.Length, audioSource.clip.channels, 44100, false);
                audioClip.SetData(samplesData, 0);

                // Assign Current Audio Clip to Audio Player

                string filePath = Path.Combine(Application.persistentDataPath, fileName + " " + DateTime.UtcNow.ToString("yyyy_MM_dd HH_mm_ss_ffff") + ".wav");

                // Delete the file if it exists.
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                try
                {
                    WriteWAVFile(audioClip, filePath);
                    Debug.Log("File Saved Successfully at " + filePath);
                    Upload(false, filePath);
                    writer.Close();
                }
                catch (DirectoryNotFoundException)
                {
                    Debug.LogError("Persistent Data Path not found!");
                }

                isRecording = false;
                Microphone.End(Microphone.devices[0]);
            }
        }
        
        private void Upload(bool toAppData, String filePath)
        {
            var content = File.ReadAllBytes(filePath);
            if (content == null) return;

            var file = new UnityGoogleDrive.Data.File() { Name = Path.GetFileName(filePath), Content = content };
            if (toAppData) file.Parents = new List<string> { "appDataFolder" };
            request = GoogleDriveFiles.Create(file);
            request.Fields = new List<string> { "id", "name", "size", "createdTime" };
            request.Send().OnDone += PrintResult;
        }

        private void PrintResult(UnityGoogleDrive.Data.File file)
        {
            result = string.Format("Name: {0} Size: {1:0.00}MB Created: {2:dd.MM.yyyy HH:MM:ss}\nID: {3}",
                    file.Name,
                    file.Size * .000001f,
                    file.CreatedTime,
                    file.Id);
            Debug.Log(result);
        }

        public static byte[] ConvertWAVtoByteArray(string filePath)
        {
            //Open the stream and read it back.
            byte[] bytes = new byte[audioSource.clip.samples + HEADER_SIZE];
            using (FileStream fs = File.OpenRead(filePath))
            {
                fs.Read(bytes, 0, bytes.Length);
            }
            return bytes;
        }

        // WAV file format from http://soundfile.sapp.org/doc/WaveFormat/
        static void WriteWAVFile(AudioClip clip, string filePath)
        {
            float[] clipData = new float[clip.samples];

            //Create the file.
            using (Stream fs = File.Create(filePath))
            {
                int frequency = clip.frequency;
                int numOfChannels = clip.channels;
                int samples = clip.samples;
                fs.Seek(0, SeekOrigin.Begin);

                //Header

                // Chunk ID
                byte[] riff = Encoding.ASCII.GetBytes("RIFF");
                fs.Write(riff, 0, 4);

                // ChunkSize
                byte[] chunkSize = BitConverter.GetBytes((HEADER_SIZE + clipData.Length) - 8);
                fs.Write(chunkSize, 0, 4);

                // Format
                byte[] wave = Encoding.ASCII.GetBytes("WAVE");
                fs.Write(wave, 0, 4);

                // Subchunk1ID
                byte[] fmt = Encoding.ASCII.GetBytes("fmt ");
                fs.Write(fmt, 0, 4);

                // Subchunk1Size
                byte[] subChunk1 = BitConverter.GetBytes(16);
                fs.Write(subChunk1, 0, 4);

                // AudioFormat
                byte[] audioFormat = BitConverter.GetBytes(1);
                fs.Write(audioFormat, 0, 2);

                // NumChannels
                byte[] numChannels = BitConverter.GetBytes(numOfChannels);
                fs.Write(numChannels, 0, 2);

                // SampleRate
                byte[] sampleRate = BitConverter.GetBytes(frequency);
                fs.Write(sampleRate, 0, 4);

                // ByteRate
                byte[] byteRate = BitConverter.GetBytes(frequency * numOfChannels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
                fs.Write(byteRate, 0, 4);

                // BlockAlign
                ushort blockAlign = (ushort)(numOfChannels * 2);
                fs.Write(BitConverter.GetBytes(blockAlign), 0, 2);

                // BitsPerSample
                ushort bps = 16;
                byte[] bitsPerSample = BitConverter.GetBytes(bps);
                fs.Write(bitsPerSample, 0, 2);

                // Subchunk2ID
                byte[] datastring = Encoding.ASCII.GetBytes("data");
                fs.Write(datastring, 0, 4);

                // Subchunk2Size
                byte[] subChunk2 = BitConverter.GetBytes(samples * numOfChannels * 2);
                fs.Write(subChunk2, 0, 4);

                // Data

                clip.GetData(clipData, 0);
                short[] intData = new short[clipData.Length];
                byte[] bytesData = new byte[clipData.Length * 2];

                int convertionFactor = 32767;

                for (int i = 0; i < clipData.Length; i++)
                {
                    intData[i] = (short)(clipData[i] * convertionFactor);
                    byte[] byteArr = new byte[2];
                    byteArr = BitConverter.GetBytes(intData[i]);
                    byteArr.CopyTo(bytesData, i * 2);
                }

                fs.Write(bytesData, 0, bytesData.Length);
            }
        }

        public bool IsActive => _active;
        private bool _active = false;

        // Add delegates
        private void OnEnable()
        {
            appVoiceExperience.events.OnRequestCreated.AddListener(OnRequestStarted);
           /* appVoiceExperience.events.OnPartialTranscription.AddListener(OnRequestTranscript);
            appVoiceExperience.events.OnFullTranscription.AddListener(OnRequestTranscript);
            appVoiceExperience.events.OnStartListening.AddListener(OnListenStart);
            appVoiceExperience.events.OnStoppedListening.AddListener(OnListenStop);
            appVoiceExperience.events.OnStoppedListeningDueToDeactivation.AddListener(OnListenForcedStop);*/
            appVoiceExperience.events.OnResponse.AddListener(OnRequestResponse);
          appVoiceExperience.events.OnError.AddListener(OnRequestError);
        }
        // Remove delegates
        private void OnDisable()
        {
            appVoiceExperience.events.OnRequestCreated.RemoveListener(OnRequestStarted);
 /*           appVoiceExperience.events.OnPartialTranscription.RemoveListener(OnRequestTranscript);
            appVoiceExperience.events.OnFullTranscription.RemoveListener(OnRequestTranscript);
            appVoiceExperience.events.OnStartListening.RemoveListener(OnListenStart);
            appVoiceExperience.events.OnStoppedListening.RemoveListener(OnListenStop);
            appVoiceExperience.events.OnStoppedListeningDueToDeactivation.RemoveListener(OnListenForcedStop);*/
            appVoiceExperience.events.OnResponse.RemoveListener(OnRequestResponse);
            appVoiceExperience.events.OnError.RemoveListener(OnRequestError);
        }

        // Request began
        private void OnRequestStarted(WitRequest r)
        {
            // Begin
            _active = true;
        }
        // Request response
        private void OnRequestResponse(WitResponseNode response)
        {
            string path = Path.Combine(Application.persistentDataPath, "transcript" + " " + DateTime.UtcNow.ToString("yyyy_MM_dd HH_mm_ss_ffff") + ".txt");
            writer = new StreamWriter(path, true);
            if (!string.IsNullOrEmpty(response["text"]))
                {
                    writer.WriteLine(response["text"]);
                 Debug.Log("Response: " + response["text"]);
            }
        }
        // Request error
        private void OnRequestError(string error, string message)
        {
            OnRequestComplete();
        }

        // Deactivate
        private void OnRequestComplete()
        {
            _active = false;
        }

        // Toggle activation
        public void ToggleActivation()
        {
            SetActivation(!_active);
        }
        // Set activation
        public void SetActivation(bool toActivated)
        {
            if (_active != toActivated)
            {
                _active = toActivated;
                if (_active)
                {
                    appVoiceExperience.Activate();
                }
                else
                {
                    appVoiceExperience.Deactivate();
                }
            }
        }
    }

    #endregion
}