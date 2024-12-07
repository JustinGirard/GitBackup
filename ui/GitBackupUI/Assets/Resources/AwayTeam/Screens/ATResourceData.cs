using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

public class ATResourceData : StandardData
{
    public void Awake()
    {
        DictTable resourceTable = new DictTable();
        SetRecords((DictTable)resourceTable);
    }

    public bool Deposit(string resourceName, float amount)
    {
        //Debug.Log($"Setting Resource {name}:{resourceName}:{amount.ToString()}");
        return AddToRecordField("Encounter",resourceName,amount,create:true);
    }

    public bool Withdraw(string resourceName, float amount)
    {
        return SubtractFromRecordField("Encounter",resourceName,amount);
    }

    public object GetResourceAmount(string resourceName)
    {
        object val = GetRecordField("Encounter",resourceName);
        return val;
        
    }
    // Standard Hooks:

    public override List<string> ListRecords()
    {
        BeforeLoadData();     
        DictTable recs = GetRecords();
        return new List<string>(recs.Keys);
    }

    public override DictStrObj AfterAlterRecord(DictStrObj rec) {return rec;}
    public override void AfterSaveData(){}
    public override void BeforeLoadData(){}      

}
