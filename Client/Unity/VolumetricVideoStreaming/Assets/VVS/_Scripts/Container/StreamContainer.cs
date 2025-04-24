using System;
using System.Collections.Generic;
using UnityEngine;

public class VVFrame
{
    public bool isLoaded = false;
    public bool isCached = false;
    public string link;
    public string cachePath;
    public Mesh mesh;

    public VVFrame(string url)
    {
        isLoaded = false;
        isCached = false;
        this.link = url;
        mesh = null;
    }

    public void LoadMesh(Mesh meshdata)
    {
        mesh = meshdata;
        isLoaded = true;
    }

    public void UnloadMesh()
    {
        if (mesh != null)
        {
            mesh.Clear();
            mesh = null;
        }
        isLoaded = false;
    }

    public void CacheMesh(string cacheName)
    {
        cachePath = cacheName;
        isCached = true;
    }
}

public class StreamContainer : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;
    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public List<VVFrame> FrameContainer;

    public bool isDebuggingFrame = false;

    public void InitializeContainer(Action onComplete)
    {
        FrameContainer = new List<VVFrame>();

        for (int i = 0; i < streamManager.streamHandler.vvheader.count; i++)
        {
            FrameContainer.Add(new VVFrame($"{streamManager.LinkToFolder}/{streamManager.streamHandler.vvheader.meshes[i]}"));
        }

        streamManager.SendDebugText("Frame Container Initialized", this);

        onComplete?.Invoke();
    }

    public void LocalLoadFrame(int index, Mesh mesh)
    {
        if (isDebuggingFrame) streamManager.SendDebugText($"Loading Frame {index}", this);

        if (FrameContainer[index].mesh != null)
        {
            UnloadFrame(index);
        }

        FrameContainer[index].LoadMesh(mesh);

        if (!streamManager.streamPlayer.isOffsetApplied)
        {
            streamManager.streamPlayer.AppyOffset(mesh);
        }
    }

    public void CacheLoadFrame(int index, Mesh mesh, string cacheName)
    {
        if (isDebuggingFrame) streamManager.SendDebugText($"Loading Frame {index}", this);

        if (FrameContainer[index].mesh != null)
        {
            UnloadFrame(index);
        }

        FrameContainer[index].CacheMesh(cacheName);
        FrameContainer[index].LoadMesh(mesh);

        if (!streamManager.streamPlayer.isOffsetApplied)
        {
            streamManager.streamPlayer.AppyOffset(mesh);
        }
    }

    public void CacheFrame(int index, string cacheName)
    {
        if (isDebuggingFrame) streamManager.SendDebugText($"Caching Frame {index}", this);

        FrameContainer[index].CacheMesh(cacheName);
    }

    public void UnloadFrame(int index)
    {
        if (isDebuggingFrame) streamManager.SendDebugText($"Unloading Frame {index}", this);

        if (FrameContainer[index].mesh != null)
        {
            Destroy(FrameContainer[index].mesh);
            FrameContainer[index].UnloadMesh();
        }
    }

    public void UnloadFrames(int index, int count)
    {
        for (int i = index; i < index + count; i++)
        {
            if (i >= FrameContainer.Count) break;
            if (i < 0) continue;
            UnloadFrame(i);
        }
    }

    void OnDestroy()
    {
        for (int i = 0; i < FrameContainer.Count; i++)
        {
            UnloadFrame(i);

            // clean up cacheDirectory

            if (FrameContainer[i].isCached)
            {
                string cachePath = FrameContainer[i].cachePath;
                if (System.IO.File.Exists(cachePath))
                {
                    System.IO.File.Delete(cachePath);
                }
            }
            FrameContainer[i] = null;
        }
    }

}
