using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System;

using UnityEngine;
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
using Newtonsoft.Json;
using System.Collections;
using Unity.VisualScripting;

public class RepoGithubData : StandardData
{
    ProfileData profileDatasource;  // Use the IRepoData interface for the RepoData class
    bool __loadMore = true;
    int __limit = 50;
    int __offset = 0;

    int __totalLoaded = 0;
    //    SetStatusLabel("linking new user");

    public void Awake()
    {
        void OnUserChanged(object newValue)
        {
            __loadMore = true;
            __limit = 5;
            __offset = 0;

            ClearRecords();
            __totalLoaded = 0;
            CheckForRepoData();
        }
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        appState.RegisterChangeCallback("selected_profile", OnUserChanged);
        appState.RegisterChangeCallback("system_settings_updated", OnUserChanged);
        SetStatusLabel($"no attached user");     

    }



    public bool AddRepo(string name, string repoUrl, string branch)
    {
        // TODO -- DEAD CODE?!?!
        throw new Exception("THis is dead code");
        Debug.Log("Adding Repo!!!");
        if (GetRecord(name) != null)
            __totalLoaded = __totalLoaded + 1;
        SetStatusLabel($"total loaded {__totalLoaded}");
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

        // A Recursive Job Example
        if (profileDatasource == null)
        {
            Debug.Log("RepoGithubData: LoadData halted; profileDatasource is null");
            return;
        }
        // Set Python Root
        var navigatorObject = GameObject.Find("Navigator");
        SetupData setDat = navigatorObject.GetComponent<SetupData>();
        if (setDat.IsPythonReady() == false)
        {
            Debug.Log("CheckForRepoData Cant run, because system is not configured yet.");
            return;
        }
        string venvPythonPath = setDat.GetPythonRoot();


        var appState = navigatorObject.GetComponent<ApplicationState>();
        string selectedProfileName = (string)appState.Get("selected_profile");
        if (selectedProfileName == null)
        {
            Debug.Log("RepoGithubData LoadData halted; selectedProfileName is null");
            return;
        }
        // Load User
        DictStrStr rec = profileDatasource.GetRecord(selectedProfileName);
        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return;
        }

        
        if (__loadMore == false)
        {
            return;
        }
        string tempCacheFile = System.IO.Path.Combine(Application.temporaryCachePath, "github_repos_cache.json");
        // Build Command 
        var prnt = new DictTable {{"output",rec}};
        //Debug.Log(ShellRun.BuildJsonFromDictTable(prnt));
        string[] command = new string[] {venvPythonPath,"propagator/datasource/UtilGit.py","list_github_repos" };
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
            workingDirectory:setDat.GetPythonWorkDir(),
            onSuccess: SaveRepos,
            readProgress: () =>{
                return $"running list_github_repos {__offset.ToString()}";
            },
            onProgress: (object obj) =>{
                
            },
            onFailure: (error) =>
            {
                Debug.LogError($"Failed to List Repositories err:"+((System.Exception)error).ToString());
            },
            debugMode:false,
            parentId:"check_repo"
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
                __totalLoaded = GetRecords().Count;
                SetStatusLabel($"total_indexed {__totalLoaded}");
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


    public IEnumerator DownloadAllCoroutine(List<string> repoNames)
    {
        foreach (var repoName in repoNames)
        {
            if (!ContainsRecord(repoName))
            {
                throw new System.Exception($"!{repoName} is missing from the repos. Some strange error with repos");
            }

            DictStrStr repoRecord = GetRecord(repoName);
            SetRecordField(repoName, "download_status", "downloading");

            // Use a flag to track completion status
            bool isCompleted = false;
            bool isSuccessful = false;

            // Create the download task
            Func<Task> tsk = CreateDownloadRepoTask(
                repoRecord,
                onSuccess: (obj) =>
                {
                    Debug.Log($"Finished Download of {repoName}");
                    isSuccessful = true;
                    isCompleted = true;
                },
                onFailure: (err) =>
                {
                    Debug.Log($"Failed Download of {repoName}");
                    isSuccessful = false;
                    isCompleted = true;
                },
                debugMode: false
            );

            Debug.Log($"Starting download for {repoName}");
            var task = Task.Run(tsk);
            while (!task.IsCompleted)
            {
                yield return null; 
            }
            if (isSuccessful)
            {
                Debug.Log($"Downloaded successfully: {repoName}");
            }
            else
            {
                Debug.Log($"Download failed: {repoName}");
            }
        }

    }

