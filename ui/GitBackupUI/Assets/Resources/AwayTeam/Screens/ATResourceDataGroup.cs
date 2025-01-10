using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;


public class ATResourceDataGroup : ATResourceData
{
    private Dictionary<string, ATResourceData> subResources = new Dictionary<string, ATResourceData>();
    public override Dictionary<string, ATResourceData> GetSubResources()
    {
        return subResources;
    }

    public override void AddSubResource(string key, ATResourceData resource)
    {
        subResources[key] = resource;
    }

    public void RemoveSubResource(string key)
    {
        if (subResources.ContainsKey(key))
        {
            subResources.Remove(key);
        }
    }
    public void ClearSubResources()
    {
        subResources.Clear();
    }    
    public override void   RefreshResources(bool doDebug = false)
    {
        UpdateSummaryTable(doDebug);
    }
    bool __locked = true;
    public override void Lock()
    {
        __locked = true;
    }
    public override void Unlock()
    {
        __locked = false;
    }
    /*
    public override float Deposit(string resourceName, float amount, bool above_maximum=false)
    {
        if (__locked == true)
            throw new System.Exception("Disabled Deposit");
        CleanupDestroyedUnits();        
        if (amount < 0)
        {
            return Withdraw(
                resourceName:resourceName, 
                amount:-amount,
                below_minimum:above_maximum);
        }

        if (subResources.Count == 0) return false;

        float amountPerUnit = amount / subResources.Count;
        bool success = true;

        foreach (var resource in subResources.Values)
        {
            success = success && resource.Deposit(resourceName, amountPerUnit);
        }

        return success;
    }*/
    public override float Deposit(string resourceName, float amount, bool above_maximum = false)
    {
        if (__locked == true)
            throw new System.Exception("Disabled Deposit");
        CleanupDestroyedUnits();        
        
        if (amount < 0)
        {
            return Withdraw(
                resourceName: resourceName, 
                amount: -amount,
                below_minimum: above_maximum);
        }

        if (subResources.Count == 0) return amount;

        float amountPerUnit = amount / subResources.Count;
        float remainder = 0.0f;
        float lastRemainder = -1.0f;        

        while (remainder != lastRemainder)
        {
            lastRemainder = remainder;
            remainder = 0.0f;
            
            foreach (var resource in subResources.Values)
            {
                remainder += resource.Deposit(resourceName, amountPerUnit, above_maximum);
            }
        }

        return remainder;
    }    

    public override float Withdraw(string resourceName, float amount, bool below_minimum = false)
    {
        if (__locked == true)
            throw new System.Exception("Disabled Withdraw");

        CleanupDestroyedUnits();        
        
        if (amount < 0)
        {
            return Deposit(resourceName: resourceName, amount: -amount, above_maximum: below_minimum);
        }

        if (subResources.Count == 0) return amount;

        float amountPerUnit = amount / subResources.Count;
        float remainder = 0.0f;
        float lastRemainder = -1.0f;

        while (remainder != lastRemainder)
        {
            lastRemainder = remainder;
            remainder = 0.0f;
            
            foreach (var resource in subResources.Values)
            {
                remainder += resource.Withdraw(resourceName, amountPerUnit, below_minimum);
            }
        }

        return remainder;
    }


