using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Draco;
using UnityEngine;
using UnityEngine.Networking;

public class StreamFrameHandler : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;

    public const int DownloadThreads = 60;
    private int activeThreads = 0;
    
    private Queue<string> downloadQueue = new Queue<string>();

    private DracoMeshLoader draco = new DracoMeshLoader();


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
        foreach (var meshName in streamManager.streamHandler.vvheader.meshes)
        {
            downloadQueue.Enqueue($"{streamManager.streamHandler.DomainBaseLink}/{streamManager.streamHandler.VVFolderLinkName}/{meshName}");
        }

        yield return null;

        StartCoroutine(StartDownload());
    }

    IEnumerator StartDownload()
    {
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
                var dracoMesh = draco.ConvertDracoMeshToUnity(request.downloadHandler.data);

                while (!dracoMesh.IsCompleted) yield return null;

                streamManager.streamContainer.LoadFrame(index, dracoMesh.Result);
            }
        }
        
        activeThreads--;
    }
}
