using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class StreamPlayer : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;
    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public GameObject PlayerInstance { get; private set; }
    private MeshFilter PlayerInstanceMesh;
    private MeshRenderer PlayerInstanceRenderer;
    private Material PlayerInstanceMaterial;
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


    public int BufferingThreshold = 1;
    public int ForwardBufferingTime = 2;
    public int BufferDroppingTime = 1;
    private bool isCheckingBuffer = false;
    private bool isDroppingBuffer = false;
    private bool isWaitingBuffer = false;
    private bool isNeedingBuffer = false;






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

        TexturePlayer.playOnAwake = false;
        TexturePlayer.isLooping = true;
        TexturePlayer.renderMode = VideoRenderMode.RenderTexture;
        TexturePlayer.targetTexture = TextureRenderer;
        TexturePlayer.url = $"{streamManager.LinkToFolder}/{streamManager.streamHandler.vvheader.texture}";

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

        PlayerInstance.transform.localPosition = MeshPositionOffset;
        PlayerInstance.transform.localRotation = Quaternion.Euler(MeshRotationOffset);
        PlayerInstance.transform.localScale = MeshScaleOffset;
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
        if (streamManager.streamContainer.FrameContainer[TargetFrame].isLoaded)
        {
            PlayFrame = TargetFrame;

            PlayerInstanceMesh.sharedMesh = null;
            PlayerInstanceMesh.sharedMesh = streamManager.streamContainer.FrameContainer[PlayFrame].mesh;
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
        //if (isDroppingBuffer) yield break;

        isCheckingBuffer = true;

        for (int i = 0; i < BufferingThreshold * streamManager.streamHandler.vvheader.fps; i++)
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
            for (int i = 0; i < ForwardBufferingTime * streamManager.streamHandler.vvheader.fps; i++)
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

        for (int i = 1; i < BufferDroppingTime * streamManager.streamHandler.vvheader.fps; i++)
        {
            int droppingFrame = PlayFrame - i;
            if (droppingFrame < 0)
            {
                droppingFrame = streamManager.streamContainer.FrameContainer.Count + droppingFrame;
            }

            if (streamManager.streamContainer.FrameContainer[droppingFrame].isLoaded)
            {
                streamManager.streamFrameHandler.DroppingFrameAt(droppingFrame);
            }

            yield return null;
        }


        isCheckingBuffer = false;
    }

    IEnumerator BufferingWait()
    {
        if (isWaitingBuffer) yield break;

        isWaitingBuffer = true;

        streamManager.SendDebugText("Buffering Wait", this);
        TexturePlayer.Pause();

        for (int i = 0; i < (int)(ForwardBufferingTime * streamManager.streamHandler.vvheader.fps); i++)
        {
            if ((PlayFrame + i) >= streamManager.streamContainer.FrameContainer.Count)
            {
                break;
            }

            if (!streamManager.streamContainer.FrameContainer[PlayFrame + i].isLoaded)
            {
                yield return new WaitForSeconds(1f / streamManager.streamHandler.vvheader.fps);
                i--;
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
        PlayerInstanceMesh.mesh = null;
    }

}
