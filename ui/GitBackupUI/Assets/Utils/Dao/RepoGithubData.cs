using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System;

using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using Newtonsoft.Json;
using System.Collections;
using Unity.VisualScripting;
using System.Threading;
using System.Net.Http.Headers;

public class RepoGithubData : StandardData
{
    ProfileData profileDatasource;  // Use the IRepoData interface for the RepoData class
    bool __loadMore = true;
    int __limit = 20;
    int __offset = 0;

    int __totalLoaded = 0;
    //    SetStatusLabel("linking new user");

    public void Awake()
    {
        void OnUserChanged(string key,object newValue)
        {
            __loadMore = true;
            __limit = 20;
            __offset = 0;
            if (key == "selected_profile")
                SetStatusLabel($"attached user {newValue}");   

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
        return SetRecord( new DictStrObj
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
        DictStrObj rec = profileDatasource.GetRecord(selectedProfileName);
        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return;
        }

        
        if (__loadMore == false)
        {
            return;
        }
        string tempCacheFile = System.IO.Path.Combine(Application.temporaryCachePath, $"{selectedProfileName}_github_repos_cache.json");
        // Build Command 
        var prnt = new DictObjTable {{"output",rec}};
        //Debug.Log(ShellRun.BuildJsonFromDictTable(prnt));
        string set_limit = __limit.ToString();
        string set_offset = __offset.ToString();
        string[] command = new string[] {venvPythonPath,"propagator/datasource/UtilGit.py","list_github_repos" };
        DictStrObj arguments =new DictStrObj {
            {"username",rec["username"]},
            {"access_token",rec["access_key"]},
            {"cache_file",tempCacheFile},
            {"limit",set_limit.ToString()},
            {"offset",set_offset.ToString()}
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
                return $"running list_github_repos {set_offset},{set_limit}";
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
            GameObject navigatorObject = GameObject.Find("Navigator");
            SetupData setDat = navigatorObject.GetComponent<SetupData>();     
            RepoData repoDatabase = navigatorObject.GetComponent<RepoData>();           

            Debug.Log("LOADING REMOTE REPOS");
            Debug.Log((string)rawJsonString);
            //List<object> jsonObj = (List<object> )JsonParser.ParseJsonObjects((string)rawJsonString);
            //List<object> jsonObj = (List<object> )DJson.ParseGeneric((string)rawJsonString);
            var jsonData = DJson.ParseGeneric((string)rawJsonString);

            string jsonString = JsonConvert.SerializeObject(jsonData, Formatting.Indented);
            if ( jsonData is Dictionary<string,object>)
            {
                Debug.LogWarning(" Got an invalid object back from  github");
                Debug.Log(jsonString);
                NavigationManager navInst = navigatorObject.GetComponent<NavigationManager>();    
                navInst.NotifyError($"Critical: Could not load repositories due to error:{jsonString}");
                return;
                /*
                navInst.NavigateToWithRecord(
                                    "NotificationScreen",
                                    new DictStrObj{{"message",$"Could not load repositories due to error:{jsonString}"}},
                                    false);*/

            }
            List<object> jsonObj = (List<object>)jsonData;
            if ((jsonObj as List<object>).Count <= 0)
            {
                Debug.Log("NO REPOS LOADED FROM A QUERY. COULD BE FINISHED?");
                return;
            }
            jsonString = JsonConvert.SerializeObject(jsonObj[0], Formatting.Indented);
            //Debug.Log(jsonString);
            __loadMore = false;

            DictObjTable currentLocalRepos =                 repoDatabase.GetRecords();


            foreach (Dictionary<string,object> folder in jsonObj)
            {
                //Debug.Log("Storing record :"+(string)folder["name"]);
                string[] keys =  new string[] { "name", "html_url", "clone_url", "ssh_url", "latest_commit_hash", "latest_download_datetime", "branch" };
                DictStrObj rec = new DictStrObj();
                foreach (string key in keys) { DJson.SafeCopyKey( key:key, dest:rec,src:folder);    }
                /*
                DictStrStr rec = new DictStrStr
                {
                    { "name", (string)folder["name"] },
                    { "download_status", "undefined" },
                    { "html_url", (string)folder["html_url"] },
                    { "clone_url", (string)folder["clone_url"] },
                    { "ssh_url", (string)folder["ssh_url"] },
                    { "latest_commit_hash", (string)folder["latest_commit_hash"] },
                    { "last_download_datetime", (string)folder["last_download_datetime"] },
                    { "branch", (string)folder["branch"] } // Default status on creation
                    //{ "latest_commit_hash", (string)folder["latest_commit_hash"] } // Default status on creation
                };*/
                
                //Debug.Log(DJson.Stringify(rec));                
                SetRecord(rec);
                if( currentLocalRepos.ContainsKey((string)rec["name"]))
                {
//                    Debug.Log($"Loading Record into RepoData {(string)rec["name"]}");
                    repoDatabase.SetRecordField((string)folder["name"],"gh_latest_commit_hash",(string)rec["latest_commit_hash"] );
                    repoDatabase.SetRecordField((string)folder["name"],"gh_latest_download_datetime",(string)rec["latest_download_datetime"]);
                }
                
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
    public void AppEnqueue(System.Action act)
    {
        ApplicationState.Instance().Enqueue(act);

    }
    public void RunTaskDownloadAll(List<string> repoNames,bool debugMode, bool downloadDebugMode)
    {
        GameObject navigatorObject = GameObject.Find("Navigator");
        SetupData setDat = navigatorObject.GetComponent<SetupData>();     
        RepoData repoDatabase = navigatorObject.GetComponent<RepoData>();           
        int rindex = 0;
        Func<Task> tsk = JobUtils.CreateStepJob(jobId:$"dl_allrepo", jobName:$"dl_allrepo",
        stepJob:async (JobResult jobResult) =>
        {
            /// Handle Standard Job Updates & termination
            jobResult.SetProgress( rindex/repoNames.Count,"ran iteration normally");
            if (jobResult.ReadCancel())
            {
                if (debugMode)
                    Debug.Log($"Download all: Finished - Cancel flag");
                return -1; 
            }
            if(rindex >= repoNames.Count) 
            {
                if (debugMode)
                    Debug.Log($"Download all: Finished all jobs as index {rindex} of {repoNames.Count}");

                return 1;    
            }
            ApplicationState.Instance().Enqueue(() => Debug.Log($"download_repos is at {rindex.ToString()}"));

            // Load Next Download Record
            string repoName = repoNames[rindex];
            DictStrObj repoRecord = GetRecord(repoName);
            SetRecordField(repoName, "download_status", "downloading");

            bool downloadSuccess = false;

            // Download The Record
            Func<Task> tsk = CreateDownloadRepoTask(
                repoDatabase,
                setDat,
                repoRecord,
                onSuccess: (obj) => {downloadSuccess = true; Debug.Log($"Inner: Finished Download of {repoName}");},
                onFailure: (err) => { downloadSuccess = false; Debug.Log($"Failed Download of {repoName}");},
                debugMode: downloadDebugMode
            );
            
            // Run 
            if (debugMode)
               ApplicationState.Instance().Enqueue(() =>  Debug.Log($"Download all: BLOCKING ON download for {repoName} as index {rindex} of {repoNames.Count}"));
            await Task.Run(tsk);
            if (debugMode)
                ApplicationState.Instance().Enqueue(() => Debug.Log($"Download all: FINISHED download for {repoName}"));

            // Save status. iterate.
            if (downloadSuccess == true)
                SetRecordField(repoName, "download_status", "finished");
            else
                SetRecordField(repoName, "download_status", "failed");
            rindex ++;
            return null;   // Returning null means Keep going     

        },debugMode:debugMode);
        Task.Run(tsk);
    }


    // Example usage for downloading a repository
    private  Func<Task> CreateDownloadRepoTask(RepoData repoDatabase,SetupData setDat , DictStrObj repoData,System.Action<object> onSuccess=null, System.Action<object> onFailure=null,bool debugMode=false)
    {
        ApplicationState appState = ApplicationState.Instance();
        string selectedProfileName = (string)appState.Get("selected_profile");
        DictStrObj rec = profileDatasource.GetRecord(selectedProfileName);
        if (setDat.IsPythonReady() == false)
        {
           ApplicationState.Instance().Enqueue(() => Debug.Log("CreateDownloadRepoTask Cant run, because system is not configured yet."));
            return null;
        }
        string venvPythonPath = setDat.GetPythonRoot();

        if (rec == null)
        {
            ApplicationState.Instance().Enqueue(() =>Debug.Log("Could not find target user"));
            return null;
        }

        // Prepare the CLI command arguments
        string repoUrl = (string)repoData["html_url"];
        string branch = (string)repoData["branch"];
        string[] command = new string[]
        {
            venvPythonPath,
            "propagator/GitBackupManager.py",
            "download"
        };

        DictStrObj arguments = new DictStrObj
        {
            { "git_username", rec["username"] },
            { "git_access_key", rec["access_key"] },
            { "backup_path", Path.Combine((string)rec["path"], (string)repoData["name"]) },
            { "encryption_password", rec["encryption_password"] },
            { "repo_url", repoUrl },
            { "branch", branch }
        };


        // Call the generalized shell execution method
        ApplicationState.Instance().Enqueue(() =>Debug.Log($"2++{this.ToString()}: TEMP OUTER Starting Shell Download {repoData["name"]}"));
        Func<Task> tsk = JobUtils.CreateJobifiedShellTask(
            jobName: $"dl_repo_{repoData["name"]}",
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
                Debug.Log($"2++TEMP FINAL TEMP Successfully downloaded {repoData["name"]}");
                SetRecordField((string)repoData["name"], "download_status", "finished");
                Debug.Log($"2++TEMP FINAL Successfully downloaded {repoData["name"]}");
                //repoDatabase.UpdateDataRevision();
                repoDatabase.ReloadData();
                onSuccess?.Invoke(output);
            },
            onFailure: (error) =>
            {
                Debug.Log($"2++TEMP FINAL FAILED downloaded {repoData["name"]}");
                SetRecordField((string)repoData["name"], "download_status", "failed");
                Debug.LogError($"2++TEMP FINAL Failed to download {repoData["name"]}: {((System.Exception)error).ToString()}");
                onFailure?.Invoke(error);
            },
            debugMode:debugMode,
            parentId:"dl_allrepo"
        );
        return tsk;
    }


}
