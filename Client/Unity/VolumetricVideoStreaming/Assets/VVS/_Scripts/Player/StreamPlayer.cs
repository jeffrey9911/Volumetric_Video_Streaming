using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Video;

public class StreamPlayer : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;
    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public GameObject StreamPlayerManager;

    public GameObject PlayerInstanceA;
    public GameObject PlayerInstanceB;

    private MeshFilter PlayerInstanceMeshA;
    private MeshFilter PlayerInstanceMeshB;

    private VideoPlayer TexturePlayer;
    private AudioSource TextureAudio;
    public RenderTexture TextureRenderer;

    private int TextureOffset = 0;
    private Vector3 MeshPositionOffset;
    private Vector3 MeshRotationOffset;
    private Vector3 MeshScaleOffset;
    public bool isOffsetApplied = false;

    private int TargetFrame = 0;
    private int PlayFrame = 0;


    public float BufferingThreshold = 1;
    public float ForwardBufferingTime = 2;
    public float BufferDroppingTime = 1;
    private bool isCheckingBuffer = false;
    private bool isWaitingBuffer = false;
    private bool isNeedingBuffer = false;
    private bool isUsingA = false;


    void Update()
    {
        if (streamManager.streamerStatus.isPlayerReady)
        {
            if (TexturePlayer.isPlaying) SyncFrame();
        }
    }

    public void InitializePlayer(Action onComplete, Vector3 positionOffset, Vector3 rotationOffset, Vector3 scaleOffset)
    {
        streamManager.SendDebugText("Initializing Player", this);

        MeshPositionOffset = positionOffset;
        MeshRotationOffset = rotationOffset;
        MeshScaleOffset = scaleOffset;

        StreamPlayerManager = new GameObject("StreamPlayerManager");
        StreamPlayerManager.transform.SetParent(this.transform);
        StreamPlayerManager.transform.localPosition = Vector3.zero;


        TextureRenderer = new RenderTexture(2048, 2048, 24);
        TextureRenderer.wrapMode = TextureWrapMode.Repeat;

        TextureAudio = StreamPlayerManager.AddComponent<AudioSource>();
        TextureAudio.playOnAwake = false;
        TextureAudio.spatialize = true;

        TexturePlayer = StreamPlayerManager.AddComponent<VideoPlayer>();
        TexturePlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        TexturePlayer.SetTargetAudioSource(0, TextureAudio);

        TexturePlayer.playOnAwake = false;
        TexturePlayer.isLooping = true;
        TexturePlayer.renderMode = VideoRenderMode.RenderTexture;
        TexturePlayer.targetTexture = TextureRenderer;
        TexturePlayer.url = $"{streamManager.LinkToFolder}/{streamManager.streamHandler.vvheader.texture}";

        PlayerInstanceA = new GameObject("PlayerInstanceA");
        PlayerInstanceB = new GameObject("PlayerInstanceB");

        PlayerInstanceA.transform.SetParent(StreamPlayerManager.transform);
        PlayerInstanceB.transform.SetParent(StreamPlayerManager.transform);

        PlayerInstanceMeshA = PlayerInstanceA.AddComponent<MeshFilter>();
        PlayerInstanceMeshB = PlayerInstanceB.AddComponent<MeshFilter>();

        MeshRenderer MeshRendererA = PlayerInstanceA.AddComponent<MeshRenderer>();
        MeshRenderer MeshRendererB = PlayerInstanceB.AddComponent<MeshRenderer>();

        Material UniversalMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        UniversalMaterial.mainTexture = TextureRenderer;
        UniversalMaterial.SetTextureScale("_BaseMap", new Vector2(1, -1));
        UniversalMaterial.SetFloat("_Smoothness", 0);

        MeshRendererA.sharedMaterial = UniversalMaterial;
        MeshRendererB.sharedMaterial = UniversalMaterial;

        streamManager.SetDebugTexturePreview(TextureRenderer);

        streamManager.SendDebugText("Player Initialized", this);

        onComplete?.Invoke();
    }

    public void AppyOffset(Mesh mesh)
    {
        float max = float.MaxValue;
        foreach (Vector3 vertex in mesh.vertices)
        {
            if (vertex.y < max)
            {
                max = vertex.y;
            }
        }

        MeshPositionOffset += new Vector3(0, -max, 0);

        PlayerInstanceA.transform.localPosition = MeshPositionOffset;
        PlayerInstanceA.transform.localRotation = Quaternion.Euler(MeshRotationOffset);
        PlayerInstanceA.transform.localScale = MeshScaleOffset;

        PlayerInstanceB.transform.localPosition = MeshPositionOffset;
        PlayerInstanceB.transform.localRotation = Quaternion.Euler(MeshRotationOffset);
        PlayerInstanceB.transform.localScale = MeshScaleOffset;

        isOffsetApplied = true;
        streamManager.SendDebugText("Mesh Offset Applied", this);
    }


    void SyncFrame()
    {
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
            DisplayFrame();
        }
    }

    void DisplayFrame()
    {
        StartCheckBufferingChunk();

        int NextFrame = TargetFrame + 1;
        if (NextFrame >= streamManager.streamContainer.FrameContainer.Count)
        {
            NextFrame = NextFrame - streamManager.streamContainer.FrameContainer.Count;
        }
        if (streamManager.streamContainer.FrameContainer[TargetFrame].isLoaded
        && streamManager.streamContainer.FrameContainer[NextFrame].isLoaded)
        {
            streamManager.streamFrameHandler.DroppingFrameAt(PlayFrame);
            PlayFrame = TargetFrame;

            if (isUsingA)
            {
                PlayerInstanceA.SetActive(false);
                PlayerInstanceB.SetActive(true);

                PlayerInstanceMeshA.sharedMesh = null;
                PlayerInstanceMeshA.sharedMesh = streamManager.streamContainer.FrameContainer[NextFrame].mesh;


                isUsingA = false;
            }
            else
            {
                PlayerInstanceA.SetActive(true);
                PlayerInstanceB.SetActive(false);

                PlayerInstanceMeshB.sharedMesh = null;
                PlayerInstanceMeshB.sharedMesh = streamManager.streamContainer.FrameContainer[NextFrame].mesh;

                isUsingA = true;
            }

            streamManager.UpdateDebugPlayFrame(PlayFrame);
        }
        else
        {
            //if (TexturePlayer.isPlaying) TexturePlayer.Pause();
            if (!isWaitingBuffer) StartCoroutine(BufferingWait());
        }
    }

    void StartCheckBufferingChunk()
    {
        if (!isCheckingBuffer) StartCoroutine(CheckBufferingChunk());
    }

    IEnumerator CheckBufferingChunk()
    {

        if (isCheckingBuffer) yield break;

        isCheckingBuffer = true;


        int ThresholdIndex = (int)(BufferingThreshold * streamManager.streamHandler.vvheader.fps);
        int BufferingIndex = (int)(ForwardBufferingTime * streamManager.streamHandler.vvheader.fps);
        int DroppingAmount = (int)(BufferDroppingTime * streamManager.streamHandler.vvheader.fps);

        for (int i = 0; i < ThresholdIndex; i++)
        {
            int bufferingFrame = PlayFrame + i;
            if (bufferingFrame >= streamManager.streamContainer.FrameContainer.Count)
            {
                bufferingFrame = bufferingFrame - streamManager.streamContainer.FrameContainer.Count;
            }

            isNeedingBuffer = !streamManager.streamContainer.FrameContainer[bufferingFrame].isLoaded;

            yield return null;
        }

        if (isNeedingBuffer)
        {
            for (int i = 0; i < BufferingIndex; i++)
            {
                int bufferingFrame = PlayFrame + i;
                if (bufferingFrame >= streamManager.streamContainer.FrameContainer.Count)
                {
                    bufferingFrame = bufferingFrame - streamManager.streamContainer.FrameContainer.Count;
                }

                streamManager.streamFrameHandler.BufferingFrameAt(bufferingFrame);
            }

            isNeedingBuffer = false;
        }

        
/*
        for (int i = 0; i < DroppingAmount; i++)
        {
            int droppingFrame = PlayFrame - ThresholdIndex - i;

            if (droppingFrame < 0)
            {
                droppingFrame = streamManager.streamContainer.FrameContainer.Count + droppingFrame;
                if (droppingFrame <= BufferingIndex)
                {
                    continue;
                }
            }
            else
            {
                if (BufferingIndex >= streamManager.streamContainer.FrameContainer.Count)
                {
                    if (droppingFrame <= BufferingIndex - streamManager.streamContainer.FrameContainer.Count)
                    {
                        continue;
                    }
                }
                else
                {
                    if (droppingFrame <= BufferingIndex)
                    {
                        continue;
                    }
                }
            }

            if (streamManager.streamContainer.FrameContainer[droppingFrame].isLoaded)
            {
                streamManager.streamFrameHandler.DroppingFrameAt(droppingFrame);
            }

            yield return null;
        }
*/

        isCheckingBuffer = false;
    }


    
    IEnumerator BufferingWait()
    {
        if (isWaitingBuffer) yield break;

        isWaitingBuffer = true;

        streamManager.SendDebugText("Buffering Wait", this);
        TexturePlayer.Pause();

        float BufferingWaitTime = 0;

        for (int i = 0; i < (int)(ForwardBufferingTime * streamManager.streamHandler.vvheader.fps); i++)
        {
            int bufferingFrame = PlayFrame + i;
            if (bufferingFrame >= streamManager.streamContainer.FrameContainer.Count)
            {
                bufferingFrame = bufferingFrame - streamManager.streamContainer.FrameContainer.Count;
            }

            if (!streamManager.streamContainer.FrameContainer[PlayFrame + i].isLoaded)
            {
                yield return new WaitForSeconds(1f / streamManager.streamHandler.vvheader.fps);
                BufferingWaitTime += 1f / streamManager.streamHandler.vvheader.fps;
                i--;
            }

            if (BufferingWaitTime >= BufferingThreshold)
            {
                streamManager.SendDebugText("Buffering Wait Time Exceeded", this);
                StartCheckBufferingChunk();
                BufferingWaitTime = 0;
            }

            yield return null;
        }

        streamManager.SendDebugText("Buffering Wait Complete", this);
        TexturePlayer.Play();

        isWaitingBuffer = false;
    }

    [ContextMenu("Play")]
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
        PlayerInstanceMeshA.mesh = null;
    }
    
    void OnDestroy()
    {
        if (TextureRenderer != null)
        {
            TextureRenderer.Release();
            Destroy(TextureRenderer);
        }
    }
}
