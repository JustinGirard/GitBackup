using System.Collections.Generic;
using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using System.IO;

public class JobData : StandardData
{
    public bool AddJob(string id, string name, string status)
    {
        if (string.IsNullOrEmpty(id))
            throw new System.Exception("Null is id");
        if (string.IsNullOrEmpty(name))
            throw new System.Exception("Null is name");
        if (string.IsNullOrEmpty(status))
            throw new System.Exception("Null is status");
        return SetRecord(new DictStrStr
        {
            { "id", id },
            { "name", name },
            { "status", status },
        });

    }
}

