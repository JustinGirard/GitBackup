using System;    
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace PhysicalModel
{
    public class TransactionType 
    {
        public static bool IsValid(string action)
        {
            return all.Contains(action);
        }               
        public static readonly List<string> all = new List<string> { Projectile,Area,Melee,Block,Dodge };
        public const string Projectile = "Projectle";
        public const string Area = "AreaEffect";
        public const string Melee = "Melee";
        public const string Block = "Block";
        public const string Dodge = "Dodge";
    }    
    public class Transaction
    {
        private string transactionId;
        private string transactionType;
        private bool canExecute = false;
        private GameObject sourceUnit;
        private GameObject targetUnit;
        private List<PhysicalModel.Record> events;
        public Transaction(string transactionType, GameObject sourceUnit,GameObject targetUnit)
        {
            events = new List<PhysicalModel.Record>();
            transactionId = Guid.NewGuid().ToString();            
            if(!TransactionType.all.Contains(transactionType))
            {
                CoroutineRunner.Instance.DebugLog($"Got invalid transaction type {transactionType}");
                canExecute = false;
                throw new System.Exception($"Got invalid transaction type {transactionType}");
                return;
            }
            if(sourceUnit == null)
            {
                CoroutineRunner.Instance.DebugLog($"Tried to create an unbound transaction(1)");
                canExecute = false;
                throw new System.Exception($"Tried to create an unbound transaction");
                return;
            }
            if(targetUnit == null)
            {
                CoroutineRunner.Instance.DebugLog($"Tried to create an unbound transaction(2)");
                canExecute = false;
                throw new System.Exception($"Tried to create an unbound transaction");
                return;
            }            
            this.transactionType = transactionType;
            this.sourceUnit = sourceUnit;
            this.targetUnit = targetUnit;
            canExecute = true;
        }
        public bool CanExecute()
        {
            return canExecute;
        }
        public string GetUID(){

            return transactionId;
        }
    }

}