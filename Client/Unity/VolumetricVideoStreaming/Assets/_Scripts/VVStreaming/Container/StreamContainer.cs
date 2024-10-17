using System.Collections.Generic;
using UnityEngine;

public class VVFrame
{
    public bool isLoaded = false;
    public Mesh mesh;

    public VVFrame(bool isLoad, Mesh meshdata)
    {
        this.isLoaded = isLoad;
        this.mesh = meshdata;
    }
}

public class StreamContainer : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;
    public List<VVFrame> FrameContainer;

    public Vector3 MeshOffset { get; private set; }
    public bool isOffestReady {get; private set; } = false;

    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public void InitializeFrameContainer(int frameCount)
    {
        if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("Initializing Frame Container");

        FrameContainer = new List<VVFrame>();

        for (int i = 0; i < frameCount; i++)
        {
            FrameContainer.Add(new VVFrame(false, null));
        }

        MeshOffset = new Vector3(0, 0, 0);

        // container size
        Debug.Log("Frame Container Initialized with size: " + FrameContainer.Count);
    }

    public void LoadFrame(int index, Mesh mesh)
    {
        if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("Loading Frame: " + index);

        FrameContainer[index] = new VVFrame(true, mesh);

        if (!isOffestReady)
        {
            ApplyMeshOffset(mesh);

            isOffestReady = true;
        }
    }

    void ApplyMeshOffset(Mesh mesh)
    {
        if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("Applying Mesh Offset");

        float max = float.MaxValue;
        foreach (Vector3 vertex in mesh.vertices)
        {
            if (vertex.y < max)
            {
                max = vertex.y;
            }
        }

        MeshOffset = new Vector3(0, -max, 0);
    }
}
