using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System;

using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;

public class RepoGithubData : StandardData
{
    ProfileData profileDatasource;  // Use the IRepoData interface for the RepoData class
    bool __loadMore = true;
    int __limit = 50;
    int __offset = 0;

    public void Awake()
    {
        void OnUserChanged(object newValue)
        {
            Debug.Log("Changed Profile: Clearing repos");
            __loadMore = true;
            __limit = 5;
            __offset = 0;

            ClearRecords();

            Debug.Log("Changed Profile: CheckForRepoData");
            CheckForRepoData();
        }
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        appState.RegisterChangeCallback("selected_profile", OnUserChanged);


    }


    public bool AddRepo(string name, string repoUrl, string branch)
    {
        return SetRecord( new DictStrStr
        {
            { "name", name },
            { "url", repoUrl },
            { "branch", branch },
            { "status", "Active" },
            { "last_updated", "Never" } // Default status on creation
        });
    }
    public void BindToProfileData(ProfileData profileData)
    {
        profileDatasource = profileData;

    }
    public void CheckForRepoData()
    {
        if (profileDatasource == null)
        {
            Debug.Log("RepoGithubData: LoadData halted; profileDatasource is null");
            return;
        }
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        string selectedProfileName = (string)appState.Get("selected_profile");
        if (selectedProfileName == null)
        {
            Debug.Log("RepoGithubData LoadData halted; selectedProfileName is null");
            return;
        }
        Debug.Log("*****> RepoGithubData: Doing Hard Reload.");
        // Load User
        DictStrStr rec = profileDatasource.GetRecord(selectedProfileName);
        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return;
        }

        
        //Debug.Log("--------------------Running Command:");
        //Debug.Log("--------------------Running Command:");
        //Debug.Log("--------------------Running Command:");
        //Debug.Log( ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[0]);
        //Debug.Log( ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[1]);      
        //ShellRun.Response r = ShellRun.RunCommand( command, arguments,  isNamedArguments,  workingDirectory );
        if (__loadMore == false)
        {
            Debug.Log("Finished Loading all Repos");
            return;
        }
        string tempCacheFile = System.IO.Path.Combine(Application.temporaryCachePath, "github_repos_cache.json");
        // Build Command 
        var prnt = new DictTable {{"output",rec}};
        //Debug.Log(ShellRun.BuildJsonFromDictTable(prnt));
        string[] command = new string[] { "/Users/computercomputer/justinops/propagator/propenv/bin/python3","UtilGit.py","list_github_repos" };
        DictStrStr arguments =new DictStrStr {
            {"username",rec["username"]},
            {"access_token",rec["access_key"]},
            {"cache_file",tempCacheFile},
            {"limit",__limit.ToString()},
            {"offset",__offset.ToString()}
        };
        bool isNamedArguments = true; 

        Func<Task> tsk = JobUtils.CreateJobifiedShellTask(
            jobName: $"list_github_repos",
            command: command,
            arguments: arguments,
            isNamedArguments:isNamedArguments,
            workingDirectory: "/Users/computercomputer/justinops/propagator/datasource",
            onSuccess: SaveRepos,
            onFailure: (error) =>
            {
                Debug.LogError($"Failed to List Repositories err:"+((System.Exception)error).ToString());
            },
            debugMode:true
        );
        Task.Run(tsk);

