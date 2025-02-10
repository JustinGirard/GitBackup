using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;
using VisualCommand;

class PlayerAgent:Agent, ICommandReceiver
{
    [SerializeField]
    public override void ChooseCommand(string commandType)
    {
       // Debug.Log("I am a player, I already chose off time.");
    }    
    public void OnCommandReceived(string commandId, Vector2 mousePosition)
    {
        /*
       if( GeneralInputManager.Command.player_control_mode_up == commandId)
       {
          TogglePlayerNavigationMode();
           UINotificationWaterfall.Instance().Dispatch("basic", "player_control_mode", $"nav_mode: {GetPlayerNavigationMode()}", 10f, true); 
           // Agent target =  GetPrimaryEnemyAgent();
           // if (target != null)
           // {
           //     AddCommand( AgentCommand.Type.Navigation,  AgentNavigationType.NavigateToOff, target.gameObject);
           // }
           // else
           // {
           //     AddCommand( AgentCommand.Type.Navigation,  AgentNavigationType.NavigateToOff, null);
           // }
       }
       if( GeneralInputManager.Command.player_aim_cycle_up == commandId)
       {
            BlindToggleEnemyAgent();
            Agent target =  GetPrimaryEnemyAgent();
            if (target != null)
            {
                SetPlayerNavigationMode(PlayerNavigationMode.ZTargetingEnemy);
                UINotificationWaterfall.Instance().Dispatch("basic", "player_aim_cycle_up", $"enemy_target: {target.gameObject.name}", 10f, true); 
                AddCommand( AgentCommand.Type.Navigation,  AgentNavigationType.NavigateAim, target.gameObject);
            }
            else // target == null
            {
                SetPlayerNavigationMode(PlayerNavigationMode.TopDownTraditional);
                ClearEnemyAgent();
                UINotificationWaterfall.Instance().Dispatch("basic", "player_aim_cycle_up", $"enemy_target: NONE", 10f, true); 
                AddCommand( AgentCommand.Type.Navigation,  AgentNavigationType.NavigateAim, null);
            }
       }
       */
    }

    public GameObject GetGameObject(){
        return this.gameObject;
    }

    public override bool AddCommand(string commandType, string actionId, GameObject subject)
    {
        bool ret = false;
        if (commandType == AgentCommand.Type.Combat)
            ret = AddTargetAttack(actionId,subject);
        if (commandType == AgentCommand.Type.Navigation)
            ret = AddTargetNavigation(actionId,subject);
        if (commandType == AgentCommand.Type.Formation)
            ret = AddTargetFormation(actionId);
        if (commandType == AgentCommand.Type.AttackPattern)
            ret = AddTargetAttackPattern(actionId);
        return ret;
    }

    public AgentCommand GetFirstActionFromQueue(string commandType)
    {
        if(!AgentCommand.Type.IsValid(commandType))
        {
            return null;
        }
        return null;
    }
    
    public void TogglePlayerNavigationMode()
    {
        if (playerNavigationMode == PlayerNavigationMode.TopDownTraditional)
        {
            playerNavigationMode = PlayerNavigationMode.ZTargetingEnemy;
        }
        else if (playerNavigationMode == PlayerNavigationMode.ZTargetingEnemy)
        {
            playerNavigationMode = PlayerNavigationMode.TopDownTraditional;
        }
        else
        {
            playerNavigationMode = PlayerNavigationMode.TopDownTraditional; // Default to a valid mode
        }
    }
    
    public void SetPlayerNavigationMode(string navMode)
    {
        if (!PlayerNavigationMode.IsValid(navMode))
        {
            Debug.LogError($"SetPlayerNavigationMode used with invalid navMode={navMode}");
            return;
        }
        playerNavigationMode  = navMode;
    }
    

    string playerNavigationMode = PlayerNavigationMode.ZTargetingEnemy;
    public string GetPlayerNavigationMode()
    {
        return playerNavigationMode;
    }
    
    private bool AddTargetFormation(string formation)
    {
        if (!AgentFormation.all.Contains(formation) )
        {
            Debug.LogError($"Could not find formation {formation}");
            return false;
        }
        EnqueueAgentCommand(AgentCommand.Type.Formation,formation,null);
        return true;
    }
    private bool AddTargetAttackPattern(string attackPattern)
    {
        if (!AgentTargetPattern.all.Contains(attackPattern) )
        {
            Debug.LogError($"Could not find attackPattern {attackPattern}");
            return false;
        }
        EnqueueAgentCommand(AgentCommand.Type.AttackPattern,attackPattern,null);
        return true;
    }

    private bool AddTargetNavigation(string actionId, GameObject subject)
    {
        bool validAction = AgentNavigationType.IsValid(actionId);
        if (validAction )
        {
            EnqueueAgentCommand(AgentCommand.Type.Navigation,actionId,subject);
            return true;
        }
        else
        {
            Debug.LogError($"Player Agent  {this.name} asked to set an invalid target action of ({actionId})");
        }
            return false;
    }

    public void Start(){
        GeneralInputManager.Instance().RegisterObserver(this);
    }

    public void Update()
    {
        float deltaTime = Time.deltaTime;
        RunIfTime("RunPlayerActions", 0.01f, deltaTime, () =>{
            StartCoroutine(RunActions());
        });
    }

    private  bool AddTargetAttack(string attackId, GameObject subject)
    {
        if (!AgentPowerType.IsValid(attackId) )
        {
            Debug.LogError($"Could not find attack {attackId}");
            return false;
        }
        EnqueueAgentCommand(AgentCommand.Type.Combat,attackId,subject);
        return true;
    }
}
