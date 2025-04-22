using System;
using System.Collections;
using UnityEngine;

using UnityEngine.Networking;

[System.Serializable]
public class VV
{
    public string name;
    public float fps;
    public bool audio;
    public int count;
    public string texture;
    public string[] meshes;
}

public class StreamHandler : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;
    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }
    public bool isRunning { get; private set; } = false;
    public bool isReady { get; private set; } = false;

    public VV vvheader;

    Action onCompleteCallback;

    public void InitializeHandler(Action onComplete = null)
    {
        if (isRunning)
        {
            streamManager.SendDebugText("Already Running", this);
            return;
        }
        else if (isReady)
        {
            streamManager.SendDebugText("Already Ready", this);
            return;
        }

        isRunning = true;
        onCompleteCallback = onComplete;

        StartCoroutine(ReadHeader());
    }

    IEnumerator ReadHeader()
    {
        streamManager.SendDebugText("Header Loading", this);

        string jsonUrl = $"{streamManager.LinkToFolder}/manifest.json";

        using (UnityWebRequest request = UnityWebRequest.Get(jsonUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                streamManager.SendDebugText(request.error, this);
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                vvheader = JsonUtility.FromJson<VV>(jsonData);


                isReady = true;
                isRunning = false;
                streamManager.SendDebugText("Header Loaded", this);

                onCompleteCallback?.Invoke();
            }
        }
    }

}
