using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class StreamDebuggerInspector
{
    public int TextureVideoFrame = 0;
    public int TargetFrame = 0;
    public int PlayFrame = 0;
}

public class StreamDebugger : MonoBehaviour
{
    public static StreamDebugger instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    public InputField inputField;

    public StreamDebuggerInspector Inspector;

    public void DebugText(string text)
    {
        inputField.text += "\n" + text ;
    }
}
