using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public StreamManager streamManager;
    public RawImage rawImage;

    public InputField BaseLinkInputField;
    public InputField FolderLinkInputField;

    public void UpdateLinks()
    {
        BaseLinkInputField.gameObject.SetActive(!BaseLinkInputField.gameObject.activeSelf);
        FolderLinkInputField.gameObject.SetActive(!FolderLinkInputField.gameObject.activeSelf);

        streamManager.OverideConfigLink = true;
        streamManager.OverideDomainBaseLink = BaseLinkInputField.text;
        streamManager.OverideVVFolderLinkName = FolderLinkInputField.text;
        streamManager.SendDebugText("Links Updated");
    }

    public void PlayOnClick()
    {
        streamManager.StreamingPlay();
    }
}
