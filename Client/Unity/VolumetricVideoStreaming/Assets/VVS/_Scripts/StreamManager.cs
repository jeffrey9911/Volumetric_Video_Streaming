using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StreamManager : MonoBehaviour
{
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

    public bool PlayOnLoad = false;
    public bool EnableStreamDebugger = false;
    public bool OverideConfigLink = false;
    public string OverideDomainBaseLink = "";
    public string OverideVVFolderLinkName = "";

    
    public bool isAllMeshesLoaded = false;

    void Start()
    {
        streamHandler = this.AddComponent<StreamHandler>();
        streamFrameHandler = this.AddComponent<StreamFrameHandler>();
        streamContainer = this.AddComponent<StreamContainer>();
        streamPlayer = this.AddComponent<StreamPlayer>();


        streamHandler.SetManager(this);
        streamFrameHandler.SetManager(this);
        streamContainer.SetManager(this);
        streamPlayer.SetManager(this);

        if (EnableStreamDebugger)
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

        SendDebugText("Stream Manager Initialized");
    }

    public void SetConfig(string baseLink, string folderName)
    {
        OverideConfigLink = true;
        OverideDomainBaseLink = baseLink;
        OverideVVFolderLinkName = folderName;
    }

    [ContextMenu("Streaming Play")]
    public void StreamingPlay()
    {
        try
        {
            if (!isAllMeshesLoaded)
            {
                streamHandler.LoadHeader();
            }
            else
            {
                streamPlayer.Play();
            }
        }
        catch (System.Exception e)
        {
            SendDebugText(e.Message);
            throw;
        }
    }

    [ContextMenu("PreLoad Meshes")]
    public void PreLoadMeshes()
    {
        PlayOnLoad = false;
        streamHandler.LoadHeader();
    }

    [ContextMenu("Manual Play")]
    public void ManualPlay()
    {
        streamPlayer.Play();
    }

    [ContextMenu("Manual Pause")]
    public void ManualPause()
    {
        streamPlayer.Pause();
    }

    [ContextMenu("Manual Stop")]
    public void ManualStop()
    {
        streamPlayer.Stop();
    }

    public void FinishLoadHeader()
    {
        try
        {
            streamContainer.InitializeFrameContainer(streamHandler.vvheader.count);
            streamFrameHandler.StartDownloadFrames();
            
            streamPlayer.InitializePlayer();
        }
        catch (System.Exception e)
        {
            SendDebugText(e.Message);
            throw;
        }
    }

    public void SendDebugText(string text)
    {
        if(EnableStreamDebugger) streamDebugger.DebugText(text);
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
        if (EnableStreamDebugger) streamDebugger.Inspector.TextureVideoRender.texture = texture;
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