    /*
   void DownloadAllNew(List<string> repoNames)
    {
        bool procDoCancel = false;
        DateTime lastHeartbeat = DateTime.UtcNow;
        TaskCompletionSource<bool> taskStarted = new TaskCompletionSource<bool>();
        // Define the task function
        Func<Task<object>> longRunningTask = async () =>
        {
            foreach (var repoName in repoNames)
            {
                taskStarted.SetResult(true);                
                SetRecordField(repoName,"download_status", "downloading");
                DictStrStr repoRecord = GetRecord(repoName);
                NonBlockingDownloadRepo(repoRecord,
                    onSuccess: (obj) => {

                        
                    },
                    onFailure: (err) => {}
                ); 
                if(procDoCancel == true)
                    return new DictStrStr() {{"error","cancelled"}};

            }
        };

        Func<bool> isRunning = () =>
        {
            TimeSpan timeSinceLastHeartbeat = DateTime.UtcNow - lastHeartbeat;
            return timeSinceLastHeartbeat.TotalSeconds < 2;
        };

        Func<bool> cancel = () =>
        {
            procDoCancel = true;
            return true;
        };

        Action<object> onSuccess = (object obj) => Debug.Log("Long-running task completed successfully.");
        Action<object> onFailure = (object obj) => Debug.LogError($"Long-running task failed: ");

        Func<Task> jobifiedTask = JobUtils.CreateJobifiedFunc(
            id:id,
            jobName: "LongTask",
            taskFunc: longRunningTask,
            isRunning: isRunning,
            cancel: cancel,
            onSuccess: onSuccess,
            onFailure: onFailure,
            debugMode: false
        );

        Task.Run(jobifiedTask);
        taskStarted.Task.Wait(5000);
    }    */
    
    
    

    // Example usage for downloading a repository
    private  Func<Task> CreateDownloadRepoTask(DictStrStr repoData,System.Action<object> onSuccess=null, System.Action<object> onFailure=null,bool debugMode=false)
    {
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        RepoData repoDatabase = navigatorObject.GetComponent<RepoData>();
        string selectedProfileName = (string)appState.Get("selected_profile");
        DictStrStr rec = profileDatasource.GetRecord(selectedProfileName);

        SetupData setDat = navigatorObject.GetComponent<SetupData>();
        if (setDat.IsPythonReady() == false)
        {
            Debug.Log("CreateDownloadRepoTask Cant run, because system is not configured yet.");
            return null;
        }
        string venvPythonPath = setDat.GetPythonRoot();

        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return null;
        }

        // Prepare the CLI command arguments
        string repoUrl = repoData["html_url"];
        string branch = repoData["branch"];
        string[] command = new string[]
        {
            venvPythonPath,
            "propagator/GitBackupManager.py",
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


        // Call the generalized shell execution method
        Debug.Log($"{this.ToString()}: TEMP OUTER Starting Shell Download {repoData["name"]}");
        Func<Task> tsk = JobUtils.CreateJobifiedShellTask(
            jobName: $"DownloadRepo_{repoData["name"]}",
            command: command,
            arguments: arguments,
            readProgress:() => {
                 return "running download repo task";
            
            },
            onProgress: (object progressInt) => {},
            isNamedArguments:true,
            workingDirectory: setDat.GetPythonWorkDir(),
            onSuccess: (output) =>
            {
                Debug.Log($"TEMP FINAL TEMP Successfully downloaded {repoData["name"]}");
                SetRecordField(repoData["name"], "download_status", "finished");
                Debug.Log($"TEMP FINAL Successfully downloaded {repoData["name"]}");
                //repoDatabase.UpdateDataRevision();
                repoDatabase.ReloadData();
                onSuccess?.Invoke(output);
            },
            onFailure: (error) =>
            {
                Debug.Log($"TEMP FINAL FAILED downloaded {repoData["name"]}");
                SetRecordField(repoData["name"], "download_status", "failed");
                Debug.LogError($"TEMP FINAL Failed to download {repoData["name"]}: {((System.Exception)error).ToString()}");
                onFailure?.Invoke(error);
            },
            debugMode:debugMode,
            parentId:"download"
        );
        return tsk;
    }


}
