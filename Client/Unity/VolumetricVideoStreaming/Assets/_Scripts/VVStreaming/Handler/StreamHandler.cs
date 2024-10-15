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
    public bool isUsingConfig = false;
    public string DomainBaseLink = "";
    public string VVFolderLinkName = "";

    void Start()
    {
        if(isUsingConfig)
        {
            ReadConfig();
        }

        StartCoroutine(GetJsonData());
    }

    void ReadConfig()
    {
        string configPath = "Assets/config.json";
        if (System.IO.File.Exists(configPath))
        {
            StreamKey streamKey = JsonUtility.FromJson<StreamKey>(System.IO.File.ReadAllText(configPath));
            DomainBaseLink = streamKey.baseLink;
            VVFolderLinkName = streamKey.folderName;
        }
    }

    IEnumerator GetJsonData()
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
                // Parse the JSON data
                string jsonData = request.downloadHandler.text;
                VV data = JsonUtility.FromJson<VV>(jsonData);
                Debug.Log(data.name);
                Debug.Log(data.fps);
                Debug.Log(data.audio);
                Debug.Log(data.count);
                Debug.Log(data.texture);
                foreach (string meshLinkName in data.meshes)
                {
                    Debug.Log(meshLinkName);
                }
            }
        }
    }
}
