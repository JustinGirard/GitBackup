

/*
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
 
 */

/*
using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>();

public class RepoData : MonoBehaviour
{

    private DictTable repos = new DictTable();
    private Dictionary<string, List<string>> repoUsers = new Dictionary<string, List<string>>();
    private DictTable users = new DictTable(); //  "User1", "User2", "User3" 

    // Simulate adding a repository
    public bool AddRepo(string name, string repoUrl, string branch)
    {
        repos.Add(repoUrl);
        return true; // Simulated success
    }

    // Simulate listing all repositories for a user
    public List<string> ListUser(string username)
    {
        // Returning simulated repositories
        return repos;
    }

    // Simulate removing a repository
    public bool RemoveRepo(string repoUrl)
    {
        return repos.Remove(repoUrl);
    }

    // Simulate adding a user
    public bool AddUser(string username)
    {
        users.Add(username);
        return true; // Simulated success
    }

    // Simulate removing a user
    public bool RemoveUser(string username)
    {
        return users.Remove(username);
    }

    // Simulate attaching a user to a repository
    public bool AttachUserToRepo(string username, string repoUrl)
    {
        if (!repoUsers.ContainsKey(repoUrl))
            repoUsers[repoUrl] = new List<string>();

        repoUsers[repoUrl].Add(username);
        return true; // Simulated success
    }

    // Simulate removing a user from a repository
    public bool RemoveUserFromRepo(string username, string repoUrl)
    {
        if (repoUsers.ContainsKey(repoUrl))
        {
            return repoUsers[repoUrl].Remove(username);
        }
        return false; // Failure if repo doesn't exist or user isn't attached
    }

    // Simulate listing users on a repository
    public List<string> ListUsersOnRepo(string repoUrl)
    {
        if (repoUsers.ContainsKey(repoUrl))
            return repoUsers[repoUrl];

        return new List<string>(); // Empty list if no users are attached
    }

    // Simulate getting repository info (just returns some dummy values)
    public Dictionary<string, string> GetRepoInfo(string repoUrl)
    {
        return new Dictionary<string, string>
        {
            { "repo_name", repoUrl },
            { "branch", "main" },
            { "status", "Active" },
            { "last_updated", "Yesterday" }
        };
    }

    // Simulate starting downloads
    public bool StartDownloads(string repoUrl)
    {
        Debug.Log("Download started for " + repoUrl);
        return true; // Simulated success
    }

    // Simulate getting download status
    public string DownloadStatus(string repoUrl)
    {
        return "Download in progress for " + repoUrl;
    }

    // Simulate stopping downloads
    public bool StopDownloads(string repoUrl)
    {
        Debug.Log("Download stopped for " + repoUrl);
        return true; // Simulated success
    }

    // Simulate getting a repository report
    public Dictionary<string, Dictionary<string, string>> GetRepoReport(string repoUrl)
    {
        return new Dictionary<string, Dictionary<string, string>>
        {
            {
                "Repo1", new Dictionary<string, string>
                {
                    { "commits", "50" },
                    { "last_commit", "Yesterday" },
                    { "branch", "main" }
                }
            },
            {
                "Repo2", new Dictionary<string, string>
                {
                    { "commits", "30" },
                    { "last_commit", "2 days ago" },
                    { "branch", "develop" }
                }
            }
        };
    }

    // Simulate getting repository status
    public bool GetRepoStatus(string repoUrl)
    {
        return true; // Simulated "active" status
    }

    // Simulate getting repository logs
    public string GetRepoLogs(string repoUrl)
    {
        return "Log entry for " + repoUrl + ": Success!";
    }
}
*/

using System.Collections.Generic;
using UnityEngine;

using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;



class RecordRepoReference
{
    public string name;
    public RecordRepoReference(DictStrStr record)
    {
        name = record["name"];
    }

    public DictStrStr ToDictRecord()
    {
        DictStrStr d = new DictStrStr { { "name", name } };
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

    public RecordRepoFull(DictStrStr record)
    {
        name = record["name"];
        url = record["url"];
        branch = record["branch"];
        status = record["status"];
        last_updated = record["last_updated"];
        //username = record["username"];
    }

    public DictStrStr ToDictRecord()
    {
        DictStrStr d = new DictStrStr {
            { "name", name },
            { "url", url },
            { "branch", branch },
            { "status", status },
            { "last_updated", last_updated },
        };
        return d;
    }

}


public class RepoData : MonoBehaviour
{

    private DictTable repos = new DictTable();
    private Dictionary<string, List<string>> repoUsers = new Dictionary<string, List<string>>();
    private DictTable users = new DictTable(); // To store user information

