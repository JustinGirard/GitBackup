using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using System.Linq;

public class ResourceTypes {

    public static bool IsValid(string resource)
    {
        return all.Contains(resource);
    }               
    public static readonly List<string> all = new List<string> {
        Food, Power, Clones, Parts, Currency, Pods, Soldiers, Missiles, Hull, Fuel, Ammunition, AttackPower, MissilePower, ShieldPower
    };

    public const string Food = "Food";
    public const string Power = "Power";
    public const string Clones = "Clones";
    public const string Parts = "Parts";
    public const string Currency = "Currency";
    public const string Pods = "Pods";
    public const string Soldiers = "Soldiers";
    public const string Missiles = "Missiles";
    public const string Hull = "Hull";
    public const string Fuel = "Fuel";
    public const string Ammunition = "Ammunition";
    public const string AttackPower = "AttackPower";
    public const string MissilePower = "MissilePower";
    public const string ShieldPower = "ShieldPower";
}

public class ATResourceData : StandardData
{
    
    public virtual void RefreshResources(){}
    public virtual void Lock(){}
    public virtual void Unlock(){}
    public virtual void AddSubResource(string name, ATResourceData subResource){}
    public virtual Dictionary<string,ATResourceData> GetSubResources(){ return null;}
    public virtual void RefreshResources(bool doDebug = false){}

    
    // resources.AddSubResource(unit.name,unitResourceData);
    public static Dictionary<string, float> AddDeltas(Dictionary<string, float> dict1, Dictionary<string, float> dict2)
    {
        return dict1.Keys.Union(dict2.Keys)
                    .ToDictionary(key => key, key => dict1.GetValueOrDefault(key) + dict2.GetValueOrDefault(key));
    }

    public static Dictionary<string, float> SubtractDeltas(Dictionary<string, float> dict1, Dictionary<string, float> dict2)
    {
        return dict1.Keys.Union(dict2.Keys)
                    .ToDictionary(key => key, key => dict1.GetValueOrDefault(key) - dict2.GetValueOrDefault(key));
    }

    public static Dictionary<string, float> MultiplyDeltas(Dictionary<string, float> dict1, Dictionary<string, float> dict2)
    {
        return dict1.Keys.Union(dict2.Keys)
                    .ToDictionary(key => key, key => dict1.GetValueOrDefault(key) * dict2.GetValueOrDefault(key));
    }

    public static Dictionary<string, float> DivideDeltas(Dictionary<string, float> dict1, Dictionary<string, float> dict2)
    {
        return dict1.Keys.Union(dict2.Keys)
                    .ToDictionary(key => key, key => dict1.GetValueOrDefault(key) / dict2.GetValueOrDefault(key));
    }


    public void Awake()
    {
        DictTable resourceTable = new DictTable();
        SetRecords((DictTable)resourceTable);
    }

    //public virtual bool Deposit(string resourceName, float amount)
    //{
    //    return AddToRecordField("Encounter",resourceName,amount,create:true);
    //}

    //public virtual bool Deposit(Dictionary<string,float> deltaDict)
    //{
    //    bool success = true;
    //    foreach (var delta in deltaDict)
    //    {
    //      success = success && Deposit(delta.Key, (float)delta.Value);
    //    }       
    //    return success;
    //}
    public virtual float Balance(string resourceName)
    {
        return (float)GetResourceAmount(resourceName);

    }


    const float __minimumValue = 0.0f;
    const float __maximumValue = 65535.0f;
    public virtual float Deposit(string resourceName, float amount, bool above_maximum = false)
    {
        float currentAmount = (float)GetResourceAmount(resourceName);        
        if (amount < 0)
        {
            return Withdraw(
                resourceName:resourceName, 
                amount:-amount,
                below_minimum:above_maximum);
        }

        float newAmount = currentAmount + amount;
        if (newAmount > __maximumValue && !above_maximum)
        {
            float allowableDeposit = __maximumValue - currentAmount;
            AddToRecordField("Encounter", resourceName, allowableDeposit, create: true);
            return amount - allowableDeposit;
        }

        AddToRecordField("Encounter", resourceName, amount, create: true);
        return 0.0f;
    }

    public Dictionary<string, float> Deposit(Dictionary<string, float> resourceAmounts, bool above_maximum = false)
    {
        Dictionary<string, float> remainders = new Dictionary<string, float>();

        foreach (var resource in resourceAmounts)
        {
            float remainder = Deposit(resource.Key, resource.Value, above_maximum);
            if (remainder > 0)
            {
                remainders[resource.Key] = remainder;
            }
        }

        return remainders;
    }    
    
    public Dictionary<string, float> Withdraw(Dictionary<string, float> resourceAmounts, bool below_minimum = false)
    {
        Dictionary<string, float> remainders = new Dictionary<string, float>();

        foreach (var resource in resourceAmounts)
        {
            float remainder = Withdraw(resource.Key, resource.Value, below_minimum);
            if (remainder > 0)
            {
                remainders[resource.Key] = remainder;
            }
        }

        return remainders;
    }


    public virtual float Withdraw(string resourceName, float amount, bool below_minimum = false)
    {
        float currentAmount = (float)GetResourceAmount(resourceName);
        if (amount < 0)
        {
            return Deposit(
                resourceName:resourceName, 
                amount:-amount,
                above_maximum:below_minimum);
        }

        float newAmount = currentAmount - amount;
        if (newAmount < __minimumValue && !below_minimum)
        {
            float allowableWithdraw = currentAmount - __minimumValue;
            SubtractFromRecordField("Encounter", resourceName, allowableWithdraw);
            return amount - allowableWithdraw;
        }

        SubtractFromRecordField("Encounter", resourceName, amount);
        return 0.0f;
    }


    public virtual object GetResourceAmount(string resourceName)
    {
        object val = GetRecordField("Encounter",resourceName);
        if (val == null)
            return 0.0f;
        return val;
        
    }
    public virtual object GetResourceMax(string resourceName)
    {
        return 20;
        
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

