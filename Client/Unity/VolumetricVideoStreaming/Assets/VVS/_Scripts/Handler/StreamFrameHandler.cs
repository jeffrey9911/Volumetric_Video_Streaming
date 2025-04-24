using System;
using System.Collections;
using System.Collections.Generic;
using Draco;
using UnityEngine;
using UnityEngine.Networking;

public class StreamFrameHandler : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;
    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public int DownloadThreads = 60;
    private int activeThreads = 0;

    private Queue<int> BufferingFrameQueue = new Queue<int>();
    private Queue<int> DroppingFrameQueue = new Queue<int>();

    private Queue<int> CachingFrameQueue = new Queue<int>();

    private bool isBuffering = false;

    public void SetDownloadThreads(int threads)
    {
        if (threads < 1)
        {
            streamManager.SendDebugText("Download Threads must be greater than 0", this);
            return;
        }

        DownloadThreads = threads;
    }

    public void StartPreCacheFrames()
    {
        if (streamManager.streamerStatus.isPreCached)
        {
            streamManager.SendDebugText("Already Preloaded", this);
            return;
        }

        StartCoroutine(PreCacheFrames());
    }

    IEnumerator PreCacheFrames()
    {
        streamManager.SendDebugText("PreCaching Downloads", this);

        for (int i = 0; i < streamManager.streamContainer.FrameContainer.Count; i++)
        {
            if (streamManager.streamContainer.FrameContainer[i].isCached) continue;

            CachingFrameAt(i);

            yield return null;
        }

        while (!streamManager.streamerStatus.isPreCached)
        {
            streamManager.streamerStatus.isPreCached = true;
            foreach (var frame in streamManager.streamContainer.FrameContainer)
            {
                if (!frame.isCached)
                {
                    streamManager.streamerStatus.isPreCached = false;
                    break;
                }


                yield return null;
            }

            if (streamManager.streamerStatus.isPreCached)
            {
                streamManager.SendDebugText("PreCache Complete", this);
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
        StartCoroutine(FrameCachingDownload());
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
                StartCoroutine(iHandleLoad(BufferingFrameQueue.Dequeue()));
            }

            yield return null;
        }
    }

    IEnumerator FrameCachingDownload()
    {
        while (isBuffering)
        {
            while (activeThreads < DownloadThreads && CachingFrameQueue.Count > 0)
            {
                StartCoroutine(iHandleCache(CachingFrameQueue.Dequeue()));
            }

            yield return null;
        }
    }

    public void DroppingFrameAt(int index)
    {
        if (index < 0 || index >= streamManager.streamContainer.FrameContainer.Count) return;
        if (!streamManager.streamContainer.FrameContainer[index].isLoaded) return;

        DroppingFrameQueue.Enqueue(index);
    }

    public void BufferingFrameAt(int index)
    {
        if (index < 0 || index >= streamManager.streamContainer.FrameContainer.Count) return;
        if (streamManager.streamContainer.FrameContainer[index].isLoaded) return;

        BufferingFrameQueue.Enqueue(index);
    }

    public void CachingFrameAt(int index)
    {
        if (index < 0 || index >= streamManager.streamContainer.FrameContainer.Count) return;
        if (streamManager.streamContainer.FrameContainer[index].isLoaded) return;

        CachingFrameQueue.Enqueue(index);
    }

    IEnumerator iHandleLoad(int index)
    {
        activeThreads++;

        VVFrame frame = streamManager.streamContainer.FrameContainer[index];

        if (frame.isLoaded)
        {
            activeThreads--;
            yield break;
        }

        if (frame.isCached)
        {
            byte[] cachedData = System.IO.File.ReadAllBytes(frame.cachePath);

            var dracoMesh = DracoDecoder.DecodeMesh(cachedData);

            while (!dracoMesh.IsCompleted) yield return null;
            
            streamManager.streamContainer.LocalLoadFrame(index, dracoMesh.Result);
        }
        else
        {
            using (UnityWebRequest request = UnityWebRequest.Get(frame.link))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    streamManager.SendDebugText($"Frame {index}: {request.error}", this);

                    if (request.responseCode == 423)
                    {
                        BufferingFrameAt(index);
                    }
                }
                else
                {
                    string cacheDirec = $"{Application.temporaryCachePath}/VVCache/{streamManager.streamHandler.vvheader.name}/";

                    if (!System.IO.Directory.Exists(cacheDirec))
                    {
                        System.IO.Directory.CreateDirectory(cacheDirec);
                    }

                    cacheDirec += $"frame_{index}.drc";
                    System.IO.File.WriteAllBytes(cacheDirec, request.downloadHandler.data);

                    //var dracoMesh = draco.ConvertDracoMeshToUnity(request.downloadHandler.data);
                    var dracoMesh = DracoDecoder.DecodeMesh(request.downloadHandler.data);

                    while (!dracoMesh.IsCompleted) yield return null;

                    streamManager.streamContainer.CacheLoadFrame(index, dracoMesh.Result, cacheDirec);

                    request.downloadHandler.Dispose();
                    request.Dispose();
                }
            }
        }

        activeThreads--;
    }


    IEnumerator iHandleCache(int index)
    {
        activeThreads++;

        VVFrame frame = streamManager.streamContainer.FrameContainer[index];

        if (frame.isCached)
        {
            activeThreads--;
            yield break;
        }
        else
        {
            using (UnityWebRequest request = UnityWebRequest.Get(frame.link))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    streamManager.SendDebugText($"Frame {index}: {request.error}", this);

                    if (request.responseCode == 423)
                    {
                        CachingFrameAt(index);
                    }
                }
                else
                {
                    string cacheDirec = $"{Application.temporaryCachePath}/VVCache/{streamManager.streamHandler.vvheader.name}/";

                    if (!System.IO.Directory.Exists(cacheDirec))
                    {
                        System.IO.Directory.CreateDirectory(cacheDirec);
                    }

                    cacheDirec += $"frame_{index}.drc";
                    System.IO.File.WriteAllBytes(cacheDirec, request.downloadHandler.data);

                    streamManager.streamContainer.CacheFrame(index, cacheDirec);

                    request.downloadHandler.Dispose();
                    request.Dispose();
                }
            }
        }

        activeThreads--;
    }
    
    void OnDestroy()
    {
        StopBuffering();
    }
}
