using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public class NavigationManager : MonoBehaviour
{
    public interface ICanInitalize
    {
        public void InitData(DictStrStr dataframe);
        public bool SetAction(string actionLabel, System.Action action );
    }
    // Start is called before the first frame update
    public string targetFirstScreen;
    void Start()
    {
        //ProfileListScreen
        NavigateTo(targetFirstScreen); // Example start, replace "HomeScreen" with your first screen's name
    }
    public void NotifyError(string param)
    {
        bool hideAll = false;
        DictStrStr dataframe = new DictStrStr
        {
            { "message", param },
            { "mode", "error" },

        };
        Debug.Log("Calling Notification Error");

        InitDataOnto("NotificationScreen", dataframe);
        NavigateTo("NotificationScreen", hideAll);

    }
    public void NotifyConfirm(string param, System.Action onConfirm, System.Action onCancel)
    {
        DictStrStr dataframe = new DictStrStr
        {
            { "message", param },
            { "mode", "confirm" },

        };
        InitDataOnto("NotificationScreen", dataframe);

        NavigatorSetAction("NotificationScreen", "OK", onConfirm);
        NavigatorSetAction("NotificationScreen", "Cancel", onCancel);
        bool hideAll = false;
        NavigateTo("NotificationScreen", hideAll);

    }

    // Method to navigate to a specific screen by object name
    public void InitDataOnto(string objectName, DictStrStr dataframe)
    {
        // Find the object
        GameObject matchedElement = FindGameObjectByName(objectName);
        if (matchedElement == null)
            throw new System.Exception($"No UI document found with the name: {objectName}");

        // Get the correct GameObject
        var initalizer_test = matchedElement.GetComponent(objectName);
        if (initalizer_test == null)
            throw new System.Exception($"No Component name : '{objectName}' on {objectName}");

        /// Cast to the initalizer
        ICanInitalize initalizer = matchedElement.GetComponent(objectName) as ICanInitalize;
        if (initalizer == null)
            throw new System.Exception($"No ICanInitalize on : {objectName}. {objectName}");

        // initalize the data
        initalizer.InitData(dataframe);
    }
    /*
    public void RefreshUI(string objectName)
    {
        // Find GameObject
        GameObject matchedElement = FindGameObjectByName(objectName);
        if (matchedElement == null)
            throw new System.Exception($"No UI document found with the name: {objectName}");
        // Extract MonoBehaviour
        var initalizer_test = matchedElement.GetComponent(objectName);
        if (initalizer_test == null)
            throw new System.Exception($"No Component name : '{objectName}' on {objectName}");
        // Extract MonoBehaviour as ICanInitalize
        ICanInitalize initalizer = matchedElement.GetComponent(objectName) as ICanInitalize;
        if (matchedElement == null)
            throw new System.Exception($"No ICanInitalize on : {objectName}. {objectName}");

    }*/

    public void NavigatorSetAction(string objectName,string action_name, System.Action action)
    {
        // Find GameObject
        GameObject matchedElement = FindGameObjectByName(objectName);
        if (matchedElement == null)
            throw new System.Exception($"No UI document found with the name: {objectName}");
        // Extract MonoBehaviour
        var initalizer_test = matchedElement.GetComponent(objectName);
        if (initalizer_test == null)
            throw new System.Exception($"No Component name : '{objectName}' on {objectName}");
        // Extract MonoBehaviour as ICanInitalize
        ICanInitalize initalizer = matchedElement.GetComponent(objectName) as ICanInitalize;
        if (matchedElement == null)
            throw new System.Exception($"No ICanInitalize on : {objectName}. {objectName}");

        // Call Refresh
        initalizer.SetAction(action_name, action);
    }


    // Method to navigate to a specific screen by object name
    public void NavigateTo(string objectName, bool hideAll = true)
    {

        if (hideAll == true)
        {
            HideAllScreens(); 
        }
        var matchedObject= FindGameObjectByName(objectName);

        var matchedElement = FindUIDocumentByName(objectName);
        if (matchedElement != null)
        {
            matchedElement.style.display = DisplayStyle.Flex;
            matchedElement.BringToFront();
            //RefreshUI(objectName);
        }
        else
        {
            Debug.LogWarning($"No screen found with an element named: {objectName}");
        }
    }

    public void NavigateToWithRecord(string objectName,DictStrStr dataframe, bool hideAll = true)
    {
        InitDataOnto(objectName, dataframe);
        NavigateTo(objectName, hideAll);
    }

    // Method to hide all UIDocument descendants
    private void HideAllScreens()
    {
        var allDocuments = AllDescendants(transform);
        foreach (var doc in allDocuments)
        {
            var rootVisualElement = doc.GetComponent<UIDocument>()?.rootVisualElement;
            rootVisualElement.style.display = DisplayStyle.None;
        }
    }

    // Recursive method to find an element by name (with optional early termination)
    private VisualElement FindUIDocumentByName(string name)
    {
        var allDocuments = AllDescendants(transform);
        foreach (var doc in allDocuments)
        {
            if (doc.gameObject.name == name)
            {
                return doc.rootVisualElement;
            }
        }
        return null; // No matching element found
    }

    // Recursive method to get all descendant game objects (and optionally terminate if a match is found)
    private List<UIDocument> AllDescendants(Transform parent)
    {
        var result = new List<UIDocument>();
        foreach (Transform child in parent)
        {
            var uiDoc = child.gameObject.GetComponent<UIDocument>();
            if (uiDoc != null)
            {
                result.Add(uiDoc);
            }
            result.AddRange(AllDescendants(child));
        }
        return result;
    }


    private GameObject FindGameObjectByName(string name)
    {
        var allDocuments = AllDescendants(transform);
        foreach (var doc in allDocuments)
        {
            if (doc.gameObject.name == name)
            {
                return doc.gameObject;
            }
        }
        return null; // No matching element found
    }
    /*
    private List<GameObject> AllGameObjectDescendants(Transform parent)
    {
        var result = new List<GameObject>();
        foreach (Transform child in parent)
        {
            var uiDoc = child.gameObject;
            result.Add(uiDoc);
            result.AddRange(AllGameObjectDescendants(child));
        }
        return result;
    }*/

}
