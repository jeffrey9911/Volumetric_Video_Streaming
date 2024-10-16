using System.Collections;
using UnityEngine;

using UnityEngine.Networking;

[System.Serializable]
public class VV
{
    public string name;
    public float fps;
    public bool audio;
    public int count;
    public string texture;
    public string[] meshes;
}

public class StreamKey
{
    public string baseLink;
    public string folderName;
}

public class StreamHandler : MonoBehaviour
{
    [HideInInspector]
    public StreamManager streamManager;
    public string DomainBaseLink { get; private set; } = "";
    public string VVFolderLinkName {get; private set; } = "";
    public VV vvheader;

    public void SetManager(StreamManager manager)
    {
        streamManager = manager;
    }

    public void LoadHeader()
    {
        if(!streamManager.OverideConfigLink)
        {
            ReadConfig();
        }
        else
        {
            SetConfig(streamManager.OverideDomainBaseLink, streamManager.OverideVVFolderLinkName);
        }

        StartCoroutine(ReadHeader());
    }

    void ReadConfig()
    {
        string configPath = "../config.json";
        if (System.IO.File.Exists(configPath))
        {
            StreamKey streamKey = JsonUtility.FromJson<StreamKey>(System.IO.File.ReadAllText(configPath));

            SetConfig(streamKey.baseLink, streamKey.folderName);
        }
    }

    public void SetConfig(string baseLink, string folderName)
    {
        DomainBaseLink = baseLink;
        VVFolderLinkName = folderName;
    }

    IEnumerator ReadHeader()
    {
        string jsonUrl = $"{DomainBaseLink}/{VVFolderLinkName}/manifest.json";
        using (UnityWebRequest request = UnityWebRequest.Get(jsonUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                vvheader = JsonUtility.FromJson<VV>(jsonData);
                streamManager.FinishLoadHeader();
            }
        }
    }

}
