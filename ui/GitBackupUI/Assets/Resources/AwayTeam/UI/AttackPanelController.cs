using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for accessing Image components

public class PanelModeController : MonoBehaviour, ICommandReceiver
{
    [SerializeField]
    private List<GameObject> modes; // List of attack modes
    [SerializeField]
    private GameObject selectedMode;      // Currently selected mode
    [SerializeField]
    private GameObject currentMode;      // Currently selected mode

    [SerializeField]
    private string panelType="attack";      // Currently selected mode
    [SerializeField]
    private SpaceCombatScreen combatScreen; 
    void Start()
    {
        GeneralInputManager.Instance().RegisterObserver(this);
        UpdateSelection();
    }
    public void SetSelection(string modeName)
    {
        selectedMode = modes.Find(mode => mode.name == modeName);
        UpdateSelection();
    }
    public void OnCommandReceived(string commandId, Vector2 mousePosition)
    {
        if (commandId == GeneralInputManager.Command.power_01_up && panelType == "power")
            SetSelection("Attack");
        if (commandId == GeneralInputManager.Command.power_02_up && panelType == "power")
            SetSelection("Missile");
        if (commandId == GeneralInputManager.Command.power_03_up && panelType == "power")
            SetSelection("Shield");
        if (commandId == GeneralInputManager.Command.attackpattern_up_up && panelType == "attack")
            ToggleSelection();
        if (commandId == GeneralInputManager.Command.formation_up_up && panelType == "formation")
            ToggleSelection();
    }
    void UpdateSelection()
    {
        // Iterate through attackModes and update colors
        //Debug.Log($"Final update with  {currentMode}");
        foreach (GameObject mode in modes)
        {
            Image image = mode.GetComponent<Image>();
            TMPro.TextMeshProUGUI text = mode.GetComponentInChildren<TMPro.TextMeshProUGUI>();

            if (image != null)
            {
                if (selectedMode!= null && mode == selectedMode)
                {
                    image.color = Color.red; 
                }
                else if (currentMode!= null && mode == currentMode)
                {
                    image.color = Color.white; 
                }
                else
                {
                    image.color = Color.gray; 
                }
            }
            if (text != null)
            {
                if (selectedMode!= null && mode == selectedMode)
                {
                    text.color = Color.red; 
                }
                else if (currentMode!= null && mode == currentMode)
                {
                    text.color = Color.white;
                }
                else
                {
                    text.color = Color.gray; 
                }
            }            
        }
        if (panelType == "power" && selectedMode)
            combatScreen.SetPlayerPower(selectedMode.name);
        if (panelType == "attack" && selectedMode)
            combatScreen.SetPlayerAttackPattern(selectedMode.name);
        if (panelType == "formation" && selectedMode)
            combatScreen.SetPlayerFormation(selectedMode.name);

    }
    public void SetRevisedValue(string val)
    {
       //Debug.Log($"Processing {val}");
        selectedMode = null;
        foreach (GameObject mode in modes)
        {
            if (mode.name == val)
            {
                //Debug.Log($"Setting current mode : {mode}");
                currentMode = mode;
                UpdateSelection();
                return;
            }
        }
        UpdateSelection();
        Debug.LogError($"Could not selet the mode {val}");
    }
    public GameObject GetGameObject()
    {
        return this.gameObject;
    }



    void ToggleSelection()
    {
        int currentIndex = modes.IndexOf(selectedMode);
        int nextIndex = (currentIndex + 1) % modes.Count;
        selectedMode = modes[nextIndex];
        UpdateSelection();
    }
}
