using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public class TemplateListScreen : MonoBehaviour
{
    protected RepoData datasource;  // Use the IRepoData interface for the RepoData class
    protected string highlightedListItem = "";
    protected VisualElement listItemContainer;
    protected string listContainerId = "";

    /// <summary>
    /// STANDARDIZED CODE
    /// 
    /// </summary>
    protected void BaseStart()
    {
        //datasource = navigatorObject.GetComponent<RepoData>();
        //listContainerId = "repo_list_items";
        InitializeRepoData();      // Add some example repos
        LoadDatasource();               // Load and display repos in the UI
    }

    protected virtual IEnumerable<Dictionary<string, object>> GenerateNavigationActions()
    {
        throw new System.Exception("Unimplemented navigation actions");
        // Return a list of actions. Actions have buttonName,destinationScreen,passRecord
        yield return new Dictionary<string, object> {
            { "buttonName", "Visit" },
            { "destinationScreen", "RepoInfoScreen" },
            { "passRecord", true }
        };
        /*
        yield return new Dictionary<string, object> {
            { "buttonName", "Add" },
            { "destinationScreen", "EditRepoScreen" },
            { "passRecord", false }
        };
        yield return new Dictionary<string, object> {
            { "buttonName", "Edit" },
            { "destinationScreen", "EditRepoScreen" },
            { "passRecord", true }
        };
        yield return new Dictionary<string, object> {
            { "buttonName", "View" },
            { "destinationScreen", "RepoInfoScreen" },
            { "passRecord", true }
        };
        yield return new Dictionary<string, object> {
            { "buttonName", "Visit" },
            { "destinationScreen", "RepoInfoScreen" },
            { "passRecord", true }
        };*/
    }
    protected void RegisterStandardEvents()
    {
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        var root = GetComponent<UIDocument>().rootVisualElement;

        foreach (var action in GenerateNavigationActions())
        {
            var buttonName = (string)action["buttonName"];
            if (buttonName.Length == 0)
                throw new System.Exception("buttonName was empty");
            var navigateElement = root.Q<Button>(buttonName);
            if (navigateElement == null)
                throw new System.Exception("navigateElement was empty");

            var destinationScreen = (string)action["destinationScreen"];
            if (destinationScreen.Length == 0)
                throw new System.Exception("destinationScreen was empty");
            bool passRecord = (bool)action["passRecord"];


            if (passRecord)
                navigateElement.RegisterCallback<ClickEvent>(evt => NavigateToWithRecord(evt, destinationScreen));
            else
                navigateElement.RegisterCallback<ClickEvent>(evt => NavigateToWithBlankRecord(evt, destinationScreen));
        }
    }

    protected void NavigateToWithRecord(ClickEvent evt, string targetScreen)
    {
        if (targetScreen.Length == 0)
            throw new System.Exception("No Target Screen selected");

        // TODO Generalize NavigateToWithRecord and NavigateToWithBlankRecord
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        DictStrStr argument = new DictStrStr { { "name", highlightedListItem } };
        Debug.Log($"RepoList sending record {highlightedListItem} to {targetScreen}");
        navigationManager.NavigateToWithRecord(targetScreen, argument);
    }

    protected void NavigateToWithBlankRecord(ClickEvent evt, string targetScreen)
    {
        if (targetScreen.Length == 0)
            throw new System.Exception("No Target Screen selected");

        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        DictStrStr argument = new DictStrStr { };
        Debug.Log($"RepoList sending record '{highlightedListItem}' to {targetScreen}");
        navigationManager.NavigateToWithRecord(targetScreen, argument);
    }

    protected virtual void InitializeRepoData()
    {
        throw new System.Exception("Override me: This is where you initalize your data view");
        //Debug.Log(datasource);
        //datasource.AddRepo("repo1", "https://github.com/user/repo1.git", "main");
        //datasource.AddRepo("repo2", "https://github.com/user/repo2.git", "develop");
        //datasource.AddRepo("repo3", "https://github.com/user/repo3.git", "feature-branch");

    }


    // Method to load repositories and display them in the list
    protected void LoadDatasource()
    {
        // Clear the list of repository items
        listItemContainer = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>(listContainerId);
        listItemContainer.Clear();

        // Retrieve the list of repositories from RepoData
        List<string> repoNameList = datasource.ListRecords(); // Simulated call for listing repos
        List<DictStrStr> repoList = new List<DictStrStr>();
        foreach (var repo_name in repoNameList)
        {
            Debug.Log("Parsing " + repo_name);
            DictStrStr reporec = datasource.GetRecord(repo_name);
            if (reporec!= null) 
                repoList.Add(reporec);
        }

        bool highlighted_found = false;
        foreach (var repoData in repoList)
        {
            if (repoData["name"] == highlightedListItem && highlightedListItem != "")
            {
                highlighted_found = true;
            }
        }
        if (highlighted_found == false)
            highlightedListItem = "";

        // Simulate adding each repo from RepoData to the list UI
        bool first = true;

        Debug.Log($"Printing {this.gameObject.name}");
        foreach (var repoData in repoList)
        {
            Debug.Log("Parsing " + repoData["name"]);
            string repoName = repoData["name"];
            //string repoStatus = repoInfo["status"];
            //string repoBranch = repoInfo["branch"];

            // Add each repo to the UI list
            VisualElement repoListItem = AddToList(repoData);
            if (first == true && highlighted_found == false)
            {
                OnRepoItemClick(repoListItem, repoName);
                first = false;
            }
            if (highlightedListItem == repoName && highlighted_found == true)
            {
                OnRepoItemClick(repoListItem, repoName);
            }
        }
    }



    // Method to add a repository entry to the list using the RepoListItem template
    protected virtual VisualElement AddToList(DictStrStr rec)
    {
        throw new  System.Exception("Not implemented. See an example implementation");
        return new VisualElement();
    }

    // Method to handle the click event and highlight the selected repo
    protected void OnRepoItemClick(VisualElement clickedRepoListItem, string repoName)
    {
        // Remove the 'highlighted_ve' class from all repo list items
        foreach (var repoListItem in listItemContainer.Children())
        {
            repoListItem.RemoveFromClassList("highlighted_ve");
        }

        // Add the 'highlighted_ve' class to the clicked item
        clickedRepoListItem.AddToClassList("highlighted_ve");

        // Store the name of the highlighted repo
        highlightedListItem = repoName;

        Debug.Log($"Repo {repoName} is now highlighted.");
    }

}

