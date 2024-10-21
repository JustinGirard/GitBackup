using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public class JobListScreen : StandardListScreen
{
    void Start()
    {
        var navigatorObject = GameObject.Find("Navigator");
        datasource = navigatorObject.GetComponent<JobData>();

        listContainerId = "job_list_items";
        BaseStart();
    }
    void Update()
    {
        BaseUpdate();
    }

    protected override IEnumerable<Dictionary<string, object>> GenerateNavigationActions()
    {
        yield return null;
    }

    void OnEnable()
    {
        RegisterEvents();
        RegisterStandardEvents();
    }
    void RegisterEvents()
    {
        /// Special Delete Event
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        var root = GetComponent<UIDocument>().rootVisualElement;
        var navigateElement = root.Q<Button>("Close");
        navigateElement.RegisterCallback<ClickEvent>(CloseConfirm);
    }

    void EvtSelectRecord(ClickEvent evt)
    {
        string selectedProfileName = GetHighlightedItem();
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        //appState.Set("selected_profile",selectedProfileName);
        //string selectedProfile = (string)appState.Get("selected_profile");
    }

    void CloseConfirm(ClickEvent evt)
    {
        Debug.Log($"{this.name} CLOSING THE SCREEN");
        var rootVisualElement = GetComponent<UIDocument>()?.rootVisualElement;
        rootVisualElement.style.display = DisplayStyle.None;
    }


    protected override VisualElement AddToList(DictStrStr rec)
    {

        // Create a new instance of the RepoListItem template
        var repoListItem = new VisualElement();
        var repoListItemTemplate = Resources.Load<VisualTreeAsset>("Controls/RepoListItem/JobListItem");
        repoListItemTemplate.CloneTree(repoListItem);
        repoListItem.style.height = 40; // TODO TOTAL HACK. 
        
        // Set the data
        repoListItem.Q<Label>("job_id").text = rec["id"];
        repoListItem.Q<Label>("job_name").text = rec["name"];
        repoListItem.Q<Label>("job_status").text = rec["status"];
        repoListItem.Q<Label>("job_running").text = rec["running"];
        
        // Add the new item to the repo list container
        listItemContainer.Add(repoListItem);
        var navigatorObject = GameObject.Find("Navigator"); //TODO ew so wasteful. HACK
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        repoListItem.RegisterCallback<ClickEvent>(evt => OnRepoItemClick(repoListItem, name));
        return repoListItem;
    }
}
