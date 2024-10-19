using System.Collections.Generic;
using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using System.IO;

public class ProfileData : StandardData
{
    private string filePath;
    void Awake()
    {
        filePath = Application.persistentDataPath + "/profiles.json";
        //LoadData();
        // Debug.Log("1. Loading Profile data");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var data = ShellRun.ParseJsonToDictTable(json);
            if (data != null )
                SetRecords(data);
        }
    }
    public bool AddProfile(string name, string username, string path, string accessKey,string encryption_password)
    {
        //LoadData();
        if (string.IsNullOrEmpty(name))
            throw new System.Exception("Null is Name");
        if (string.IsNullOrEmpty(path))
            throw new System.Exception("Null is path");
        if (string.IsNullOrEmpty(username))
            throw new System.Exception("Null is username");
        if (string.IsNullOrEmpty(accessKey))
            throw new System.Exception("Null is accessKey");
        if (string.IsNullOrEmpty(encryption_password))
            throw new System.Exception("Null is encryption_password");
        return SetRecord(new DictStrStr
        {
            { "name", name },
            { "path", path },
            { "username", username },
            { "access_key", accessKey },
            { "encryption_password", encryption_password },
        });

    }

    public override void AfterSaveData()
    {
        // Debug.Log("FILE OP: Saving Data");
        string json = ShellRun.BuildJsonFromDictTable(GetRecords());
        //PrintRecordsToDebugLog();
        File.WriteAllText(filePath, json);
    }
    public override void BeforeLoadData()
    {
        // Any important checks to do before loading data

    }    
}

