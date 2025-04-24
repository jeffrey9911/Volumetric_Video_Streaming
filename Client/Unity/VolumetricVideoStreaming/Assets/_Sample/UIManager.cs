using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public StreamManager streamManager;
    public RawImage rawImage;

    bool isTracking = false;

    List<Image> trackingFrames = new List<Image>();

    public RectTransform Panel;
    public GameObject FramePrefab;

    void Start()
    {
        StartCoroutine(StartTracking());
    }

    void Update()
    {
        if (isTracking)
        {
            TrackFrame();
        }
    }

    public void PlayOnClick()
    {
        streamManager.Play();
    }

    public IEnumerator StartTracking()
    {
        while (!streamManager.streamerStatus.isPlayerReady)
        {
            yield return null;
        }

        foreach (var frame in streamManager.streamContainer.FrameContainer)
        {
            GameObject image = Instantiate(FramePrefab, Panel);
            trackingFrames.Add(image.GetComponent<Image>());
        }

        isTracking = true;
    }

    void TrackFrame()
    {
        for (int i = 0; i < streamManager.streamContainer.FrameContainer.Count; i++)
        {
            trackingFrames[i].color = Color.black;

            if (streamManager.streamContainer.FrameContainer[i].isCached)
            {
                trackingFrames[i].color = Color.white;
            }
            
            if (streamManager.streamContainer.FrameContainer[i].isLoaded)
            {
                trackingFrames[i].color = Color.green;
            }
        }

        trackingFrames[streamManager.streamDebugger.Inspector.TextureFrame].color = Color.yellow;
        trackingFrames[streamManager.streamDebugger.Inspector.TargetFrame].color = Color.blue;
        trackingFrames[streamManager.streamDebugger.Inspector.PlayFrame].color = Color.red;
    }


}
