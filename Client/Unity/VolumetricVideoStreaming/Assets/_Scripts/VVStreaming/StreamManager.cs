using UnityEngine;

public class StreamManager : MonoBehaviour
{
    [HideInInspector]
    public StreamHandler streamHandler;


    void Start()
    {
        streamHandler.streamManager = this;
    }
}
