using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public StreamManager streamManager;
    public RawImage rawImage;

    public InputField Link;

    public void UpdateLinks()
    {
        streamManager.SetLink(Link.text);
        streamManager.SendDebugText("Links Updated", this);
    }

    public void PlayOnClick()
    {
        streamManager.Play();
    }
}
