using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StreamPlayer : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;

    public GameObject PlayerInstance { get; private set; }
    private MeshFilter PlayerInstanceMesh;
    private MeshRenderer PlayerInstanceRenderer;
    private Material PlayerInstanceMaterial;
    private VideoPlayer TexturePlayer;
    public RenderTexture TextureRenderer;

    public int TextureOffset = 0;
    public int BufferTime = 2;
    private int TargetFrame = 0;
    private int PlayFrame = 0;

    private bool isOffseted = false;

    private bool isPlayerReady = false;

    void Update()
    {
        if (isPlayerReady)
        {
            if (!isOffseted) CheckOffset();

            if (TexturePlayer.isPlaying) AVMSyncPlay();
        }
    }

    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public void InitializePlayer()
    {
        if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("Initializing Player");

        PlayerInstance = new GameObject("PlayerInstance");
        PlayerInstance.transform.SetParent(this.transform);

        PlayerInstanceMesh = PlayerInstance.AddComponent<MeshFilter>();
        PlayerInstanceRenderer = PlayerInstance.AddComponent<MeshRenderer>();

        TexturePlayer = PlayerInstance.AddComponent<VideoPlayer>();
        TextureRenderer = new RenderTexture(2048, 2048, 24);
        TextureRenderer.wrapMode = TextureWrapMode.Repeat;

        PlayerInstanceMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        PlayerInstanceMaterial.mainTexture = TextureRenderer;
        PlayerInstanceMaterial.SetTextureScale("_BaseMap", new Vector2(1, -1));
        PlayerInstanceMaterial.SetFloat("_Smoothness", 0);

        PlayerInstanceRenderer.material = PlayerInstanceMaterial;

        InitializeTexturePlayer();
    }

    private void InitializeTexturePlayer()
    {
        if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("Initializing Texture Player");

        TexturePlayer.playOnAwake = false;
        TexturePlayer.isLooping = true;
        TexturePlayer.renderMode = VideoRenderMode.RenderTexture;
        TexturePlayer.targetTexture = TextureRenderer;
        TexturePlayer.url = $"{streamManager.streamHandler.DomainBaseLink}/{streamManager.streamHandler.VVFolderLinkName}/{streamManager.streamHandler.vvheader.texture}";

        isPlayerReady = true;

        TexturePlayer.Play();

        // UI Display for testing
        UIManager.Instance.rawImage.texture = TextureRenderer;
    }

    void CheckOffset()
    {
        if (streamManager.streamContainer.isOffestReady)
        {
            PlayerInstance.transform.localPosition = streamManager.streamContainer.MeshOffset;
            isOffseted = true;

            Debug.Log("Player Offseted");
        }
    }

    void AVMSyncPlay()
    {
        //if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("AVM Sync Play");
        if (streamManager.DisplayDebugText) StreamDebugger.instance.Inspector.TextureVideoFrame = (int)TexturePlayer.frame;
        TargetFrame = (int)TexturePlayer.frame;

        if ((TargetFrame + TextureOffset) >= streamManager.streamContainer.FrameContainer.Count
            || (TargetFrame + TextureOffset) < 0)
        {
            return;
        }
        else
        {
            TargetFrame += TextureOffset;
        }

        if (streamManager.DisplayDebugText) StreamDebugger.instance.Inspector.TargetFrame = TargetFrame;


        if (PlayFrame != TargetFrame)
        {
            if (streamManager.streamContainer.FrameContainer[TargetFrame].isLoaded)
            {
                PlayFrame = TargetFrame;
                
                PlayerInstanceMesh.mesh = streamManager.streamContainer.FrameContainer[PlayFrame].mesh;
                if (streamManager.DisplayDebugText) StreamDebugger.instance.Inspector.PlayFrame = PlayFrame;
            }
            else
            {
                TexturePlayer.Pause();
                StartCoroutine(AVMSyncBuffer());
            }
        }
    }

    IEnumerator AVMSyncBuffer()
    {
        if (streamManager.DisplayDebugText) StreamDebugger.instance.DebugText("AVM Sync Buffer");

        Debug.Log("Start Buffering");
        for (int i = 0; i < (int)(BufferTime * streamManager.streamHandler.vvheader.fps); i++)
        {
            yield return null;

            if ((TargetFrame + i) >= streamManager.streamContainer.FrameContainer.Count)
            {
                break;
            }

            if (!streamManager.streamContainer.FrameContainer[TargetFrame + i].isLoaded)
            {
                i--;
                continue;
            }
        }

        Debug.Log("Buffering Done");
        TexturePlayer.Play();
    }
}
