using System.Collections;
using System.Collections.Generic;
using Draco;
using UnityEngine;
using UnityEngine.Networking;

public class StreamFrameHandler : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;

    public const int DownloadThreads = 10;
    private int activeThreads = 0;
    
    private Queue<string> downloadQueue = new Queue<string>();

    //private DracoMeshLoader draco = new DracoMeshLoader();


    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public void StartDownloadFrames()
    {
        StartCoroutine(QueueDownload());
    }

    IEnumerator QueueDownload()
    {
        if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("Queueing Downloads");

        foreach (var meshName in streamManager.streamHandler.vvheader.meshes)
        {
            downloadQueue.Enqueue($"{streamManager.streamHandler.DomainBaseLink}/{streamManager.streamHandler.VVFolderLinkName}/{meshName}");
        }

        yield return null;

        StartCoroutine(StartDownload());
    }

    IEnumerator StartDownload()
    {
        if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("Starting Downloads");

        int i = 0;
        while (downloadQueue.Count > 0)
        {
            while (activeThreads < DownloadThreads && downloadQueue.Count > 0)
            {
                string url = downloadQueue.Dequeue();
                
                int index = i;

                StartCoroutine(iHandleDownload(index, url));

                i++;
            }

            yield return null;
        }
    }

    IEnumerator iHandleDownload(int index, string url)
    {
        if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("Downloading Frame: " + index);

        activeThreads++;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                var dracoMesh = DracoDecoder.DecodeMesh(request.downloadHandler.data);
                //var dracoMesh = draco.ConvertDracoMeshToUnity(request.downloadHandler.data);

                while (!dracoMesh.IsCompleted) yield return null;

                streamManager.streamContainer.LoadFrame(index, dracoMesh.Result);
            }
        }
        
        activeThreads--;
    }
}
