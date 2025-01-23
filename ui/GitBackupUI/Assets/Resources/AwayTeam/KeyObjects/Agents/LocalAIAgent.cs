using UnityEngine;
class LocalAIAgent:Agent
{
    private string __targetAction = "";
    private string __targetNavigation = "";
    public override void ChooseTargetAction()
    {
        return; 
        int randomIndex = Random.Range(0, 3);
        if (randomIndex == 0)
        {
            __targetAction = AgentActionType.Attack;
        }
        else if (randomIndex == 1)
        {
            __targetAction= AgentActionType.Missile;
        }
        else if (randomIndex == 2)
        {
            __targetAction= AgentActionType.Shield;
        }        

    }
    public override void ChooseTargetNavigation()
    {
         return;
        __targetNavigation = AgentNavigationType.Halt;
    }

    public override bool SetTargetAction(string actionId)
    {
        throw new System.Exception("AI can not be controlled by mortls");
        return false;   
    }
    
    public override bool SetTargetNavigation(string actionId)
    {
        throw new System.Exception("AI can not be controlled by mortls");
        return false;   
    }

    public override string GetTargetAction()
    {
       return __targetAction;
    }
    public override string GetTargetNavigation()
    {
       return __targetNavigation;
    }


    public override void ResetTargetAction()
    {
        __targetAction = "";
    }   
    public override void ResetTargetNavigation()
    {
        __targetNavigation = "";
    }    
}    
