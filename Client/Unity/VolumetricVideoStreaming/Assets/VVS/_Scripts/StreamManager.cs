using UnityEngine;

public class StreamManager : MonoBehaviour
{
    public class StreamerStatus
    {
        public bool isInitialized = false;
        public bool isPlayerReady = false;
        public bool isPreloaded = false;
        public bool isTexturePreviewReady = false;
    }


    [HideInInspector]
    public StreamHandler streamHandler;
    [HideInInspector]
    public StreamFrameHandler streamFrameHandler;
    [HideInInspector]
    public StreamContainer streamContainer;
    [HideInInspector]
    public StreamPlayer streamPlayer;
    [HideInInspector]
    public StreamDebugger streamDebugger;

    [Header("Streaming Settings")]
    public string LinkToFolder = "";
    //public bool EnableChunkedStreaming = false;
    public bool EnableStreamDebugger = false;
    

    public StreamerStatus streamerStatus = new StreamerStatus();

    [Header("Player Offset")]
    public Vector3 PositionOffset = Vector3.zero;
    public Vector3 RotationOffset = Vector3.zero;
    public Vector3 ScaleOffset = Vector3.one;
    bool isPlayOnLoad = true;

    void InitializeStreamer()
    {
        streamHandler = this.gameObject.AddComponent<StreamHandler>();
        streamFrameHandler = this.gameObject.AddComponent<StreamFrameHandler>();
        streamContainer = this.gameObject.AddComponent<StreamContainer>();
        streamPlayer = this.gameObject.AddComponent<StreamPlayer>();


        streamHandler.SetManager(this);
        streamFrameHandler.SetManager(this);
        streamContainer.SetManager(this);
        streamPlayer.SetManager(this);

        if (EnableStreamDebugger)
        {
            InitializeDebugger();
        }

        streamerStatus.isInitialized = true;
        SendDebugText("Stream Manager Initialized", this);
    }

    void InitializeDebugger()
    {
        if (TryGetComponent<StreamDebugger>(out streamDebugger))
        {
            streamDebugger.SetManager(this);
        }
        else
        {
            EnableStreamDebugger = false;
            Debug.LogAssertion("StreamDebugger Component not found");
        }
    }

    public void SetLink(string link)
    {
        LinkToFolder = link;
    }

    [ContextMenu("Play")]
    public void Play()
    {
        isPlayOnLoad = true;

        if (!streamerStatus.isInitialized) InitializeStreamer();

        if (!streamerStatus.isPlayerReady)
        {
            streamHandler.InitializeHandler(OnHeaderLoaded);
        }
        else
        {
            streamPlayer.Play();
        }
    }

    [ContextMenu("PreLoad Meshes")]
    public void PreLoadMeshes()
    {
        isPlayOnLoad = false;
        
        if (!streamerStatus.isInitialized) InitializeStreamer();

        if (!streamerStatus.isPlayerReady)
        {
            streamHandler.InitializeHandler(OnHeaderLoaded);
        }
    }

    [ContextMenu("Pause")]
    public void Pause()
    {
        streamPlayer.Pause();
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        streamPlayer.Stop();
    }

    void OnHeaderLoaded()
    {
        streamContainer.InitializeContainer(OnContainerReady);
    }

    void OnContainerReady()
    {
        streamFrameHandler.StartBuffering();
        streamPlayer.InitializePlayer(OnPlayerReady, PositionOffset, RotationOffset, ScaleOffset);
    }

    void OnPlayerReady()
    {
        streamerStatus.isPlayerReady = true;

        if (isPlayOnLoad) streamPlayer.Play();
        else streamFrameHandler.StartPreloadFrames();
    }


    public void SetPlayerBufferingTime(int threshold, int forward, int backward)
    {
        streamPlayer.BufferingThreshold = threshold;
        streamPlayer.ForwardBufferingTime = forward;
        streamPlayer.BufferDroppingTime = backward;
    }

    public void SendDebugText(string text, Object origin = null)
    {
        if (EnableStreamDebugger) streamDebugger.DebugText($"Origin: {origin.name}: {text}");
    }

    public void UpdateDebugTextureFrame(int frame)
    {
        if (EnableStreamDebugger) streamDebugger.Inspector.TextureFrame = frame;
    }

    public void UpdateDebugTargetFrame(int frame)
    {
        if (EnableStreamDebugger) streamDebugger.Inspector.TargetFrame = frame;
    }

    public void UpdateDebugPlayFrame(int frame)
    {
        if (EnableStreamDebugger) streamDebugger.Inspector.PlayFrame = frame;
    }

    public void SetDebugTexturePreview(Texture texture)
    {
        if (EnableStreamDebugger)
        {
            streamDebugger.Inspector.TextureVideoTexture = texture;
            streamerStatus.isTexturePreviewReady = true;
        }
    }


    void OnApplicationQuit()
    {
        Destroy(streamHandler);
        Destroy(streamFrameHandler);
        Destroy(streamContainer);
        Destroy(streamPlayer);
        Destroy(streamDebugger);
    }
}
