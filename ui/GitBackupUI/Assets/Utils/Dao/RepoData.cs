using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using Newtonsoft.Json;
using Task = System.Threading.Tasks.Task;
using System;
// TODO kill these stupid data classes. They dont really help.
class RecordRepoReference
{
    public string name;
    public RecordRepoReference(DictStrObj record)
    {
        name = (string)record["name"];
    }

    public DictStrObj ToDictRecord()
    {
        DictStrObj d = new System.Collections.Generic.Dictionary<string, object> { { "name", name } };
        return d;
    }

}
class RecordRepoFull
{
    public string name;
    public string url;
    public string branch;
    public string status;
    public string last_updated;
    public string username;

    public RecordRepoFull(DictStrObj record)
    {
        name = (string)record["name"];
        url = (string)record["url"];
        branch = (string)record["branch"];
        status = (string)record["status"];
        last_updated = (string)record["last_updated"];
        //username = record["username"];
    }

    public DictStrObj ToDictRecord()
    {
        DictStrObj d = new DictStrObj {
            { "name", name },
            { "url", url },
            { "branch", branch },
            { "status", status },
            { "last_updated", last_updated },
        };
        return d;
    }

}


public class RepoData : StandardData
{
    ProfileData profileDatasource;  // Use the IRepoData interface for the RepoData class
    void Awake()
    {
        void OnValueChanged(string key, object newValue)
        {
            if (key == "selected_profile")
                SetStatusLabel($"attached user {newValue}");   
            else
            {
                SetStatusLabel($"attached {key}:{newValue}");   
            }
            ReloadData();
        }
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        appState.RegisterChangeCallback("selected_profile", OnValueChanged);
        appState.RegisterChangeCallback("system_settings_updated", OnValueChanged);
        SetStatusLabel($"no attached user");            

        // Setting the value, which triggers the callback if the value changes
        //appState.Set("playerScore", 100);

        // Unregistering the callback when it is no longer needed
        //appState.UnregisterChangeCallback("playerScore", OnValueChanged);        
    }
    public bool AddRepo(string name, string repoUrl, string branch,string status ="UNKNOWN",string lastUpdated ="NEVER")
    {
        throw new System.Exception("Also dead code?!?!");
        bool success = SetRecord(new DictStrObj
        {
            { "name", name },
            { "url", repoUrl },
            { "branch", branch },
            { "status", status },
            { "last_updated",lastUpdated }
        });
        SetStatusLabel($"repo_count {GetRecords().Count}");
        return success;

    }
    // TODO get rid of this binding method, in favour of all Data classes working independetly
    public void BindToProfileData(ProfileData profileData)
    {
        //profileDatasource = navigatorObject.GetComponent<ProfileData>();
        profileDatasource = profileData;

    }
    public  void ReloadData()
    {
       
        if (profileDatasource == null)
        {
            Debug.Log("RepoData: LoadData halted; profileDatasource is null");
            return;
        }
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        SetupData setDat = navigatorObject.GetComponent<SetupData>();

    
        if (setDat.IsPythonReady() == false)
        {
            //Debug.Log("RepoData: ReloadData Cant run, because venv is not configured yet.");
            return;
        }

        string selectedProfileNames = (string)appState.Get("selected_profile");
        string[] profileNames = selectedProfileNames.Split(',');
        
        foreach ( string selectedProfileName in profileNames)
        {
            ProcessUser(selectedProfileName);
        }

    }
    private void ProcessUser(string selectedProfileName)
    {
        /// Attach Navigator
        var navigatorObject = GameObject.Find("Navigator");
        var appState = navigatorObject.GetComponent<ApplicationState>();
        SetupData setDat = navigatorObject.GetComponent<SetupData>();
        RepoGithubData ghDat = navigatorObject.GetComponent<RepoGithubData>();
        string venvPythonPath = setDat.GetPythonRoot();
        DictStrObj rec = profileDatasource.GetRecord(selectedProfileName);
        if (rec == null)
        {
            Debug.Log("Could not find target user");
            return;
        }

        System.Func<Task>  tsk = JobUtils.CreateShellTask(
            command: new string[] { venvPythonPath,"propagator/datasource/UtilGit.py","list_local_repos" },
            arguments:new DictStrObj {{"backup_directory",rec["path"]}} ,
            isNamedArguments:true,
            workingDirectory: setDat.GetPythonWorkDir(),
            onSuccess:(object rawJson) => {
                ParseAndSetRecords( (string)rawJson,selectedProfileName,ghDat);
                ghDat.AfterSaveData();
            },
            onFailure:(object error) => {
                throw (System.Exception)error;
            },
            debugMode:false);
        //Task.Run(tsk); // ASYNC
        Task.Run(tsk).GetAwaiter().GetResult(); // SYNC                
        
    }

