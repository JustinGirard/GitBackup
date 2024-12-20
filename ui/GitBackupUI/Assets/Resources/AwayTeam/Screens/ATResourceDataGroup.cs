using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;


public class ATResourceDataGroup : ATResourceData
{
    private Dictionary<string, ATResourceData> subResources = new Dictionary<string, ATResourceData>();

    public void AddSubResource(string key, ATResourceData resource)
    {
        if (!subResources.ContainsKey(key))
        {
            subResources[key] = resource;
        }
    }

    public void RemoveSubResource(string key)
    {
        if (subResources.ContainsKey(key))
        {
            subResources.Remove(key);
        }
    }

    public override bool Deposit(string resourceName, float amount)
    {

        CleanupDestroyedUnits();        
        if (amount < 0)
        {
            return Withdraw(resourceName, -amount);
        }

        if (subResources.Count == 0) return false;

        float amountPerUnit = amount / subResources.Count;
        bool success = true;

        foreach (var resource in subResources.Values)
        {
            success = success && resource.Deposit(resourceName, amountPerUnit);
        }

        return success;
    }

    public override bool Withdraw(string resourceName, float amount)
    {
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
    }
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
    private const float SummaryUpdateInterval = 3f;

    private void Update()
    {
        // Accumulate time and check if the interval has elapsed
        summaryUpdateTimer += Time.deltaTime;

        if (summaryUpdateTimer >= SummaryUpdateInterval)
        {
            summaryUpdateTimer = 0f; // Reset the timer
            UpdateSummaryTable();
        }
    }
    private Dictionary<string, int> previousRevisions = new Dictionary<string, int>();
    
    private void UpdateSummaryTable()
    {
        if (subResources.Count == 0 || base.GetRecords().Count == 0 )
            return; // No work to do if we are empty
            
        bool isDirty = false;

        foreach (var kvp in subResources)
        {
            string key = kvp.Key;
            ATResourceData resource = kvp.Value;

            int currentRevision = resource.GetDataRevision();
            if (!previousRevisions.ContainsKey(key) || previousRevisions[key] != currentRevision)
            {
                isDirty = true;
                previousRevisions[key] = currentRevision; // Update the revision ID
            }
        }

        if (!isDirty) return;

        // Build the new summary table
        var newSummaryTable = new DictObjTable();

        foreach (var resource in subResources.Values)
        {
            var records = resource.GetRecords();
            foreach (var recordKey in records.Keys)
            {
                if (!newSummaryTable.ContainsKey(recordKey))
                {
                    newSummaryTable[recordKey] = new Dictionary<string, object>();
                }

                var recordFields = records[recordKey];
                foreach (var fieldKey in recordFields.Keys)
                {
                    if (!newSummaryTable[recordKey].ContainsKey(fieldKey))
                    {
                        newSummaryTable[recordKey][fieldKey] = 0f;
                    }

                    newSummaryTable[recordKey][fieldKey] = (float)newSummaryTable[recordKey][fieldKey] + 
                                                            (float)recordFields[fieldKey];
                }
            }
        }

        // Update records in one operation
        SetRecords(newSummaryTable);
    }

    public override bool SetRecordField(string name, string field, object value, bool create = true, string keyfield = "name") 
    {
        throw new System.NotSupportedException("Direct modification of records is not allowed. Use Deposit or Withdraw instead.");
    }

    public override bool SetRecord(DictStrObj rec, string keyfield = "name") 
    {
        throw new System.NotSupportedException("Direct modification of records is not allowed. Use Deposit or Withdraw instead.");
    }

    public override void SetRecords(DictObjTable records)
    {
        throw new System.NotSupportedException("Direct modification of records is not allowed. Use Deposit or Withdraw instead.");
    }

    public override bool DeleteRecord(string name)
    {
        throw new System.NotSupportedException("Direct modification of records is not allowed. Use Deposit or Withdraw instead.");
    }


}
