using System.Collections.Generic;
using UnityEngine;
using VisualCommand;

class PlayerAgent:Agent
{
    //         Debug.Log($"PROCESSING NAVIGATION EVENT IN SpaceCombatScreen");

    private string __targetAction = "";    
    public override void ChooseTargetAction()
    {
       // Debug.Log("I am a player, I already chose off time.");
    }
    public override bool SetTargetAction(string actionId)
    {
        bool validAction = AgentActionType.IsValid(actionId);
        //string observableEffect = "fhausghadjFAILPLEASE";
        if (actionId == AgentActionType.Attack)
        {
            NotifyObservers(SpaceEncounterManager.ObservableEffects.AttackOn);
            NotifyObservers(SpaceEncounterManager.ObservableEffects.ShieldOff);        
            //NotifyObservers(SpaceEncounterManager.ObservableEffects.AttackOff);        
            NotifyObservers(SpaceEncounterManager.ObservableEffects.MissileOff);              
            __targetAction = actionId;
        }
        else if (actionId == AgentActionType.Missile)
        {
            NotifyObservers(SpaceEncounterManager.ObservableEffects.MissileOn);
            NotifyObservers(SpaceEncounterManager.ObservableEffects.ShieldOff);        
            NotifyObservers(SpaceEncounterManager.ObservableEffects.AttackOff);        
            //NotifyObservers(SpaceEncounterManager.ObservableEffects.MissileOff);              
            __targetAction = actionId;
        }
        else if (actionId == AgentActionType.Shield)
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
