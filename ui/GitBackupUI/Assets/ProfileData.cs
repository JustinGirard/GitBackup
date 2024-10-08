using System.Collections.Generic;
using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

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

public class ProfileData : RepoData
{
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
}
