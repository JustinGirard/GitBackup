
/*

Great! Now can you update the RepoListManager so it uses the repo data class. Please add three repos with random names to the repo data class, then list them.
The current RepoData interface, and RepoListManager is attached:


 using System.Collections.Generic;

public interface IRepoData
{
    bool AddRepo(string repoUrl, string branch);
    List<string> ListUser(string username);
    bool RemoveRepo(string repoUrl);
    bool AddUser(string username);
    bool RemoveUser(string username);
    bool AttachUserToRepo(string username, string repoUrl);
    bool RemoveUserFromRepo(string username, string repoUrl);
    List<string> ListUsersOnRepo(string repoUrl);
    Dictionary<string, string> GetRepoInfo(string repoUrl);
    bool StartDownloads(string repoUrl);
    string DownloadStatus(string repoUrl);
    bool StopDownloads(string repoUrl);
    Dictionary<string, Dictionary<string, string>> GetRepoReport(string repoUrl);
    bool GetRepoStatus(string repoUrl);
    string GetRepoLogs(string repoUrl);
}
 


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RepoListManager : MonoBehaviour
{
    private VisualElement repoListContainer;

    // Start is called before the first frame update
    void Start()
    {
        LoadRepos(); // Simulate loading repos at startup
    }

    // Method to load repositories (currently using a simulated list)
    private void LoadRepos()
    {
        // Clear the list of repository items
        repoListContainer = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("repo_list_items");
        repoListContainer.Clear();

        // Example list of repositories (replace this with your JSON loading in the future)
        List<Dictionary<string, string>> repoData = new List<Dictionary<string, string>>()
        {
            new Dictionary<string, string> { { "repo_name", "Repo 3" }, { "repo_status", "Active" }, { "repo_branch", "feature-branch" } }
        };

        // Add each repo to the list
        foreach (var repo in repoData)
        {
            AddRepoToList(repo["repo_name"], repo["repo_status"], repo["repo_branch"]);
        }
    }

    // Method to add a repository entry to the list using the RepoListItem template
    private void AddRepoToList(string repoName, string repoStatus, string repoBranch)
    {
        // Create a new instance of the RepoListItem template
        var repoListItem = new VisualElement();
        var repoListItemTemplate = Resources.Load<VisualTreeAsset>("Controls/RepoListItem/RepoListItem");
        Debug.Log(repoListItemTemplate);
        repoListItemTemplate.CloneTree(repoListItem);

        // Set the repository attributes (repo_name, repo_status, repo_branch)
        repoListItem.Q<Label>("repo_name").text = repoName;
        repoListItem.Q<Label>("repo_status").text = repoStatus;
        repoListItem.Q<Label>("repo_branch").text = repoBranch;

        // Add the new item to the repo list container
        repoListContainer.Add(repoListItem);
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
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("EditRepoScreen"));
        navigateElement = root.Q<Button>("Edit");
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("EditRepoScreen"));
        navigateElement = root.Q<Button>("View");
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("RepoInfoScreen"));
        navigateElement = root.Q<Button>("Visit");
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("RepoInfoScreen"));
    }
}

 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class RepoListScreen : MonoBehaviour, NavigationManager.ICanInitalize
{
    private VisualElement repoListContainer;
    private RepoData repoData;  // Use the IRepoData interface for the RepoData class

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

        // Simulate adding each repo from RepoData to the list UI
        foreach (var repo_name in repoList)
        {
            Debug.Log("Parsing " + repo_name);
            var repoInfo = repoData.GetRepoInfo(repo_name);
            string repoName = repoInfo["name"];
            string repoStatus = repoInfo["status"];
            string repoBranch = repoInfo["branch"];

            // Add each repo to the UI list
            AddRepoToList(repoName, repoStatus, repoBranch);
        }
    }

    // Method to add a repository entry to the list using the RepoListItem template
    private void AddRepoToList(string repoName, string repoStatus, string repoBranch)
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
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("EditRepoScreen"));
        navigateElement = root.Q<Button>("Edit");
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("EditRepoScreen"));
        navigateElement = root.Q<Button>("View");
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("RepoInfoScreen"));
        navigateElement = root.Q<Button>("Visit");
        navigateElement.RegisterCallback<ClickEvent>(evt => navigationManager.NavigateTo("RepoInfoScreen"));
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


