using UnityEngine;


/*

public void ResetTargetAction(string agent_id)
    {
        if(agent_id == "agent_1")
        {
            NotifyAllScreens( ObservableEffects.AttackOff);            
            NotifyAllScreens( ObservableEffects.ShieldOff);            
            NotifyAllScreens( ObservableEffects.MissileOff);            
            __targetAgent1Action = "";
        }
        if(agent_id == "agent_2")
        {
            __targetAgent2Action = "";
        }
    }
    public void SetTargetAction(string agent_id,string commandId)
    {
        
        string targEffect = "";
        if (commandId== AgentActions.Attack)
            targEffect= ObservableEffects.AttackOn;
        if (commandId== AgentActions.Missile)
            targEffect= ObservableEffects.MissileOn;
        if (commandId== AgentActions.Shield)
            targEffect= ObservableEffects.ShieldOn;

        if(agent_id == "agent_1")
        {
            if(targEffect.Length > 0)
                NotifyAllScreens(targEffect);
            __targetAgent1Action = commandId;
        }
        if(agent_id == "agent_2")
        {
            __targetAgent2Action = commandId;
        }
    }
////////
///////
///////
//////
///

    public virtual void SetTargetAction()
    {
        throw new System.Exception("No Implemented choice");
    }
    public virtual string GetTargetChoice()
    {
        throw new System.Exception("No Implemented choice");
        return "";
    }    

    public virtual void ResetActionChoice()
    {
        throw new System.Exception("No Implemented reset");


    }    



*/
class LocalAIAgent:Agent
{
    private string __targetAction = "";
    public override void ChooseTargetAction()
    {
        int randomIndex = Random.Range(0, 3);
        if (randomIndex == 0)
        {
            __targetAction = AgentActions.Attack;
        }
        else if (randomIndex == 1)
        {
            __targetAction= AgentActions.Missile;
        }
        else if (randomIndex == 2)
        {
            __targetAction= AgentActions.Shield;
        }        

    }
    public override bool SetTargetAction(string actionId)
    {
        throw new System.Exception("AI can not be controlled by mortls");
        return false;   
    }

    public override string GetTargetAction()
    {
       return __targetAction;
    }


    public override void ResetTargetAction()
    {
        __targetAction = "";
    }    
}