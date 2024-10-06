using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public class RepoListScreen : MonoBehaviour, NavigationManager.ICanInitalize
{
    private VisualElement repoListContainer;
    private RepoData repoData;  // Use the IRepoData interface for the RepoData class
    private string highlightedRepoName = "";

    // Start is called before the first frame update
    void Start()
    {
        var navigatorObject = GameObject.Find("Navigator");
        repoData = navigatorObject.GetComponent<RepoData>();
        InitializeRepoData();      // Add some example repos
        LoadRepos();               // Load and display repos in the UI
    }

    // Method to initialize RepoData with some example repositories
    private void InitializeRepoData()
    {
        Debug.Log(repoData);
        repoData.AddRepo("repo1","https://github.com/user/repo1.git", "main");
        repoData.AddRepo("repo2","https://github.com/user/repo2.git", "develop");
        repoData.AddRepo("repo3", "https://github.com/user/repo3.git", "feature-branch");

    }

    // Method to load repositories and display them in the list
    private void LoadRepos()
    {
        // Clear the list of repository items
        repoListContainer = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("repo_list_items");
        repoListContainer.Clear();

        // Retrieve the list of repositories from RepoData
        List<string> repoList = repoData.ListRepos(); // Simulated call for listing repos
        bool highlighted_found = false;
        foreach (var repo_name in repoList)
        {
            if (repo_name == highlightedRepoName && highlightedRepoName != "")
            {
                highlighted_found = true;
            }
        }
        if (highlighted_found == false)
            highlightedRepoName = "";

            // Simulate adding each repo from RepoData to the list UI
        bool first = true;
        foreach (var repo_name in repoList)
        {
            Debug.Log("Parsing " + repo_name);
            var repoInfo = repoData.GetRepoInfo(repo_name);
            string repoName = repoInfo["name"];
            string repoStatus = repoInfo["status"];
            string repoBranch = repoInfo["branch"];

            // Add each repo to the UI list
            VisualElement repoListItem = AddRepoToList(repoName, repoStatus, repoBranch);
            if (first == true && highlighted_found == false)
            {
                OnRepoItemClick(repoListItem, repoName);
                first = false;
            }
            if (highlightedRepoName == repoName && highlighted_found == true)
            {
                OnRepoItemClick(repoListItem, repoName);
            }
        }
    }

    // Method to add a repository entry to the list using the RepoListItem template
    private VisualElement AddRepoToList(string repoName, string repoStatus, string repoBranch)
    {
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
        repoListContainer.Add(repoListItem);

        var navigatorObject = GameObject.Find("Navigator"); //TODO ew so wasteful. HACK
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        //repoListContainer.Add(repoListItem);
        // Register click event for highlighting
        repoListItem.RegisterCallback<ClickEvent>(evt => OnRepoItemClick(repoListItem, repoName));
        return repoListItem;
    }

    // Method to handle the click event and highlight the selected repo
    private void OnRepoItemClick(VisualElement clickedRepoListItem, string repoName)
    {
        // Remove the 'highlighted_ve' class from all repo list items
        foreach (var repoListItem in repoListContainer.Children())
        {
            repoListItem.RemoveFromClassList("highlighted_ve");
        }

        // Add the 'highlighted_ve' class to the clicked item
        clickedRepoListItem.AddToClassList("highlighted_ve");

        // Store the name of the highlighted repo
        highlightedRepoName = repoName;

        Debug.Log($"Repo {repoName} is now highlighted.");
    }
    void OnEnable()
    {
        // Find the Navigator object and the NavigationManager component
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        // Get the root of the current UIDocument
        var root = GetComponent<UIDocument>().rootVisualElement;
        // Register navigation callbacks for buttons
        var navigateElement = root.Q<Button>("Add");
        navigateElement.RegisterCallback<ClickEvent>(evt => NavigateToWithBlankRecord(evt, "EditRepoScreen"));
        navigateElement = root.Q<Button>("Edit");
        navigateElement.RegisterCallback<ClickEvent>(evt => NavigateToWithRecord(evt, "EditRepoScreen"));
        navigateElement = root.Q<Button>("View");
        navigateElement.RegisterCallback<ClickEvent>(evt => NavigateToWithRecord(evt, "RepoInfoScreen"));
        navigateElement = root.Q<Button>("Visit");
        navigateElement.RegisterCallback<ClickEvent>(evt => NavigateToWithRecord(evt, "RepoInfoScreen"));
        navigateElement = root.Q<Button>("Delete");
        navigateElement.RegisterCallback<ClickEvent>(DeleteConfirm);

    }
    void DoDelete()
    {
        Debug.Log($"Doing Delete {highlightedRepoName}");
        repoData.DeleteRepo(highlightedRepoName);
        LoadRepos();

    }

    void DoCancel()
    {
        Debug.Log("Doing Cancel");

    }

    public bool SetAction(string actionLabel, System.Action action)
    {
        // Does not support programmable buttons
        return false;
    }

    void DeleteConfirm(ClickEvent evt)
    {
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();
        navigationManager.NotifyConfirm("Are you sure you want to delete?", DoDelete, DoCancel);
    }


    void NavigateToWithRecord(ClickEvent evt, string targetScreen) {
        // TODO Generalize NavigateToWithRecord and NavigateToWithBlankRecord
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        DictStrStr argument = new DictStrStr { { "name", highlightedRepoName } };
        Debug.Log($"RepoList sending record {highlightedRepoName} to {targetScreen}" );
        navigationManager.NavigateToWithRecord(targetScreen, argument);
    }
    void NavigateToWithBlankRecord(ClickEvent evt, string targetScreen)
    {
        var navigatorObject = GameObject.Find("Navigator");
        var navigationManager = navigatorObject.GetComponent<NavigationManager>();

        DictStrStr argument = new DictStrStr { };
        Debug.Log($"RepoList sending record {highlightedRepoName} to {targetScreen}");
        navigationManager.NavigateToWithRecord(targetScreen, argument);
    }


    public void InitData(Dictionary<string, string> dataframe)
    {
        //throw new System.NotImplementedException();
    }

    public void Refresh()
    {
        Debug.Log("Calling Refresh()->LoadRepos()");
        LoadRepos();
        //throw new System.NotImplementedException();
    }
}


