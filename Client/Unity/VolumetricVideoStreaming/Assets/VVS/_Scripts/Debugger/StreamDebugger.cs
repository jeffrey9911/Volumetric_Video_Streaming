using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StreamRTDebuggerInspector
{
    public int TextureFrame = 0;
    public int TargetFrame = 0;
    public int PlayFrame = 0;
    public RawImage TextureVideoRender;
}

public class StreamDebugger : MonoBehaviour
{
    public StreamManager streamManager;
    public Text textDebug;
    public StreamRTDebuggerInspector Inspector;

    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public void DebugText(string text)
    {
        textDebug.text += $"[{DateTime.Now.ToString("HH:mm:ss")}] {text} \n";
    }


}
