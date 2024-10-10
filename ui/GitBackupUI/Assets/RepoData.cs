using System.Collections.Generic;
using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

class RecordRepoReference
{
    public string name;
    public RecordRepoReference(DictStrStr record)
    {
        name = record["name"];
    }

    public DictStrStr ToDictRecord()
    {
        DictStrStr d = new DictStrStr { { "name", name } };
        return d;
    }

}
class RecordRepoFull
{
    public string name;
    public string url;
    public string branch;
    public string status;
    public string last_updated;
    public string username;

    public RecordRepoFull(DictStrStr record)
    {
        name = record["name"];
        url = record["url"];
        branch = record["branch"];
        status = record["status"];
        last_updated = record["last_updated"];
        //username = record["username"];
    }

    public DictStrStr ToDictRecord()
    {
        DictStrStr d = new DictStrStr {
            { "name", name },
            { "url", url },
            { "branch", branch },
            { "status", status },
            { "last_updated", last_updated },
        };
        return d;
    }

}


public class RepoData : MonoBehaviour
{

    protected DictTable __records = new DictTable();
    //private Dictionary<string, List<string>> repoUsers = new Dictionary<string, List<string>>();
    //private DictTable users = new DictTable(); // To store user information

    // Simulate adding a repository with details like name, URL, and branch
    public bool AddRepo(string name, string repoUrl, string branch)
    {
        __records[name] = new DictStrStr
        {
            { "name", name },
            { "url", repoUrl },
            { "branch", branch },
            { "status", "Active" },
            { "last_updated", "Never" } // Default status on creation
        };
        return true; // Repo already exists
    }
    public virtual bool DeleteRecord(string name)
    {
        if (__records.ContainsKey(name))
        {
            __records.Remove(name);

        }
        return true; // Repo already exists
    }

    public virtual List<string> ListRecords()
    {
        return new List<string>(__records.Keys);
    }

    // Simulate getting repository info
    public virtual DictStrStr GetRecord(string name)
    {
        if (__records.ContainsKey(name))
        {
            return __records[name];
        }
        return new DictStrStr(); // Repo not found
    }

}
