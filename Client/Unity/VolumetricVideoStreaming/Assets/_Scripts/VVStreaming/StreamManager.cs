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
    }

    [ContextMenu("Start Load Header")]
    public void StartLoadHeader()
    {
        streamHandler.LoadHeader();
    }

    public void FinishLoadHeader()
    {
        //Debug.Log($"{streamHandler.DomainBaseLink}/{streamHandler.VVFolderLinkName}/{streamHandler.vvheader.texture}");
        // Start frame handler, download frame data
        streamContainer.InitializeFrameContainer(streamHandler.vvheader.count);
        streamFrameHandler.StartDownloadFrames();
        
        streamPlayer.InitializePlayer();
    }

    void OnApplicationQuit()
    {
        Destroy(streamHandler);
        Destroy(streamFrameHandler);
        Destroy(streamContainer);
        Destroy(streamPlayer);
    }
}
