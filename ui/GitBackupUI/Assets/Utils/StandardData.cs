using System.Collections.Generic;
using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;



public class StandardData : MonoBehaviour
{
    private DictTable __records = new DictTable();
    private int __dataRevision = 0;
    private System.Random random = new System.Random(); // Create a Random instance

    public virtual void  Refresh()
    {
    }
    public DictTable  GetRecords()
    {
        BeforeLoadData();     
        return __records;
    }


    public int GetDataRevision()
    {
        return __dataRevision;
    }

    public bool ContainsRecord(string name)
    {
        BeforeLoadData();     
        return __records.ContainsKey(name);
    }

    // Method to assign a new random revision number
    public void UpdateDataRevision(int code)
    {
        // Debug.Log($"++++++UpdateDataRevision Code "+code.ToString());
        __dataRevision = random.Next(1, int.MaxValue); // Assign a new random number, avoiding zero
    }

    public void PrintRecordsToDebugLog()
    {
        Debug.Log("---------My revision"+__dataRevision.ToString());
        foreach (var outerEntry in __records)
        {
            string key = outerEntry.Key;
            DictStrStr innerDict = outerEntry.Value;
            //Debug.Log($"Record Name: {key}");
            foreach (var innerEntry in innerDict)
            {
                Debug.Log($"{innerEntry.Key}: {innerEntry.Value}");
            }
            //Debug.Log("------------------------");
        }
    }

    public List<DictStrStr> ListFullRecords()
    {
        List<string> repoNameList = ListRecords(); // Simulated call for listing repos
        List<DictStrStr> repoList = new List<DictStrStr>();
        foreach (var repo_name in repoNameList)
        {
            //Debug.Log($"{this.ToString()}: Found {repo_name}");
            DictStrStr reporec = GetRecord(repo_name);
            if (reporec!= null) 
            {
                //Debug.Log($"{this.ToString()}: Adding DATA {repo_name} into ");
                repoList.Add(reporec);
            }
        }

        return repoList;

    }
    public virtual List<string> ListRecords()
    {
        BeforeLoadData();     
        return new List<string>(__records.Keys);
    }
    public virtual void BeforeLoadData()
    {


    }
    public virtual void AfterSaveData()
    {

        
    }


    // Simulate getting repository info
    public virtual DictStrStr GetRecord(string name)
    {
        BeforeLoadData();     
        if (__records.ContainsKey(name))
        {
            return __records[name];
        }
        return null; // Repo not found
    }

    public virtual string GetRecordField(string name,string field)
    {
        BeforeLoadData();     
        if (!__records.ContainsKey(name))
        {
            return null;
        }
        if (!__records[name].ContainsKey(field))
        {
            return null;
        }
        return __records[name][field];
    }  

    public virtual bool SetRecordField(string name,string field,string value)
    {
        if (!__records.ContainsKey(name))
        {
            return false;
        }
        if (!__records[name].ContainsKey(field))
        {
            return false;
        }
        __records[name][field] = value;
        UpdateDataRevision(1);   
        AfterSaveData();     
        return true;
    }
    protected bool SetRecord(DictStrStr rec,string keyfield="name" )
    {
        string name = rec[keyfield];
        __records[name] = rec;
        UpdateDataRevision(2);   
        AfterSaveData();     
        return true; 
    }
    public virtual bool DeleteRecord(string name)
    {
        if (__records.ContainsKey(name))
        {
            __records.Remove(name);

        }
        UpdateDataRevision(3);        
        AfterSaveData();
        return true;
    }
    public void  SetRecords(DictTable records)
    {
        __records = records;
        UpdateDataRevision(4);        
        AfterSaveData();
    }
    public void  ClearRecords()
    {
        __records = new DictTable();   
        UpdateDataRevision(4);        
        AfterSaveData();
    }        

}