    // Simulate adding a repository with details like name, URL, and branch
    public bool AddRepo(string name, string repoUrl, string branch)
    {
        if (!repos.ContainsKey(repoUrl))
        {


            repos[name] = new DictStrStr
            {
                { "name", name },
                { "url", repoUrl },
                { "branch", branch },
                { "status", "Active" },
                { "last_updated", "Never" } // Default status on creation
            };
            
            return true;
        }
        return false; // Repo already exists
    }
    public bool DeleteRepo(string name)
    {
        if (repos.ContainsKey(name))
        {
            Debug.Log($"REPO REMOVED {name}");
            repos.Remove(name);

        }
        return true; // Repo already exists
    }

    // Simulate listing all repositories (in this case, URLs) for a user
    /*
    public List<string> ListUser(string username)
    {
        if (users.ContainsKey(username))
        {
            List<string> userRepos = new List<string>();
            foreach (var repo in repoUsers)
            {
                if (repo.Value.Contains(username))
                {
                    userRepos.Add(repo.Key);
                }
            }
            return userRepos; // List of repos that user is attached to
        }
        return new List<string>(); // User not found
    }*/
    public List<string> ListRepos()
    {
        return new List<string>(repos.Keys);
    }

    // Simulate removing a repository
    public bool RemoveRepo(string name)
    {
        if (repos.ContainsKey(name))
        {
            repos.Remove(name); // TODO FIX ChatGPT Error
            repoUsers.Remove(name); // Also remove any associated users
            return true;
        }
        return false;
    }

    // Simulate adding a user
    public bool AddUser(string username)
    {
        if (!users.ContainsKey(username))
        {
            users[username] = new DictStrStr { { "username", username } };
            return true;
        }
        return false; // User already exists
    }

    // Simulate removing a user
    public bool RemoveUser(string username)
    {
        if (users.ContainsKey(username))
        {
            users.Remove(username);

            // Remove user from all repos
            foreach (var repo in repoUsers)
            {
                repo.Value.Remove(username);
            }
            return true;
        }
        return false; // User not found
    }

    // Simulate attaching a user to a repository
    public bool AttachUserToRepo(string username, string repoUrl)
    {
        if (repos.ContainsKey(repoUrl) && users.ContainsKey(username))
        {
            if (!repoUsers.ContainsKey(repoUrl))
            {
                repoUsers[repoUrl] = new List<string>();
            }
            repoUsers[repoUrl].Add(username);
            return true;
        }
        return false; // Repo or user does not exist
    }

    // Simulate removing a user from a repository
    public bool RemoveUserFromRepo(string username, string repoUrl)
    {
        if (repoUsers.ContainsKey(repoUrl) && repoUsers[repoUrl].Contains(username))
        {
            repoUsers[repoUrl].Remove(username);
            return true;
        }
        return false; // Repo or user not found
    }

    // Simulate listing users on a repository
    public List<string> ListUsersOnRepo(string repoUrl)
    {
        if (repoUsers.ContainsKey(repoUrl))
        {
            return repoUsers[repoUrl];
        }
        return new List<string>(); // No users attached or repo not found
    }

    // Simulate getting repository info
    public DictStrStr GetRepoInfo(string name)
    {
        if (repos.ContainsKey(name))
        {
            return repos[name];
        }
        return new DictStrStr(); // Repo not found
    }

    // Simulate starting downloads for a repo
    public bool StartDownloads(string repoUrl)
    {
        if (repos.ContainsKey(repoUrl))
        {
            repos[repoUrl]["status"] = "Downloading";
            Debug.Log("Download started for " + repoUrl);
            return true;
        }
        return false; // Repo not found
    }

    // Simulate getting download status
    public string DownloadStatus(string repoUrl)
    {
        if (repos.ContainsKey(repoUrl))
        {
            return "Download status: " + repos[repoUrl]["status"];
        }
        return "Repo not found";
    }

    // Simulate stopping downloads for a repo
    public bool StopDownloads(string repoUrl)
    {
        if (repos.ContainsKey(repoUrl))
        {
            repos[repoUrl]["status"] = "Stopped";
            Debug.Log("Download stopped for " + repoUrl);
            return true;
        }
        return false; // Repo not found
    }

    // Simulate getting a repository report
    public DictTable GetRepoReport(string repoUrl)
    {
        if (repos.ContainsKey(repoUrl))
        {
            return new DictTable
            {
                { repoUrl, repos[repoUrl] }
            };
        }
        return new DictTable(); // Repo not found
    }

    // Simulate getting repository status
    public bool GetRepoStatus(string repoUrl)
    {
        if (repos.ContainsKey(repoUrl))
        {
            return repos[repoUrl]["status"] == "Active";
        }
        return false; // Repo not found
    }

    // Simulate getting repository logs
    public string GetRepoLogs(string repoUrl)
    {
        if (repos.ContainsKey(repoUrl))
        {
            return "Logs for " + repoUrl + ": All operations successful.";
        }
        return "Repo not found";
    }
}
