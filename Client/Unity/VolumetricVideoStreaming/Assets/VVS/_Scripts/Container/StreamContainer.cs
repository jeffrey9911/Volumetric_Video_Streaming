using System;
using System.Collections.Generic;
using UnityEngine;

public class VVFrame
{
    public bool isLoaded = false;
    public string link;
    public Mesh mesh;

    public VVFrame(bool isLoad, string url, Mesh meshdata)
    {
        this.isLoaded = isLoad;
        this.link = url;
        this.mesh = meshdata;
    }

    public void LoadMesh(Mesh meshdata)
    {
        mesh = meshdata;
        isLoaded = true;
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

    public void InitializeContainer(Action onComplete)
    {
        FrameContainer = new List<VVFrame>();

        for (int i = 0; i < streamManager.streamHandler.vvheader.count; i++)
        {
            FrameContainer.Add(new VVFrame(false, $"{streamManager.LinkToFolder}/{streamManager.streamHandler.vvheader.meshes[i]}", null));
        }

        streamManager.SendDebugText("Frame Container Initialized", this);

        onComplete?.Invoke();
    }

    public void LoadFrame(int index, Mesh mesh)
    {
        if (FrameContainer[index].mesh != null)
        {
            UnloadFrame(index);
        }

        mesh.name = "Frame_" + index;
        FrameContainer[index].LoadMesh(mesh);

        if (!streamManager.streamPlayer.isOffsetApplied) 
        {
            streamManager.streamPlayer.AppyOffset(mesh);
        }
    }

    public void UnloadFrame(int index)
    {
        if (FrameContainer[index].mesh != null)
        {
            Destroy(FrameContainer[index].mesh);
        }

        FrameContainer[index] = new VVFrame(false, FrameContainer[index].link, null);
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

}
