using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

public class ATResourceData : StandardData
{
    public void Awake()
    {
        // ContainsKey
        DictTable resourceTable = new DictTable();
        SetRecords((DictTable)resourceTable);
    }


    public bool Deposit(string resourceName, float amount)
    {
        return AddToRecordField("Encounter",resourceName,amount);
    }

    public bool Withdraw(string resourceName, float amount)
    {
        return SubtractFromRecordField("Encounter",resourceName,amount);
    }

    public object GetResourceAmount(string resourceName)
    {
        return GetRecordField("Encounter",resourceName);
    }
    // Standard Hooks:
    public override void AfterSaveData()
    {

    }
    public override void BeforeLoadData()
    {

    }      

    public override List<string> ListRecords()
    {
        BeforeLoadData();     
        DictTable recs = GetRecords();
        return new List<string>(recs.Keys);
    }

    public override DictStrObj AfterAlterRecord(DictStrObj rec)
    {
        return rec;
    }



    /*
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
    }*/



    /*
    public void AddElement(string resourceName, object element)
    {
        if (!resourceTable.ContainsKey(resourceName))
        {
            resourceTable[resourceName] = new List<object>();
        }

        if (resourceTable[resourceName] is List<object> resourceList)
        {
            resourceList.Add(element);
        }
        else
        {
            Debug.LogError($"Resource {resourceName} is fungible. Use Deposit for numeric quantities.");
        }
    }

    public bool RemoveElement(string resourceName, object element)
    {
        if (resourceTable.ContainsKey(resourceName) && resourceTable[resourceName] is List<object> resourceList)
        {
            return resourceList.Remove(element);
        }

        Debug.LogError($"Resource {resourceName} is either not a list or does not exist.");
        return false;
    }*/

    /// <summary>
    /// Get the current amount of a fungible resource or the count of a non-fungible resource.
    /// </summary>
    /*
    public bool Transfer(ATResourceData target, string resourceName, float? amount = null, object element = null)
    {
        if (resourceTable.ContainsKey(resourceName))
        {
            if (amount.HasValue && resourceTable[resourceName] is float)
            {
                if (Withdraw(resourceName, amount.Value))
                {
                    target.Deposit(resourceName, amount.Value);
                    return true;
                }
            }
            else if (element != null && resourceTable[resourceName] is List<object>)
            {
                if (RemoveElement(resourceName, element))
                {
                    target.AddElement(resourceName, element);
                    return true;
                }
            }
        }

        Debug.LogError($"Resource {resourceName} does not exist or parameters are invalid.");
        return false;
    }

    public void PrintResources()
    {
        Debug.Log($"Resources for Owner {Owner}:");
        foreach (var resource in resourceTable)
        {
            if (resource.Value is float amount)
            {
                Debug.Log($"Resource: {resource.Key}, Amount: {amount}");
            }
            else if (resource.Value is List<object> list)
            {
                Debug.Log($"Resource: {resource.Key}, Count: {list.Count}");
            }
        }
    }*/

    /*void Awake()
    {
        filePath = Application.persistentDataPath + "/profiles.json";
        //LoadData();
        // Debug.Log("1. Loading Profile data");
        // TODO implement secure password storage
        //Dictionary<string,object> results = DJson.Parse((string)obj);
        var data = FileUtils.SafeReadJsonFile(filePath);
        if (data != null )
            SetRecords((DictTable)data);
    }*/

  
}

