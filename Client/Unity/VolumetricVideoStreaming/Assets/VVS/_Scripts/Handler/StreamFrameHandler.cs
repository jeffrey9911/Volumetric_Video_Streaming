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
        streamManager.SendDebugText("Queueing Downloads");

        foreach (var meshName in streamManager.streamHandler.vvheader.meshes)
        {
            downloadQueue.Enqueue($"{streamManager.streamHandler.DomainBaseLink}/{streamManager.streamHandler.VVFolderLinkName}/{meshName}");
        }

        yield return null;

        StartCoroutine(StartDownload());
    }

    IEnumerator StartDownload()
    {
        streamManager.SendDebugText("Starting Downloads");

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
/*
        while (!streamManager.isAllMeshesLoaded)
        {
            foreach (var frame in streamManager.streamContainer.FrameContainer)
            {
                if (!frame.isLoaded)
                {
                    yield return null;
                    break;
                }

                streamManager.isAllMeshesLoaded = true;
                streamManager.SendDebugText("All Meshes Loaded");
            }

            yield return new WaitForSeconds(.5f);
        }
*/

        while (!streamManager.isAllMeshesLoaded)
        {
            streamManager.isAllMeshesLoaded = true;

            foreach (var frame in streamManager.streamContainer.FrameContainer)
            {
                if (!frame.isLoaded)
                {
                    yield return null;
                    streamManager.isAllMeshesLoaded = false;
                    break;
                }
            }

            yield return new WaitForSeconds(.5f);
        }

        streamManager.SendDebugText($"{streamManager.streamHandler.vvheader.name}: All Meshes Loaded");
    }

    IEnumerator iHandleDownload(int index, string url)
    {
        //streamManager.SendDebugText("Downloading Frame: " + index);

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
