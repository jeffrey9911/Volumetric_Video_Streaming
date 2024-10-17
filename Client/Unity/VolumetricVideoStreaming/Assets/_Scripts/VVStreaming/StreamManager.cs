using Unity.VisualScripting;
using UnityEngine;

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

    public bool DisplayDebugText = false;
    public bool OverideConfigLink = false;
    public string OverideDomainBaseLink = "";
    public string OverideVVFolderLinkName = "";

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

        StreamDebugger.instance.DebugText("Stream Manager Initialized");
    }

    [ContextMenu("Start Load Header")]
    public void StartLoadHeader()
    {
        try
        {
            streamHandler.LoadHeader();
        }
        catch (System.Exception e)
        {
            StreamDebugger.instance.DebugText(e.Message);
            throw;
        }
        
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
            StreamDebugger.instance.DebugText(e.Message);
            throw;
        }
    }

    void OnApplicationQuit()
    {
        Destroy(streamHandler);
        Destroy(streamFrameHandler);
        Destroy(streamContainer);
        Destroy(streamPlayer);
    }
}
