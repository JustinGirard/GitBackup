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

public class RepoGithubData : StandardData
{
    ProfileData profileDatasource;  // Use the IRepoData interface for the RepoData class
    public void Awake()
    {
        void OnValueChanged(object newValue)
        {
            ReloadData();
        }
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        appState.RegisterChangeCallback("selected_profile", OnValueChanged);


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
    public void ReloadData()
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
        Debug.Log("RepoGithubData: Doing Hard Reload.");
        // Load User
        DictStrStr rec = profileDatasource.GetRecord(selectedProfileName);
        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return;
        }


        // Build Command 
        var prnt = new DictTable {{"output",rec}};
        //Debug.Log(ShellRun.BuildJsonFromDictTable(prnt));
        string[] command = new string[] { "/Users/computercomputer/justinops/propagator/propenv/bin/python3","UtilGit.py","list_github_repos" };
        DictStrStr arguments =new DictStrStr {
            {"username",rec["username"]},
            {"access_token",rec["access_key"]},
            {"limit","3"},
            {"offset","0"}
        };
        bool isNamedArguments = true; 
        
        //Debug.Log("--------------------Running Command:");
        //Debug.Log("--------------------Running Command:");
        //Debug.Log("--------------------Running Command:");
        //Debug.Log( ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[0]);
        //Debug.Log( ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[1]);      


        //ShellRun.Response r = ShellRun.RunCommand( command, arguments,  isNamedArguments,  workingDirectory );

        Func<Task> tsk = CreateShellTask(
            jobName: $"list_github_repos",
            command: command,
            arguments: arguments,
            isNamedArguments:isNamedArguments,
            workingDirectory: "/Users/computercomputer/justinops/propagator/datasource",
            onSuccess: (output) =>
            {
                //Debug.Log("LOADING REMOTE REPOS");
                List<object> jsonObj = (List<object> )JsonParser.ParseJsonObjects(output.Output);
                string jsonString = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                //Debug.Log(jsonString);
                jsonString = JsonConvert.SerializeObject(jsonObj[0], Formatting.Indented);
                //Debug.Log(jsonString);
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
                    //Debug.Log("FINISHED REMOTE REPOS");
   
                }
            },
            onFailure: (error) =>
            {
                Debug.LogError($"Failed to List Repositories err:"+error.Error);
            }
        );
        Task.Run(tsk);




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
    /*
    // Helper function to download a single repository using the new CLI command
    private async Task<bool> DownloadRepo(DictStrStr repoData)
    {
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        string selectedProfileName = (string)appState.Get("selected_profile");        
        DictStrStr rec = profileDatasource.GetRecord(selectedProfileName);
        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return false;
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
            { "git_access_key",  rec["access_key"]  },
            { "backup_path", Path.Combine(rec["path"], repoData["name"])  },
            { "encryption_password",  rec["encryption_password"] },
            { "repo_url", repoUrl },
            { "branch", branch },
        };
        //--
        Debug.Log("--------------------Running Download Command:");
        Debug.Log( ShellRun.BuildCommandArguments(command, arguments, isNamedArguments:true)[0]);
        Debug.Log( ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);      
        Debug.Log($"Downloading repository: {repoData["name"]}");
        string workingDirectory = "/Users/computercomputer/justinops/propagator";        
        ShellRun.Response response = ShellRun.RunCommand(command, arguments, isNamedArguments: true, workingDirectory);
        //Debug.Log("Some output:--------------"+response.Output);
        // Debug.Log(r.Error);        
        //--

        if (!string.IsNullOrEmpty(response.Error))
        {
            Debug.LogError($"Failed to download {repoData["name"]}: {response.Error}");
            return false;
        }

        Debug.Log($"Successfully downloaded {repoData["name"]}");
        return true;
    }*/
