using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;

public class StandardData : MonoBehaviour
{
    private DictObjTable __records = new DictObjTable();
    private int __dataRevision = 0;
    private System.Random random = new System.Random(); // Create a Random instance
    protected string __statusLabel = "";

    protected virtual void SetStatusLabel(string newStatus)
    {
        __statusLabel = newStatus;
    }
    public string GetStatusLabel( )
    {
        return __statusLabel;
    }

    public virtual void  Refresh()
    {
       
    }
    public virtual DictObjTable  GetRecords()
    {
        BeforeLoadData();     
        return __records;
    }
    public virtual bool AddToRecordField(string name,string field,object value,bool create=true,string keyfield="name")
    {
        object oldVal = GetRecordField( name,field);
        if (oldVal == null)
            oldVal = 0f;
        return SetRecordField(name:name,field:field,value:(float)oldVal+(float)value,create:create,keyfield: keyfield);
    }

    public virtual bool SubtractFromRecordField(string name,string field,object value,bool create=true,string keyfield="name")
    {
            object oldVal = GetRecordField( name,field);
            if (oldVal == null)
                oldVal = 0f;
            //Debug.Log($"Doing subtract {((float)oldVal).ToString()} - {((float)value).ToString()}");
            return SetRecordField(name,field,(float)oldVal-(float)value,create,keyfield);
    }

    public virtual  int GetDataRevision()
    {
        return __dataRevision;
    }

    public virtual  bool ContainsRecord(string name)
    {
        BeforeLoadData();     
        return __records.ContainsKey(name);
    }

    public virtual void UpdateDataRevision(int code)
    {
        __dataRevision = random.Next(1, int.MaxValue); // Assign a new random number, avoiding zero
    }

    public void PrintRecordsToDebugLog()
    {
        Debug.Log("---------My revision"+__dataRevision.ToString());
        foreach (var outerEntry in __records)
        {
            string key = outerEntry.Key;
            DictStrObj innerDict = outerEntry.Value;
            foreach (var innerEntry in innerDict)
            {
                Debug.Log($"{innerEntry.Key}: {innerEntry.Value}");
            }
        }
    }

    public virtual List<DictStrObj> ListFullRecords()
    {
        List<string> repoNameList = ListRecords(); // Simulated call for listing repos
        List<DictStrObj> repoList = new List<DictStrObj>();
        foreach (var repo_name in repoNameList)
        {
            DictStrObj reporec = GetRecord(repo_name);
            if (reporec!= null) 
            {
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
    public virtual DictStrObj AfterAlterRecord(DictStrObj rec)
    {
        return rec;
    }


    // Simulate getting repository info
    public virtual DictStrObj GetRecord(string name)
    {
        BeforeLoadData();     
        if (__records.ContainsKey(name))
        {
            return __records[name];
        }
        return null; // Repo not found
    }

    public virtual bool  ContainsKey (string name)
    {
        return __records.ContainsKey(name);
    }


    public virtual object GetRecordField(string name,string field)
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

    public virtual bool SetRecordField(string name,string field,object value,bool create=true, string keyfield="name")
    {
        if (!__records.ContainsKey(name))
        {
            if(create == false)
                return false;

            if(create == true)
                SetRecord(new DictStrObj{
                    {keyfield,name}
                },keyfield:keyfield);
        }

        if (!__records[name].ContainsKey(field))
        {
            if(create == false)
                return false;
        }
        __records[name][field] = value;
        
        UpdateDataRevision(1);   
         __records[name] = AfterAlterRecord( __records[name]);   
        AfterSaveData();  
        return true;
    }
    public virtual bool SetRecord(DictStrObj rec,string keyfield="name" )
    {
        if (rec.ContainsKey(keyfield) == false)
        {
            Debug.LogError($"Coould not set a record, as the key '{keyfield}' was missing from keys. The Data: {DJson.Stringify(rec)}");
            return false;
        }
        string name = (string)rec[keyfield];
        __records[name] = rec;
        UpdateDataRevision(2);   
         __records[name] = AfterAlterRecord( __records[name]);   
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
    public virtual void  SetRecords(DictObjTable records)
    {
        __records = records;
        UpdateDataRevision(4);        
        AfterSaveData();
    }


    public void  ClearRecords()
    {
        __records = new DictObjTable();   
        UpdateDataRevision(4);        
        AfterSaveData();
    }        

}