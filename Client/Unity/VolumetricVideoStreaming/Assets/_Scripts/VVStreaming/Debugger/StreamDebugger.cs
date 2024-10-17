using UnityEngine;
using UnityEngine.UI;

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

    public void DebugText(string text)
    {
        inputField.text += "\n" + text ;
    }
}
