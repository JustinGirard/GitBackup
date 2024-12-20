using System.Collections.Generic;
using UnityEngine;

class PlayerAgent:Agent
{
    private string __targetAction = "";    
    public override void ChooseTargetAction()
    {
       // Debug.Log("I am a player, I already chose off time.");
    }

    public override bool SetTargetAction(string actionId)
    {
        bool validAction = AgentActions.IsValid(actionId);
        string observableEffect = "fhausghadjFAILPLEASE";
        if (actionId == AgentActions.Attack)
        {
            NotifyObservers(SpaceEncounterManager.ObservableEffects.AttackOn);
            NotifyObservers(SpaceEncounterManager.ObservableEffects.ShieldOff);        
            //NotifyObservers(SpaceEncounterManager.ObservableEffects.AttackOff);        
            NotifyObservers(SpaceEncounterManager.ObservableEffects.MissileOff);              
            __targetAction = actionId;
        }
        else if (actionId == AgentActions.Missile)
        {
            NotifyObservers(SpaceEncounterManager.ObservableEffects.MissileOn);
            NotifyObservers(SpaceEncounterManager.ObservableEffects.ShieldOff);        
            NotifyObservers(SpaceEncounterManager.ObservableEffects.AttackOff);        
            //NotifyObservers(SpaceEncounterManager.ObservableEffects.MissileOff);              
            __targetAction = actionId;
        }
        else if (actionId == AgentActions.Shield)
        {
            NotifyObservers(SpaceEncounterManager.ObservableEffects.ShieldOn);
            //NotifyObservers(SpaceEncounterManager.ObservableEffects.ShieldOff);        
            NotifyObservers(SpaceEncounterManager.ObservableEffects.AttackOff);        
            NotifyObservers(SpaceEncounterManager.ObservableEffects.MissileOff);
            __targetAction = actionId;
        }
        else if(actionId=="" || actionId == null )
        {
            Debug.LogError($"Player Agent  {this.name}  actionId was null ({actionId})");
            return false;

        }
        else
        {
            Debug.LogError($"Player Agent  {this.name} asked to set an invalid target action of ({actionId})");
            return false;

        }

        return true;
    }

    public override void ResetTargetAction()
    {
        __targetAction = "";

    }    
    public override string GetTargetAction()
    {
       return __targetAction;
    }

}
