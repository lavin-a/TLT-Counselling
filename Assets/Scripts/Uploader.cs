using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Uploader: MonoBehaviour
{
    public enum UploadStatus
    {
        notStarted,
        started,
        successful,
        error,
        completed
    }
    public UploadStatus uploadStatus;

    public void Start()
    {
        uploadStatus = UploadStatus.notStarted;
    }

    public void UploadFile(string path)
    {
        StartCoroutine(Upload(path));
    }

    IEnumerator Upload(string path)
    {
        uploadStatus = UploadStatus.started;
        Debug.Log(path + " " + uploadStatus);
        WWWForm form = new WWWForm();
        UnityWebRequest dataFile = UnityWebRequest.Get("file://" + path);
        yield return dataFile.SendWebRequest();
        form.AddBinaryData("dataFile", dataFile.downloadHandler.data, Path.GetFileName("file://" + path));
        using (UnityWebRequest req = UnityWebRequest.Post("https://unity.lavinamarnani.com/counselling/getFile.php", form))
        {
            uploadStatus = UploadStatus.completed;
            yield return req.SendWebRequest();
            Debug.Log("SERVER: " + req.downloadHandler.text); // server response
            if (req.result == UnityWebRequest.Result.ProtocolError || req.result == UnityWebRequest.Result.ConnectionError || !(req.downloadHandler.text.Contains("FILE OK")))
                uploadStatus = UploadStatus.error;
            else
                uploadStatus = UploadStatus.successful;
        }

        Debug.Log("Upload status: " + uploadStatus);

        yield break;
    }
}

