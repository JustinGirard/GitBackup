using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
//public class RepoListScreen : TemplateListScreen, NavigationManager.ICanInitalize
public class ProfileListScreen : StandardListScreen//, NavigationManager.ICanInitalize
{
    void Start()
    {
        var navigatorObject = GameObject.Find("Navigator");
        datasource = navigatorObject.GetComponent<ProfileData>();
        listContainerId = "profile_list_items";
        BaseStart();
    }
    void Update()
    {
        BaseUpdate();
    }

    protected override IEnumerable<Dictionary<string, object>> GenerateNavigationActions()
    {
        yield return new Dictionary<string, object> {
            { "buttonName", "Add" },
            { "destinationScreen", "ProfileEditScreen" },
            { "passRecord", false }
        };
        yield return new Dictionary<string, object> {
            { "buttonName", "Edit" },
            { "destinationScreen", "ProfileEditScreen" },
            { "passRecord", true }
        };
        yield return new Dictionary<string, object> {
            { "buttonName", "Open" },
            { "destinationScreen", "RepoListScreen" },
            { "passRecord", true }
        };
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
        var navigateElement = root.Q<Button>("Delete");
        navigateElement.RegisterCallback<ClickEvent>(DeleteConfirm);
        navigateElement = root.Q<Button>("Open");
        navigateElement.RegisterCallback<ClickEvent>(EvtSelectRecord);
    }

    void EvtSelectRecord(ClickEvent evt)
    {
        string selectedProfileName = GetHighlightedItem();
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        appState.Set("selected_profile",selectedProfileName);
        string selectedProfile = (string)appState.Get("selected_profile");
    }

    void DeleteConfirm(ClickEvent evt)
    {
        void DoDelete()
        {
            Debug.Log($"Doing Delete {GetHighlightedItem()}");
            datasource.DeleteRecord(GetHighlightedItem());
            //LoadDatasource();
        }

        void DoCancel()
        {
            Debug.Log("Doing Cancel");
        }
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        navigationManager.NotifyConfirm("Are you sure you want to delete?", DoDelete, DoCancel);
    }


    protected override VisualElement AddToList(DictStrStr rec)
    {
        string name = rec["name"];

        // Create a new instance of the RepoListItem template
        var repoListItem = new VisualElement();
        var repoListItemTemplate = Resources.Load<VisualTreeAsset>("Controls/RepoListItem/ProfileListItem");
        repoListItemTemplate.CloneTree(repoListItem);
        repoListItem.style.height = 40; // TODO TOTAL HACK. 
        // Set the repository attributes (repo_name, repo_status, repo_branch)
        repoListItem.Q<Label>("profile_name").text = name;

        // Add the new item to the repo list container
        listItemContainer.Add(repoListItem);

        var navigatorObject = GameObject.Find("Navigator"); //TODO ew so wasteful. HACK
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        //repoListContainer.Add(repoListItem);
        // Register click event for highlighting
        repoListItem.RegisterCallback<ClickEvent>(evt => OnRepoItemClick(repoListItem, name));
        return repoListItem;
    }
}
