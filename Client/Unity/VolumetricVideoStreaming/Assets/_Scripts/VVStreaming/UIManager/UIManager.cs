using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public StreamManager streamManager;

    public RawImage rawImage;

    public void PlayOnClick()
    {
        streamManager.StartLoadHeader();
    }
}