public class RepoListScreen : TemplateListScreen, NavigationManager.ICanInitalize
{

    void Start()
    {
        var navigatorObject = GameObject.Find("Navigator");
        datasource = navigatorObject.GetComponent<RepoData>();
        listContainerId = "repo_list_items";

        BaseStart();
    }
    protected override VisualElement AddToList(DictStrStr rec)
    {
        string repoName = rec["name"];
        string repoStatus = rec["status"];
        string repoBranch = rec["branch"];

        // Create a new instance of the RepoListItem template
        var repoListItem = new VisualElement();
        var repoListItemTemplate = Resources.Load<VisualTreeAsset>("Controls/RepoListItem/RepoListItem");
        repoListItemTemplate.CloneTree(repoListItem);
        repoListItem.style.height = 40; // TODO TOTAL HACK. 
        // Set the repository attributes (repo_name, repo_status, repo_branch)
        repoListItem.Q<Label>("repo_name").text = repoName;
        repoListItem.Q<Label>("repo_status").text = repoStatus;
        repoListItem.Q<Label>("repo_branch").text = repoBranch;

        // Add the new item to the repo list container
        listItemContainer.Add(repoListItem);

        var navigatorObject = GameObject.Find("Navigator"); //TODO ew so wasteful. HACK
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        //repoListContainer.Add(repoListItem);
        // Register click event for highlighting
        repoListItem.RegisterCallback<ClickEvent>(evt => OnRepoItemClick(repoListItem, repoName));
        return repoListItem;
    }


    protected override void InitializeRepoData()
    {
        Debug.Log(datasource);
        datasource.AddRepo("repo1","https://github.com/user/repo1.git", "main");
        datasource.AddRepo("repo2","https://github.com/user/repo2.git", "develop");
        datasource.AddRepo("repo3", "https://github.com/user/repo3.git", "feature-branch");

    }

    protected override IEnumerable<Dictionary<string, object>> GenerateNavigationActions()
    {
            yield return new Dictionary<string, object> {
            { "buttonName", "Add" },
            { "destinationScreen", "EditRepoScreen" },
            { "passRecord", false }
        };
            yield return new Dictionary<string, object> {
            { "buttonName", "Edit" },
            { "destinationScreen", "EditRepoScreen" },
            { "passRecord", true }
        };
            yield return new Dictionary<string, object> {
            { "buttonName", "View" },
            { "destinationScreen", "RepoInfoScreen" },
            { "passRecord", true }
        };
            yield return new Dictionary<string, object> {
            { "buttonName", "Visit" },
            { "destinationScreen", "RepoInfoScreen" },
            { "passRecord", true }
        };
    }

    void OnEnable()
    {
        RegisterStandardEvents();
        RegisterDelete();

    }
    void RegisterDelete() {
        /// Special Delete Event
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        var root = GetComponent<UIDocument>().rootVisualElement;
        var navigateElement = root.Q<Button>("Delete");
        navigateElement.RegisterCallback<ClickEvent>(DeleteConfirm);

    }

    void DeleteConfirm(ClickEvent evt)
    {
        void DoDelete()
        {
            Debug.Log($"Doing Delete {highlightedListItem}");
            datasource.DeleteRecord(highlightedListItem);
            LoadDatasource();
        }

        void DoCancel()
        {
            Debug.Log("Doing Cancel");
        }
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        navigationManager.NotifyConfirm("Are you sure you want to delete?", DoDelete, DoCancel);
    }


    //  NavigationManager.ICanInitalize
    public bool SetAction(string actionLabel, System.Action action)
    {
        return false;
    }


    public void InitData(Dictionary<string, string> dataframe)
    {
        //throw new System.NotImplementedException();
    }

    public void Refresh()
    {
        Debug.Log("Calling Refresh()->LoadRepos()");
        LoadDatasource();
        //throw new System.NotImplementedException();
    }

}