        void SaveRepos(object rawJsonString)
        {
            //Debug.Log("LOADING REMOTE REPOS");
            List<object> jsonObj = (List<object> )JsonParser.ParseJsonObjects((string)rawJsonString);
            string jsonString = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            //Debug.Log(jsonString);
            if (jsonObj.Count <= 0)
            {
                Debug.Log("Finished Loading Repos");
                return;
            }
            jsonString = JsonConvert.SerializeObject(jsonObj[0], Formatting.Indented);
            //Debug.Log(jsonString);
            __loadMore = false;
            foreach (Dictionary<string,object> folder in jsonObj)
            {
                //Debug.Log("Storing record :"+(string)folder["name"]);
                SetRecord( new DictStrStr
                {
                    { "name", (string)folder["name"] },
                    { "download_status", "undefined" },
                    { "html_url", (string)folder["html_url"] },
                    { "clone_url", (string)folder["clone_url"] },
                    { "ssh_url", (string)folder["ssh_url"] },
                    { "branch", (string)folder["branch"] } // Default status on creation
                });
                Debug.Log("Loading More Repos");
                //Debug.Log("FINISHED REMOTE REPOS");
                __loadMore = true;
                __offset = __offset + __limit;
            }
            CheckForRepoData();            
        }

    }


    public override void AfterSaveData()
    {
        //throw new System.Exception("Can not 'SaveData' in RepoGithubData");
    }    


    // DownloadAll function to download repositories concurrently in the background
    public void DownloadAll(List<string> repoNames)
    {      
        foreach (var repoName in repoNames)
        {
            // Make sure it is on the server
            if (!ContainsRecord(repoName))
                throw new System.Exception($"!{repoName} is missing from the repos. Some strange error with repos");
            DictStrStr repoRecord = GetRecord(repoName);
            SetRecordField(repoName,"download_status", "downloading");
            NonBlockingDownloadRepo(repoRecord); 
        }
    }

    // Example usage for downloading a repository
    private void NonBlockingDownloadRepo(DictStrStr repoData)
    {
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        RepoData repoDatabase = navigatorObject.GetComponent<RepoData>();
        string selectedProfileName = (string)appState.Get("selected_profile");
        DictStrStr rec = profileDatasource.GetRecord(selectedProfileName);
        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return;
        }

        // Prepare the CLI command arguments
        string repoUrl = repoData["html_url"];
        string branch = repoData["branch"];
        string[] command = new string[]
        {
            "/Users/computercomputer/justinops/propagator/propenv/bin/python3",
            "GitBackupManager.py",
            "download"
        };

        DictStrStr arguments = new DictStrStr
        {
            { "git_username", rec["username"] },
            { "git_access_key", rec["access_key"] },
            { "backup_path", Path.Combine(rec["path"], repoData["name"]) },
            { "encryption_password", rec["encryption_password"] },
            { "repo_url", repoUrl },
            { "branch", branch }
        };

        //Debug.Log("--------------------Running Download Command:");
        //Debug.Log(ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] +
        //        ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
        //Debug.Log($"Downloading repository: {repoData["name"]}");

        // Call the generalized shell execution method
        Debug.Log($"{this.ToString()}: TEMP OUTER Starting Shell Download {repoData["name"]}");
        Func<Task> tsk = JobUtils.CreateJobifiedShellTask(
            jobName: $"DownloadRepo_{repoData["name"]}",
            command: command,
            arguments: arguments,
            isNamedArguments:true,
            workingDirectory: "/Users/computercomputer/justinops/propagator",
            onSuccess: (output) =>
            {
                Debug.Log($"TEMP FINAL TEMP Successfully downloaded {repoData["name"]}");
                SetRecordField(repoData["name"], "download_status", "finished");
                Debug.Log($"TEMP FINAL Successfully downloaded {repoData["name"]}");
                //repoDatabase.UpdateDataRevision();
                repoDatabase.ReloadData();
            },
            onFailure: (error) =>
            {
                Debug.Log($"TEMP FINAL FAILED downloaded {repoData["name"]}");
                SetRecordField(repoData["name"], "download_status", "failed");
                Debug.LogError($"TEMP FINAL Failed to download {repoData["name"]}: {((System.Exception)error).ToString()}");
            },
            debugMode:true
        );
        Debug.Log($"{this.ToString()}: TEMP OUTER Starting Shell Download {repoData["name"]}");
        Task.Run(tsk);
    }


}
