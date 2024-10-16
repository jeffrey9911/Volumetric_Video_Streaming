using UnityEngine;
using UnityEngine.Video;

public class TEstPlayer : MonoBehaviour
{
    public StreamPlayer streamPlayer;

    void Update()
    {
        //Debug.Log(videoPlayer.frame);
    }

    [ContextMenu("TEST")]
    void TEST()
    {
        this.GetComponent<MeshRenderer>().material.mainTexture = streamPlayer.TextureRenderer;
    }
}
