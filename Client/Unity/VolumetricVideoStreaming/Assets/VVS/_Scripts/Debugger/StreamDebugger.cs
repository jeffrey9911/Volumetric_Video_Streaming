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
    public Texture TextureVideoTexture;
}

public class StreamDebugger : MonoBehaviour
{
    public StreamManager streamManager;
    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public Text textDebug;
    public StreamRTDebuggerInspector Inspector;

    public void DebugText(string text)
    {
        string textToDisplay = $"[{DateTime.Now.ToString("HH:mm:ss")}] - [{text}]\n";
        Debug.Log(textToDisplay);
        if ( textDebug == null) return;
        textDebug.text += textToDisplay;
    }


}