// Updated method to download a repository using JobData
    /*
    private void NonBlockingDownloadRepo(DictStrStr repoData)
    {
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
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
            { "git_access_key",  rec["access_key"]  },
            { "backup_path", Path.Combine(rec["path"], repoData["name"])  },
            { "encryption_password",  rec["encryption_password"] },
            { "repo_url", repoUrl },
            { "branch", branch },
        };

        Debug.Log("--------------------Running Download Command:");
        Debug.Log(ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] + ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
        Debug.Log($"Downloading repository: {repoData["name"]}");

        // Create the JobData instance
        var jobData = GameObject.FindObjectOfType<JobData>();
        if (jobData == null)
        {
            Debug.LogError("JobData component not found.");
            return;
        }

        // Define the async task function
        Func<Job, Task> asyncTaskFunction = async (job) =>
        {
            string workingDirectory = "/Users/computercomputer/justinops/propagator";
            ShellRun.Response response = ShellRun.RunCommand(command, arguments, isNamedArguments: true, workingDirectory);

            // Capture the output
            job.stdout = response.Output;
            job.stderr = response.Error;

            if (!string.IsNullOrEmpty(response.Error))
            {
                SetRecordField(repoData["name"],"download_status", "failed");
                Debug.LogError($"Failed to download {repoData["name"]}: {response.Error}");
                throw new Exception(response.Error);
            }
            SetRecordField(repoData["name"],"download_status", "finished");
            Debug.Log($"Successfully downloaded {repoData["name"]}");
        };

        // Add the job using the JobData class
        jobData.AddAsyncJob(
            name: $"DownloadRepo_{repoData["name"]}",
            taskFunction: asyncTaskFunction,
            successFunction: () => Debug.Log($"Job succeeded: {repoData["name"]} download completed."),
            failureFunction: () => Debug.LogError($"Job failed: {repoData["name"]} download failed.")
        );

        // Optionally refresh the job data to start processing jobs
        jobData.Refresh();
    }*/
    /*
    private void NonBlockingShellExecution(
        string jobName,
        string[] command,
        DictStrStr arguments,
        string workingDirectory,
        Action<string> onSuccess,
        Action<string> onFailure)
    {
        // Create the JobData instance

        // Define the async task function for shell execution
        Func<Job, Task> asyncTaskFunction = async (job) =>
        {
            try
            {
                ShellRun.Response response = ShellRun.RunCommand(command, arguments, isNamedArguments: true, workingDirectory);

                // Capture the output
                job.stdout = response.Output;
                job.stderr = response.Error;

                if (!string.IsNullOrEmpty(response.Error))
                {
                    // Call the failure callback with the error message
                    onFailure?.Invoke(response.Error);
                    throw new Exception(response.Error);
                }

                // Call the success callback with the output message
                onSuccess?.Invoke(response.Output);
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred during shell execution: {ex.Message}");
                throw;
            }
        };

        var jobData = GameObject.FindObjectOfType<JobData>();
        if (jobData == null)
        {
            Debug.LogError("JobData component not found.");
            return;
        }

        // Add the job using the JobData class
        jobData.RunAsyncJob(
            name: jobName,
            taskFunction: asyncTaskFunction,
            successFunction: () => Debug.Log($"Job succeeded: {jobName}"),
            failureFunction: () => Debug.LogError($"Job failed: {jobName}")
        );

        // Optionally refresh the job data to start processing jobs
        //jobData.Refresh();
    }
    */

