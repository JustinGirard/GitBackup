using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StandardResourceStatusPanel : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI title;
    [SerializeField]
    private TextMeshProUGUI description;
    [SerializeField]
    private UnityEngine.UI.Image heatProgress; // Modify Fill Amount
    [SerializeField]
    private UnityEngine.UI.Image executeProgress; // Modify Fill Amount
    [SerializeField]
    private UnityEngine.UI.Image chargeProgress; // Modify Fill Amount

    [SerializeField]
    private GameObject iconLocation;
    [SerializeField]
    private GameObject __icon;

    [SerializeField]
    private string boundResourceId;
    [SerializeField]
    private ATResourceData boundResource;

    public void BindResource(string resourceId, ATResourceData resourceData)
    {
        if (boundResourceId != resourceId || boundResource != resourceData)
        {
            boundResourceId = resourceId;
            boundResource = resourceData;
            Rebind();
        }
    }

    private void Rebind()
    {
        if (__icon != null)
        {
            __icon.SetActive(false);
        }
        
        if (iconLocation == null)
        {
            Debug.LogWarning("StandardResourceStatusPanel: IconLocation not found");
            return;
        }

        if (boundResource == null || string.IsNullOrEmpty(boundResourceId))
        {
            Debug.LogWarning("StandardResourceStatusPanel: Bound resource is null or ID is invalid");
            return;
        }
        
        //GameObject iconPrefab = boundResource.GetIconPrefab();
        //if (iconPrefab == null)
       // {
       //     Debug.LogWarning("StandardResourceStatusPanel: GetIconPrefab returned null");
       //     return;
       // }

        ObjectPool poolInstance = ObjectPool.Instance();
        if (poolInstance == null)
        {
            Debug.LogWarning("StandardResourceStatusPanel: ObjectPool instance is null");
            return;
        }
        
        //__icon = poolInstance.Load(iconPrefab);
        //if (__icon == null)
        //{
        //    Debug.LogWarning("StandardResourceStatusPanel: ObjectPool failed to load icon");
        //    return;
        //}
        
        ///__icon.transform.SetParent(iconLocation.transform, false);

        Ricimi.Gradient heatGrad = heatProgress.gameObject.GetComponent<Ricimi.Gradient>();
        Ricimi.Gradient executeGrad = executeProgress.gameObject.GetComponent<Ricimi.Gradient>();
        Ricimi.Gradient chargeGrad = chargeProgress.gameObject.GetComponent<Ricimi.Gradient>();
        heatGrad.Color1 = Color.red;
        heatGrad.Color2 = Color.yellow;
        chargeGrad.Color1 = Color.blue;
        chargeGrad.Color2 = Color.white;
        executeGrad.Color1 = Color.green;
        executeGrad.Color2 = Color.green;
    }

    void Update()
    {
        if (boundResource == null || string.IsNullOrEmpty(boundResourceId))
        {
            Debug.LogWarning($"StandardResourceStatusPanel: Bound resource [{boundResourceId}] is null or ID is invalid");
            return;
        }

        float currentAmount = boundResource.Balance(boundResourceId);
        //Debug.Log($"Resource: {boundResourceId}, Balance: {currentAmount}");
        //Debug.Log($"GetResourceMax: {boundResource.GetResourceMax(boundResourceId)}");
        //Debug.Log($"GetResourceMax ({boundResource.GetResourceMax(boundResourceId)?.GetType()}): {boundResource.GetResourceMax(boundResourceId)}");

        float maxAmount = System.Convert.ToSingle(boundResource.GetResourceMax(boundResourceId));
        float percent = (maxAmount > 0) ? Mathf.Clamp01(currentAmount / maxAmount) : 0f;

        heatProgress.fillAmount = percent;
        executeProgress.fillAmount = percent;
        chargeProgress.fillAmount = percent;

        description.text = $"{currentAmount}/{maxAmount}";
        title.text = boundResourceId.Substring(0, Mathf.Min(4, boundResourceId.Length));
    }
}
