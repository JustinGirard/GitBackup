using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using System.IO;
using System.Threading;

using System.Collections.Concurrent;

class FileUtils
{
    private static readonly ConcurrentDictionary<string, Mutex> _mutexes = new ConcurrentDictionary<string, Mutex>();

    private static Mutex GetMutex(string filePath)
    {
        return _mutexes.GetOrAdd(filePath, _ => new Mutex());
    }

    public static void LockReserve(string filePath)
    {
        GetMutex(filePath).WaitOne();
    }

    public static void LockRelease(string filePath)
    {
        GetMutex(filePath).ReleaseMutex();
    }

    public static object SafeReadJsonFile(string filePath)
    {
        LockReserve(filePath);
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return ShellRun.ParseJsonToDictTable(json);
            }
        }
        finally
        {
            LockRelease(filePath);
        }
        return null;
    }

    public static bool SafeWriteJsonFile(string filePath, DictObjTable records)
    {
        LockReserve(filePath);
        try
        {
            string json = ShellRun.BuildJsonFromDictTable(records);
            File.WriteAllText(filePath, json);
            return true;
        }
        finally
        {
            LockRelease(filePath);
        }
    }
}


public class ProfileData : StandardData
{
    private string filePath;
    void Awake()
    {
        filePath = Application.persistentDataPath + "/profiles.json";
        //LoadData();
        // Debug.Log("1. Loading Profile data");
        // TODO implement secure password storage
        //Dictionary<string,object> results = DJson.Parse((string)obj);
        var data = FileUtils.SafeReadJsonFile(filePath);
        if (data != null )
            SetRecords((DictObjTable)data);
    }
    public bool AddProfile(string name, 
                          string username, 
                          string path, 
                          string accessKey,
                          string encryption_password)
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
        return SetRecord(new DictStrObj
        {
            { "name", name },
            { "path", path },
            { "username", username },
            { "access_key", accessKey },
            { "encryption_password", encryption_password },
            { "python_install_dir", "Tools/Python/" },
            { "ipfs_install_dir", "Tools/IPFS/" },
            { "git_install_dir",  "Tools/Git/" },
            { "venv_path", "Tools/venv/" },
            { "decelium_wallet_url", "https://github.com/Decelium/decelium_wallet" },
            { "decelium_wallet_dir", "Tools/decelium_wallet/"  },
            { "propagator_url", "https://github.com/Decelium/propagator"  },
            { "propagator_dir", "Tools/propagator/"  }
            
        });

    }

    public override void AfterSaveData()
    {
        // Debug.Log("FILE OP: Saving Data");
        FileUtils.SafeWriteJsonFile(filePath,GetRecords());
        // Debug.Log("Saving User");
        // Debug.Log(json);

    }
    public override void BeforeLoadData()
    {
        // Any important checks to do before loading data

    }    
}