private Func<Task> CreateShellTask(
    string jobName,
    string[] command,
    DictStrStr arguments,
    bool isNamedArguments,
    string workingDirectory,
    Action<ShellRun.Response> onSuccess,
    Action<ShellRun.Response> onFailure,
    bool debugMode = false)
{
    Func<Task> asyncTaskFunction = async () =>
    {
        try
        {
            if (debugMode)
            {
                // Log on the main thread using a dispatcher
                ApplicationState.Instance().Enqueue(() => Debug.Log("INNER TEMP Running a command " + command[0]));
            }

            ShellRun.Response response = ShellRun.RunCommand(command, arguments, isNamedArguments: isNamedArguments, workingDirectory);
            if (debugMode)
            {
                // Log on the main thread using a dispatcher
                ApplicationState.Instance().Enqueue(() => Debug.Log("INNER TEMP RAN a command " + command[0]));
            }

            if (!string.IsNullOrEmpty(response.Error))
            {
                if (debugMode)
                {
                    ApplicationState.Instance().Enqueue(() => Debug.Log("INNER TEMP Running a command FAIL 1"));
                }
                ApplicationState.Instance().Enqueue(() => onFailure.Invoke(response));
                return;
            }

            if (debugMode)
            {
                ApplicationState.Instance().Enqueue(() => Debug.Log("INNER TEMP Running a command SUCCESS"));
            }
            ApplicationState.Instance().Enqueue(() => onSuccess?.Invoke(response));
        }
        catch (Exception ex)
        {
            if (debugMode)
            {
                ApplicationState.Instance().Enqueue(() => Debug.Log("*********Fatal Exception running command:" + ex.Message + " " + ex.StackTrace));
            }

            ShellRun.Response resp = new ShellRun.Response
            {
                Output = "",
                Error = $"An error occurred during shell execution: {ex.Message}: {ex.StackTrace}"
            };
            ApplicationState.Instance().Enqueue(() => onFailure.Invoke(resp));
        }
    };
    return asyncTaskFunction;
}


   private Func<Task> CreateShellTaskOLD(
        string jobName,
        string[] command,
        DictStrStr arguments,
        bool isNamedArguments,
        string workingDirectory,
        Action<ShellRun.Response> onSuccess,
        Action<ShellRun.Response> onFailure,
        bool debugMode = false)
    {
        Func<Task> asyncTaskFunction = async () =>
        {
            try
            {
                if (debugMode == true) Debug.Log("INNER TEMP Running a command "+command[0]);
                ShellRun.Response response = ShellRun.RunCommand(command, arguments, isNamedArguments: isNamedArguments, workingDirectory);
                if (!string.IsNullOrEmpty(response.Error))
                {
                    if (debugMode == true) Debug.Log("INNER TEMP Running a command FAIL 1");
                    onFailure.Invoke(response);
                }
                if (debugMode == true) Debug.Log("INNER TEMP Running a command SUCCESS");
                onSuccess?.Invoke(response);
            }
            catch (Exception ex)
            {
                if (debugMode == true) Debug.Log("*********Fatal Exception running command:"+ex.Message+" "+ex.StackTrace);
                if (debugMode == true) Debug.Log("TEMP Running a command FAIL 2");
                ShellRun.Response resp = new ShellRun.Response();
                resp.Output = "";
                resp.Error = $"An error occurred during shell execution: {ex.Message}: {ex.StackTrace}";
                onFailure.Invoke(resp);
            }
        };
        return  asyncTaskFunction;
        //Example basic usage Task.Run(() => asyncTaskFunction());
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
        Func<Task> tsk = CreateShellTask(
            jobName: $"DownloadRepo_{repoData["name"]}",
            command: command,
            arguments: arguments,
            isNamedArguments:true,
            workingDirectory: "/Users/computercomputer/justinops/propagator",
            onSuccess: (ShellRun.Response output) =>
            {
                Debug.Log($"TEMP FINAL TEMP Successfully downloaded {repoData["name"]}");
                SetRecordField(repoData["name"], "download_status", "finished");
                Debug.Log($"TEMP FINAL Successfully downloaded {repoData["name"]}");
                //repoDatabase.UpdateDataRevision();
                repoDatabase.ReloadData();
            },
            onFailure: (ShellRun.Response  error) =>
            {
                Debug.Log($"TEMP FINAL FAILED downloaded {repoData["name"]}");
                SetRecordField(repoData["name"], "download_status", "failed");
                Debug.LogError($"TEMP FINAL Failed to download {repoData["name"]}: {error.Error}");
            },
            debugMode:true
        );
        Debug.Log($"{this.ToString()}: TEMP OUTER Starting Shell Download {repoData["name"]}");
        Task.Run(tsk);
        /*
        NonBlockingShellExecution(
            jobName: $"DownloadRepo_{repoData["name"]}",
            command: command,
            arguments: arguments,
            workingDirectory: "/Users/computercomputer/justinops/propagator",
            onSuccess: (output) =>
            {
                SetRecordField(repoData["name"], "download_status", "finished");
                Debug.Log($"Successfully downloaded {repoData["name"]}");
            },
            onFailure: (error) =>
            {
                SetRecordField(repoData["name"], "download_status", "failed");
                Debug.LogError($"Failed to download {repoData["name"]}: {error}");
            }
        );*/
    }


}
