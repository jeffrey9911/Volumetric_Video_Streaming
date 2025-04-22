using System;
using System.Collections;
using System.Collections.Generic;
using Draco;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

public class StreamFrameHandler : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;
    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public const int DownloadThreads = 10;
    private int activeThreads = 0;

    private Queue<int> BufferingFrameQueue = new Queue<int>();
    private Queue<int> DroppingFrameQueue = new Queue<int>();
    private bool isBuffering = false;



    public void StartPreloadFrames()
    {
        if (streamManager.streamerStatus.isPreloaded)
        {
            streamManager.SendDebugText("Already Preloaded", this);
            return;
        }

        StartCoroutine(PreloadFrames());
    }

    IEnumerator PreloadFrames()
    {
        streamManager.SendDebugText("Preloading Downloads", this);

        for (int i = 0; i < streamManager.streamContainer.FrameContainer.Count; i++)
        {
            if (streamManager.streamContainer.FrameContainer[i].isLoaded) continue;

            BufferingFrameAt(i);

            yield return null;
        }

        while (!streamManager.streamerStatus.isPreloaded)
        {
            streamManager.streamerStatus.isPreloaded = true;
            foreach (var frame in streamManager.streamContainer.FrameContainer)
            {
                if (!frame.isLoaded)
                {
                    streamManager.streamerStatus.isPreloaded = false;
                    break;
                }


                yield return null;
            }

            if (streamManager.streamerStatus.isPreloaded)
            {
                streamManager.SendDebugText("Preload Complete", this);
                break;
            }

            yield return null;
        }
    }

    public void StartBuffering()
    {
        streamManager.SendDebugText("Starting Buffering", this);

        isBuffering = true;

        StartCoroutine(FrameBufferingDownload());
        StartCoroutine(FrameDroppingDownload());
    }

    public void StopBuffering()
    {
        streamManager.SendDebugText("Stopping Buffering", this);

        isBuffering = false;
    }

    IEnumerator FrameDroppingDownload()
    {
        while (isBuffering)
        {
            if (DroppingFrameQueue.Count > 0)
            {
                streamManager.streamContainer.UnloadFrame(DroppingFrameQueue.Dequeue());
            }

            yield return null;
        }
    }

    IEnumerator FrameBufferingDownload()
    {
        while (isBuffering)
        {
            while (activeThreads < DownloadThreads && BufferingFrameQueue.Count > 0)
            {
                StartCoroutine(iHandleDownload(BufferingFrameQueue.Dequeue()));
            }

            yield return null;
        }
    }

    public void DroppingFrameAt(int index)
    {
        if (!streamManager.streamContainer.FrameContainer[index].isLoaded) return;

        DroppingFrameQueue.Enqueue(index);
    }

    public void BufferingFrameAt(int index)
    {
        if (streamManager.streamContainer.FrameContainer[index].isLoaded) return;

        BufferingFrameQueue.Enqueue(index);
    }

    IEnumerator iHandleDownload(int index)
    {
        activeThreads++;

        using (UnityWebRequest request = UnityWebRequest.Get(streamManager.streamContainer.FrameContainer[index].link))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                streamManager.SendDebugText(request.error, this);
            }
            else
            {
                var dracoMesh = DracoDecoder.DecodeMesh(request.downloadHandler.data);
                //var dracoMesh = draco.ConvertDracoMeshToUnity(request.downloadHandler.data);

                while (!dracoMesh.IsCompleted) yield return null;

                // clean the downloaded data
                request.downloadHandler.Dispose();
                request.Dispose();

                streamManager.streamContainer.LoadFrame(index, dracoMesh.Result);
            }
        }

        activeThreads--;
    }
    
    void OnDestroy()
    {
        StopBuffering();
    }
}