    public static class ParserService
    {
        public static DateTime? ParseDate(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime parsedDate))
            {
                return parsedDate;
            }
            return null;
        }
    }    
    public static string GenerateStatusForRecord(DictStrObj rec, int minutesThreshold = 5)
    {
        string status = "unverified";

        // Parse local and GitHub timestamps
        DateTime? localDownloadDate = ParserService.ParseDate(rec.ContainsKey("latest_download_datetime") ? (string)rec["latest_download_datetime"] : null);
        DateTime? ghDownloadDate = ParserService.ParseDate(rec.ContainsKey("gh_latest_download_datetime") ? (string)rec["gh_latest_download_datetime"] : null);

        // Check if GitHub data is available
        bool hasGhData = rec.ContainsKey("gh_latest_commit_hash") && rec["gh_latest_commit_hash"] != "UNKNOWN";

        if (hasGhData)
        {
            string localHash = (string)rec["latest_commit_hash"];
            string ghHash = (string)rec["gh_latest_commit_hash"];

            if (localHash == ghHash)
            {
                if (ghDownloadDate.HasValue && (DateTime.UtcNow - ghDownloadDate.Value).TotalMinutes <= minutesThreshold)
                {
                    status = "verified"; // Hashes match, GH date within threshold
                }
                else if (ghDownloadDate.HasValue && (DateTime.UtcNow - ghDownloadDate.Value).TotalMinutes > minutesThreshold)
                {
                    status = "stale"; // Hashes match, but GH date is older than threshold
                }
            }
            else
            {
                status = "conflict"; // Hashes do not match
            }
        }
        else if (localDownloadDate.HasValue && (DateTime.UtcNow - localDownloadDate.Value).TotalMinutes <= minutesThreshold)
        {
            status = "new"; // No GH data but local date is within threshold
        }

        return status;
    }



    public void ParseAndSetRecords(string rawJson,string selectedProfileName,RepoGithubData ghDat)
    {
        var schema = new Dictionary<string, object>
        {
            { "directory_name", "string" },
            { "object", new Dictionary<string, object>
                {
                    { "obj_id", "string" },
                    { "repo_url", "string" },
                    { "repo_branch", "string" },
                    { "latest_commit_hash", "string" },

                }
            }
        };

        List<object> jsonObj = (List<object> )JsonParser.ParseJsonObjects(rawJson);
        string jsonString = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
        if (jsonObj.Count == 0)
            return;

        jsonString = JsonConvert.SerializeObject(jsonObj[0], Formatting.Indented);
        List<DictStrObj> allRemoteRecs = ghDat.ListFullRecords();
        foreach (Dictionary<string,object> folder in jsonObj)
        {

            try
            {
                var (isValid, error) = DJson.ValidateJsonSchema(folder, schema);
                if (!isValid)
                    throw new System.Exception($"Validation error: {error}");
                DateTime theDate = (DateTime)(((Dictionary<string,object>)folder["object"])["latest_download_datetime"]) ;
                //string utcString = dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

                
                DictStrObj rec =  new DictStrObj
                {
                    { "name", (string)folder["directory_name"] },
                    { "profile_name", selectedProfileName },
                    { "url",  (string)(((Dictionary<string,object>)folder["object"])["repo_url"])  },
                    { "branch", (string)(((Dictionary<string,object>)folder["object"])["repo_branch"])    },
                    { "obj_id", (string)(((Dictionary<string,object>)folder["object"])["obj_id"])    },
                    { "latest_commit_hash", (string)(((Dictionary<string,object>)folder["object"])["latest_commit_hash"])    },
                    { "latest_download_datetime",theDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")    },
                    { "gh_latest_commit_hash", "UNKNOWN"  },
                    { "gh_latest_download_datetime", "UNKNOWN"  },
                    { "status", "BAD_INIT_ERROR" },
                };


                SetRecord(rec);                

                SetStatusLabel($"repo_count {GetRecords().Count}");            
            }
            catch(System.Exception ex)
            {
                string strJson =  DJson.Stringify(folder);
                Debug.LogError($"Could not parse folder json {strJson}");
                throw ex;
            }

        }
    }

    public override void AfterSaveData()
    {
        
    }

    public override DictStrObj AfterAlterRecord(DictStrObj rec)
    {
        rec["status"] = GenerateStatusForRecord( rec);    
        return rec;    
    }

}
