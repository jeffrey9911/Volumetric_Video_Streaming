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
    private AudioSource TextureAudio;
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
        streamManager.SendDebugText("Initializing Player");

        PlayerInstance = new GameObject("PlayerInstance");
        PlayerInstance.transform.SetParent(this.transform);

        PlayerInstanceMesh = PlayerInstance.AddComponent<MeshFilter>();
        PlayerInstanceRenderer = PlayerInstance.AddComponent<MeshRenderer>();

        TexturePlayer = PlayerInstance.AddComponent<VideoPlayer>();
        TextureRenderer = new RenderTexture(2048, 2048, 24);
        TextureRenderer.wrapMode = TextureWrapMode.Repeat;

        TextureAudio = PlayerInstance.AddComponent<AudioSource>();
        TextureAudio.playOnAwake = false;
        TextureAudio.spatialize = true;
        
        TexturePlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        TexturePlayer.SetTargetAudioSource(0, TextureAudio);

        PlayerInstanceMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        PlayerInstanceMaterial.mainTexture = TextureRenderer;
        PlayerInstanceMaterial.SetTextureScale("_BaseMap", new Vector2(1, -1));
        PlayerInstanceMaterial.SetFloat("_Smoothness", 0);

        PlayerInstanceRenderer.material = PlayerInstanceMaterial;

        InitializeTexturePlayer();
    }

    private void InitializeTexturePlayer()
    {
        streamManager.SendDebugText("Initializing Texture Player");

        TexturePlayer.playOnAwake = false;
        TexturePlayer.isLooping = true;
        TexturePlayer.renderMode = VideoRenderMode.RenderTexture;
        TexturePlayer.targetTexture = TextureRenderer;
        TexturePlayer.url = $"{streamManager.streamHandler.DomainBaseLink}/{streamManager.streamHandler.VVFolderLinkName}/{streamManager.streamHandler.vvheader.texture}";

        isPlayerReady = true;

        if (streamManager.PlayOnLoad) TexturePlayer.Play();

        streamManager.SetDebugTexturePreview(TextureRenderer);
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
        streamManager.UpdateDebugTextureFrame((int)TexturePlayer.frame);
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

        streamManager.UpdateDebugTargetFrame(TargetFrame);


        if (PlayFrame != TargetFrame)
        {
            if (streamManager.streamContainer.FrameContainer[TargetFrame].isLoaded)
            {
                PlayFrame = TargetFrame;
                
                PlayerInstanceMesh.mesh = streamManager.streamContainer.FrameContainer[PlayFrame].mesh;
                streamManager.UpdateDebugPlayFrame(PlayFrame);
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
        streamManager.SendDebugText("AVM Sync Start Buffering");

        for (int i = 0; i < (int)(BufferTime * streamManager.streamHandler.vvheader.fps); i++)
        {
            yield return null;

            if ((TargetFrame + i) >= streamManager.streamContainer.FrameContainer.Count)
            {
                break;
            }

            if (!streamManager.streamContainer.FrameContainer[TargetFrame + i].isLoaded)
            {
                if (TexturePlayer.isPlaying) TexturePlayer.Pause();
                i--;
                continue;
            }
        }

        streamManager.SendDebugText("AVM Sync End Buffering");
        TexturePlayer.Play();
    }

    public void Play()
    {
        TexturePlayer.Play();
    }

    public void Pause()
    {
        TexturePlayer.Pause();
    } 

    public void Stop()
    {
        TexturePlayer.Stop();
        PlayerInstanceMesh.mesh = null;
    }

}
