using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StandardPowerStatusPanel : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI title; 
    [SerializeField]
    TextMeshProUGUI description;
    [SerializeField]
    UnityEngine.UI.Image heatProgress; // Modify Fill Amount
    [SerializeField]
    UnityEngine.UI.Image executeProgress; // Modify Fill Amount
    [SerializeField]
    UnityEngine.UI.Image chargeProgress; // Modify Fill Amount
    private GameObject __icon;
    bool isActive;
    [SerializeField]
    StandardSystem boundSystem;
    [SerializeField]
    GameObject iconLocation;
    private StandardSystem lastBoundSystem;




    public void BindSystem(StandardSystem system)
    {
        //Debug.Log("StandardPowerStatusPanel: Binding BindSystem");
        if(lastBoundSystem !=system)
        {
            //Debug.Log("StandardPowerStatusPanel: Binding BindSystem2");
            boundSystem = system;
            Rebind();
            lastBoundSystem = system;
        }
    }
    
    public void Rebind(){

        //Debug.Log("StandardPowerStatusPanel: Binding 3");
        // Clean up old icon
        if (__icon != null)
        {
            __icon.SetActive(false);
        }
        

        if (iconLocation == null)
        {
            Debug.LogWarning("StandardPowerStatusPanel: IconLocation not found");
            return;
        }
        
        if (boundSystem == null)
        {
            Debug.LogWarning("StandardPowerStatusPanel: boundSystem is null");
            return;
        }
        
        GameObject iconPrefab = boundSystem.GetIconPrefab();
        if (iconPrefab == null)
        {
            Debug.LogWarning("StandardPowerStatusPanel: GetIconPrefab returned null");
            return;
        }
        //Debug.Log("StandardPowerStatusPanel: Binding 4");
        ObjectPool poolInstance = ObjectPool.Instance();
        if (poolInstance == null)
        {
            Debug.LogWarning("StandardPowerStatusPanel: ObjectPool instance is null");
            return;
        }
        __icon = poolInstance.Load(iconPrefab);
        if (__icon == null)
        {
            Debug.LogWarning("StandardPowerStatusPanel: ObjectPool failed to load icon");
            return;
        }
        
        //Debug.Log($"StandardPowerStatusPanel: Binding 5 ----{__icon.name}");
        __icon.transform.SetParent(iconLocation.transform, false);

        Ricimi.Gradient heatGrad = heatProgress.gameObject.GetComponent<Ricimi.Gradient>(); // Modify Fill Amount
        Ricimi.Gradient executeGrad = executeProgress.gameObject.GetComponent<Ricimi.Gradient>(); // Modify Fill Amount
        Ricimi.Gradient chargeGrad = chargeProgress.gameObject.GetComponent<Ricimi.Gradient>(); // Modify Fill Amount
        heatGrad.Color1 = Color.red;
        heatGrad.Color2 = Color.yellow;
        chargeGrad.Color1 = Color.blue;
        chargeGrad.Color2 = Color.white;
        executeGrad.Color1 = Color.green;
        executeGrad.Color2 = Color.green;
    } 

    void Update()
    {
        if (boundSystem == null) {
            Debug.LogWarning("StandardPowerStatusPanel: boundSystem is null");
            return;
        }
        
        if (heatProgress == null) {
            Debug.LogWarning("StandardPowerStatusPanel: heatProgress is null");
            return;
        }
        if (executeProgress == null) {
            Debug.LogWarning("StandardPowerStatusPanel: executeProgress is null");
            return;
        }
        if (chargeProgress == null) {
            Debug.LogWarning("StandardPowerStatusPanel: chargeProgress is null");
            return;
        }
        if(lastBoundSystem !=boundSystem)
        {
            BindSystem(boundSystem);
        }        

        FloatRange overheatLevel = boundSystem.GetLevel("overheat");
        FloatRange executeLevel = boundSystem.GetLevel("execute");
        FloatRange chargeLevel = boundSystem.GetLevel("charge");
        
        if (overheatLevel == null) {
            Debug.LogWarning("StandardPowerStatusPanel: overheatLevel is null");
            return;
        }
        if (executeLevel == null) {
            Debug.LogWarning("StandardPowerStatusPanel: executeLevel is null");
            return;
        }
        if (chargeLevel == null) {
            Debug.LogWarning("StandardPowerStatusPanel: chargeLevel is null");
            return;
        }
        
        heatProgress.fillAmount = overheatLevel.Percent();
        executeProgress.fillAmount = executeLevel.Percent();
        chargeProgress.fillAmount = chargeLevel.Percent();

        description.text = boundSystem.GetShortDescriptionText();
    }



   

}