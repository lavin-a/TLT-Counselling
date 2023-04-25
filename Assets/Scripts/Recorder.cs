using System;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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

                string filePath = Path.Combine(Application.persistentDataPath, fileName + " " + SystemInfo.deviceUniqueIdentifier + " " + DateTime.UtcNow.ToString("yyyy_MM_dd HH_mm_ss_ffff") + ".wav");

                // #if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX || UNITY_IOS || UNITY_ANDROID
                //     filePath = "file:///" + filePath;
                // #endif

                // Delete the file if it exists.
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                try
                {
                    WriteWAVFile(audioClip, filePath);
                    Debug.Log("File Saved Successfully at " + filePath);
                    var uploader = (Uploader)gameObject.GetComponent(typeof(Uploader));
                    if (uploader != null)
                    {
                        uploader.UploadFile(filePath);
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Debug.LogError("Persistent Data Path not found!");
                }

                isRecording = false;
                Microphone.End(Microphone.devices[0]);
                AudioClip.Destroy(audioClip, 10.0f);
                AudioClip.Destroy(audioSource.clip, 10.0f);
            }
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
    }

    #endregion
}