using System.Collections.Generic;
using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using System.IO;

class RecordProfileReference
{
    public string name;
    public RecordProfileReference(DictStrStr record)
    {
        name = record["name"];
    }

    public DictStrStr ToDictRecord()
    {
        DictStrStr d = new DictStrStr { { "name", name } };
        return d;
    }

}

class RecordProfileFull
{
    public string name;
    public string path;
    public string username;
    public string accessKey;

    public RecordProfileFull(DictStrStr record)
    {
        name = record["name"];
    }

    public DictStrStr ToDictRecord()
    {
        DictStrStr d = new DictStrStr {
            { "name", name },
            { "path", path },
            { "username", username },
            { "access_key", accessKey },
        };
        return d;
    }

}

//using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
//using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
/*
public class ProfileData : RepoData
{
    // RECORDS IS INHERITED, and is a DictTable
    // Simulate adding a repository with details like name, URL, and branch
    public bool AddProfile(string name, string username, string path,string accessKey)
    {
        __records[name] = new DictStrStr
        {
            { "name", name },
            { "path", path },
            { "username", username },
            { "access_key", accessKey },
        };
        return true;
    }

    public bool DeleteRecord(string name)
    {
        if (__records.ContainsKey(name))
        {
            __records.Remove(name);

        }
        return true; // Repo already exists
    }
    public List<string> ListRecords()
    {
        return new List<string>(__records.Keys);
    }

    public bool LoadData()
    {
        //CREATE OR INIT THE JSON FILE
        // LOAD RECORDS INOT __records
    }

}
*/


public class ProfileData : RepoData
{
    private string filePath;

    // Use Awake or Start to initialize filePath after Unity is fully initialized
    void Awake()
    {
        filePath = Application.persistentDataPath + "/profiles.json";
    }
    // RECORDS IS INHERITED, and is a DictTable
    public bool AddProfile(string name, string username, string path, string accessKey)
    {
        LoadData();
        if (string.IsNullOrEmpty(name))
            throw new System.Exception("Null is Name");
        if (string.IsNullOrEmpty(path))
            throw new System.Exception("Null is path");
        if (string.IsNullOrEmpty(username))
            throw new System.Exception("Null is username");
        if (string.IsNullOrEmpty(accessKey))
            throw new System.Exception("Null is accessKey");


        __records[name] = new DictStrStr
        {
            { "name", name },
            { "path", path },
            { "username", username },
            { "access_key", accessKey },
        };

        // Save the updated data to the file
        SaveData();

        return true;
    }

    public override bool DeleteRecord(string name)
    {
        LoadData();
        if (__records.ContainsKey(name))
        {
            __records.Remove(name);

            // Save the updated data to the file
            SaveData();
        }
        SaveData();
        return true;
    }

    public override List<string> ListRecords()
    {
        LoadData();
        return new List<string>(__records.Keys);
    }

    // Save __records to the JSON file
    private void PrintRecordsToDebugLog()
    {
        foreach (var outerEntry in __records)
        {
            string key = outerEntry.Key;
            DictStrStr innerDict = outerEntry.Value;

            Debug.Log($"Profile Name: {key}");

            foreach (var innerEntry in innerDict)
            {
                Debug.Log($"{innerEntry.Key}: {innerEntry.Value}");
            }

            Debug.Log("------------------------");
        }
    }


    private void SaveData()
    {
        // Convert the __records dictionary to a JSON string
        //string json = JsonUtility.ToJson(new Serialization<DictTable>(__records), true);
        string json = ShellRun.BuildJsonFromDictTable(__records);
        // Write the JSON string to the file
        Debug.Log("SAVING TO "+filePath); // WHY IS THIS NULL <-----
        PrintRecordsToDebugLog();
        File.WriteAllText(filePath, json);
        Debug.Log("SAVED: "+ json);
        File.WriteAllText(filePath+".txt", "TEST");
    }

    public void InitializeBackupDirectory(string targetDirectory, string backupFileName)
    {
        // Step (a): Recursively create the directory if it doesn't exist
        if (!Directory.Exists(targetDirectory))
        {
            Debug.Log($"Directory not found. Creating directory (recursively): {targetDirectory}");
            Directory.CreateDirectory(targetDirectory);  // This will create all parent directories if they don't exist
        }

        // Step (b): List all files in the directory
        Debug.Log($"Listing files in: {targetDirectory}");
        string[] files = Directory.GetFiles(targetDirectory);
        foreach (string file in files)
        {
            Debug.Log(file);
        }

        // Step (c): Check if the specific backup file exists, if not, create it
        string backupFilePath = Path.Combine(targetDirectory, backupFileName);
        if (!File.Exists(backupFilePath))
        {
            Debug.Log($"Backup file '{backupFileName}' not found. Creating file...");
            // You can create an empty file or initialize it with specific content
            File.Create(backupFilePath).Dispose();  // Dispose to release the file handle
        }
        else
        {
            Debug.Log($"Backup file '{backupFileName}' already exists.");
        }
    }

    // Load data from the JSON file into __records
    public bool LoadData()
    {
        if (File.Exists(filePath))
        {
            // Read the JSON file
            string json = File.ReadAllText(filePath);
            //Debug.Log("READ THE JSON" + json);
            // Deserialize the JSON string into the __records dictionary
            var data = ShellRun.ParseJsonToDictTable(json);

            if (data != null )
            {
                __records = data;
                return true;
            }
        }
        else
        {
            // If the file doesn't exist, create it
            SaveData(); // Create an empty file
        }

        return true;
    }

    // Utility class to handle serialization of dictionaries (JsonUtility doesn't directly handle generic dictionaries well)
    [System.Serializable]
    private class Serialization<T>
    {
        public T target;
        public Serialization(T target)
        {
            this.target = target;
        }
    }
}