    /*
    public override bool Withdraw(string resourceName, float amount)
    {
        if (__locked == true)
            throw new System.Exception("Disabled Deposit");

        CleanupDestroyedUnits();        
        if (subResources.Count == 0) return false;

        float remainder = amount;
        float previousRemainder;

        do
        {
            previousRemainder = remainder;
            float amountPerUnit = remainder / subResources.Count;
            remainder = 0;

            foreach (var resource in subResources.Values)
            {
                object currentAmountObj = resource.GetResourceAmount(resourceName);
                float currentAmount = currentAmountObj is float ? (float)currentAmountObj : 0;

                float withdrawAmount = Mathf.Max(0, Mathf.Min(currentAmount, amountPerUnit));
                resource.Withdraw(resourceName, withdrawAmount);
                remainder += (amountPerUnit - withdrawAmount);
            }

        } while (remainder > 0 && remainder < previousRemainder);

        if (remainder > 0)
        {
            // Push remaining amount proportionally negative across all resources
            float amountPerUnit = remainder / subResources.Count;

            foreach (var resource in subResources.Values)
            {
                resource.Withdraw(resourceName, amountPerUnit);
            }
        }

        return true;
    }*/
    private void CleanupDestroyedUnits()
    {
        var keysToRemove = new List<string>();

        foreach (var kvp in subResources)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        bool removed = false;
        foreach (var key in keysToRemove)
        {

            subResources.Remove(key);
            removed = true;
        }
        if (removed)
            previousRevisions.Clear();        
    }
    ////
    //////
    ///
    private float summaryUpdateTimer = 0f;
    private const float summaryUpdateInterval = 0.5f;

    private void Update()
    {
        // Accumulate time and check if the interval has elapsed
        summaryUpdateTimer += Time.deltaTime;

        if (summaryUpdateTimer >= summaryUpdateInterval)
        {
            summaryUpdateTimer = 0f; // Reset the timer
            UpdateSummaryTable();
        }
    }
    private Dictionary<string, int> previousRevisions = new Dictionary<string, int>();
    
    private void UpdateSummaryTable(bool doDebug = false)
    {
        if (subResources.Count == 0 )
        {
            if (doDebug == true)
                Debug.Log("Resource Group Empty");
            return; // No work to do if we are empty
        }
        if (doDebug == true)
            Debug.Log("RUNNING");

        bool isDirty = false;

        foreach (var kvp in subResources)
        {
            string key = kvp.Key;
            if (doDebug == true)
                Debug.Log($"checking {key}");
            ATResourceData resource = kvp.Value;

            int currentRevision = resource.GetDataRevision();
            if (!previousRevisions.ContainsKey(key) || previousRevisions[key] != currentRevision)
            {
                isDirty = true;
                previousRevisions[key] = currentRevision; // Update the revision ID
            }
        }

        if (!isDirty) return;
        if (doDebug == true)
            Debug.Log($"Now loading data ...");

        // Build the new summary table
        var newSummaryTable = new DictObjTable();
        base.ClearRecords();
        foreach (var resourceTable in subResources.Values)
        {
            var recordsAll = resourceTable.GetRecords();
            if (recordsAll.ContainsKey("Encounter") == false)
                return;
            var recordsForEncounter = recordsAll["Encounter"];
            //Debug.Log($"Loading from {resourceTable.name}...");
            //Debug.Log(DJson.Stringify(recordsForEncounter));
            foreach (var recordKey in recordsForEncounter.Keys)
            {
                if (doDebug == true)
                    Debug.Log($"1. Now loading key {recordKey} from {resourceTable.name}...");
                if (recordsForEncounter[recordKey] is float)
                {
                    base.Deposit(recordKey,(float)recordsForEncounter[recordKey]);
                }
            }
        }
    }

    protected new bool SetRecordField(string name, string field, object value, bool create = true, string keyfield = "name") 
    {
        throw new System.NotSupportedException("Direct modification of records is not allowed. Use Deposit or Withdraw instead.");
    }

    protected new bool SetRecord(DictStrObj rec, string keyfield = "name") 
    {
        throw new System.NotSupportedException("Direct modification of records is not allowed. Use Deposit or Withdraw instead.");
    }

    protected new void SetRecords(DictObjTable records)
    {
        throw new System.NotSupportedException("Direct modification of records is not allowed. Use Deposit or Withdraw instead.");
    }

    protected new bool DeleteRecord(string name)
    {
        throw new System.NotSupportedException("Direct modification of records is not allowed. Use Deposit or Withdraw instead.");
    }


}
