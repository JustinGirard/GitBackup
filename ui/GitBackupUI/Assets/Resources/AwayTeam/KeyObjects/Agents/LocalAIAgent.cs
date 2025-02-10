using UnityEngine;
class LocalAIAgent:Agent
{
    private string __targetAction = "";
    private string __targetNavigation = "";
    private string __targetFormation = "";
    private string __targetAttackPattern = "";

    public override void ChooseCommand(string commandType)
    {
        return;
        //Debug.Log("Choosing Action");
        //__targetAction= AgentAttackType.Shield;
        
        int randomIndex = Random.Range(0, 3);
        if (randomIndex == 0)
        {
            __targetAction = AgentPowerType.Attack;
        }
        else if (randomIndex == 1)
        {
            __targetAction= AgentPowerType.Missile;
        }
        else if (randomIndex == 2)
        {
            __targetAction= AgentPowerType.Shield;
        }        

    }

    public override bool AddCommand(string commandType, string actionId, GameObject subject)
    {
        throw new System.Exception("AI can not be controlled by mortls");
        return false;   
    }
    
    public override string GetSelectedCommand(string commandType)
    {
        if (commandType == AgentCommand.Type.Combat)
        {
            //Debug.Log($"Getting Combat Action '{__targetAction}'");
            return __targetAction;

        }
        if (commandType == AgentCommand.Type.Navigation)
            return __targetNavigation;
        if (commandType == AgentCommand.Type.Formation)
            return __targetFormation;
        if (commandType == AgentCommand.Type.AttackPattern)
            return __targetAttackPattern;
        return null;
    }

}