using System.Collections.Generic;
using UnityEngine;
using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System.Runtime.InteropServices;
using UnityEngine.AI;
using Unity.VisualScripting;
using System.Text;
using UnityEditor;
/// <summary>
/// Step result is created before each job step, and passed to the function
/// Its main role is to cause a process to crash if it is not finalized properly. That way
/// all jobs will be forced to maintain proper reporting behaviour on each iteration
/// </summary>
public class JobResult
{
    bool flagCancel = false;
    bool flagFinished = false;
    float minProgress = 0;
    float maxProgress = 100;
    string progressJson =      $"{"value"}:0.1,{"message"}:{"job was initalized, but not started"}";
    bool flagCancelChecked = false;
    bool flagProgressUpdated = false;
    public void ResetFlags()
    {
        flagCancelChecked = false;
        flagProgressUpdated = false;
    }    
    public bool SetCancel()
    {
        flagCancel = true;
        return true;
    }
    public bool SetFinished()
    {
        flagFinished = true;
        return true;
    }    
    public bool GetFinished()
    {
        return flagFinished;
    }    
    public bool ReadCancel()
    {
        flagCancelChecked = true;
        return flagCancel;
    }
    public bool SetProgress(float progress, string message )
    {
        flagProgressUpdated = true;
        //throw new System.Exception($"Progress was UPDATED ");
        if (progress > maxProgress || progress <minProgress)
            throw new System.Exception($"Progress was supplied an invalid value {progress}");
        progressJson =  $"{"value"}:{progress},{"message"}:{message}";
        return true;
    }
    public string ReadProgress()
    {
        return progressJson;
    } 

    public bool WasCancelChecked()
    {
        return flagCancelChecked;
    }
    public bool WasProgressUpdated()
    {
        return flagProgressUpdated;
    }

}



public class IntervalRunner
{
    // Dictionary to store timers for different operations
    private readonly Dictionary<string, float> timers = new Dictionary<string, float>();

    /// <summary>
    /// Runs the provided action every specified delay in real-time seconds.
    /// </summary>
    /// <param name="id">Unique ID for the operation.</param>
    /// <param name="delay">Interval in seconds between each execution.</param>
    /// <param name="deltaTime">Elapsed time since the last update, typically Time.deltaTime.</param>
    /// <param name="action">The action to execute.</param>
    public void RunIfTime(string id, float delay, float deltaTime, Action action)
    {
        // Check if the timer exists; if not, initialize it
        if (!timers.ContainsKey(id))
        {
            timers[id] = 0f;
        }

        // Update the timer
        timers[id] += deltaTime;

        // If the timer exceeds the delay, execute the action and reset the timer
        if (timers[id] >= delay)
        {
            action?.Invoke();
            timers[id] = 0f;
        }
    }

    /// <summary>
    /// Clears a timer by ID (optional utility method).
    /// </summary>
    /// <param name="id">The ID of the timer to clear.</param>
    public void ClearTimer(string id)
    {
        if (timers.ContainsKey(id))
        {
            timers.Remove(id);
        }
    }

    /// <summary>
    /// Clears all timers (optional utility method).
    /// </summary>
    public void ClearAllTimers()
    {
        timers.Clear();
    }
}

public class JobUtils
{
    private static void AsyncRunMainThread(Action action)
    {
        ApplicationState.Instance().Enqueue(action);
    }
    private static void AsyncRunMainThread(Action<object> action,object param)
    {
        ApplicationState.Instance().Enqueue(action,param);
    }




    public static Func<Task> CreateStepJob(
                                        string jobName,
                                        Func<JobResult, Task<object>> stepJob, 
                                        string jobId=null, 
                                        string parentId="",
                                        Action<object> doOnSuccess=null,
                                        Action<object> doOnFailure=null, 
                                        Action<object> doOnProgress=null, 
                                        bool debugMode=false)
    {
        // bool procDoCancel = false; Cant cancel tasks
        DateTime lastHeartbeat = DateTime.UtcNow;
        bool waitingOnStep = false;
        JobResult culmulativeStepResult = new JobResult();

        // Function to check if the job is still running based on heartbeat
        Func<bool> isRunning = () =>
        {
            TimeSpan timeSinceLastHeartbeat = DateTime.UtcNow - lastHeartbeat;
            //ApplicationState.Instance().Enqueue(() => Debug.Log($">Step function reportiong  {rindex.ToString()}"));

            return timeSinceLastHeartbeat.TotalSeconds < 2 || waitingOnStep == true;
        };

        Func<bool> cancel = () =>
        {
            culmulativeStepResult.SetCancel();
            return false;
        };

        Func<object> readProgress = () => culmulativeStepResult.ReadProgress();
        Action<object> onSuccess = doOnSuccess!= null ? doOnSuccess: (object obj) => {};
        Action<object> onFailure = doOnFailure!= null ? doOnFailure: (object obj) => Debug.LogError("Task failed: " + obj.ToString());
        Action<object> onProgress = doOnProgress!= null ? doOnProgress: (object obj) => {};

        Func<Task<object>> wrappedLongJob = async () =>
        {
            object returnVal = null;
            culmulativeStepResult.ResetFlags();
            while(culmulativeStepResult.GetFinished() == false)
            {
                lastHeartbeat = DateTime.UtcNow;
                waitingOnStep = true;
                try
                {
                    returnVal = await Task.Run(() => stepJob(culmulativeStepResult));
                }
                finally
                {
                    waitingOnStep = false;
                }
                if (returnVal!= null)
                {
                    culmulativeStepResult.SetFinished();
                    return returnVal;
                }
                if (culmulativeStepResult.WasCancelChecked() == false)
                    throw new System.Exception($"Cancel was not checked by {jobId}.{jobName}");
                if (culmulativeStepResult.WasProgressUpdated() == false)
                    throw new System.Exception($"Progress was not checked by {jobId}.{jobName}");
                culmulativeStepResult.ResetFlags();
            }
            return returnVal;
        };


        return CreateJobifiedFunc(
            id: jobId,
            jobName: jobName,
            taskFunc: wrappedLongJob,
            isRunning: isRunning,
            cancel: cancel,
            readProgress: readProgress,
            onSuccess: onSuccess,
            onProgress: onProgress,
            onFailure: onFailure,
            debugMode: debugMode,
            parentId: parentId
        );
    }


    public static Func<Task> CreateJobifiedFunc(
        string id,
        string jobName,
        Func<Task<object>> taskFunc,
        Func<bool> isRunning,
        Func<bool> cancel,
        Func<object> readProgress,
        Action<object> onProgress,
        Action<object> onSuccess,
        Action<object> onFailure,
        bool debugMode,
        string parentId)
    {
        Func<Task> asyncTaskFunction = async () =>
        {
            // Generate a unique job ID
            string jobId = "";
            if(id == null)
                 jobId = Guid.NewGuid().ToString();
            else
                 jobId = id;

            // Register the job with JobData
            JobData jobData = JobData.Instance();
            if (jobData.RegisterJob(id:jobId, name:jobName, parentId:parentId)==false)
            {
                AsyncRunMainThread(() => onFailure?.Invoke(new System.Exception("Can not start job, it is already running")));
                // jobData.UpdateJobStatus(jobId, "failed");
                return;
            }
            // Create JobHandle
            object progressJson = "UNKNOWN";
            DeceliumJobHandle jobHandle = new DeceliumJobHandle
            {
                IsRunning = isRunning,
                Cancel = cancel,
                Poll = () =>
                {
                    progressJson = readProgress();
                    onProgress(progressJson); // TODO Consider injecting progress logic somewhere else

                    return true;
                },
                GetProgress = () =>{
                    return progressJson;
                },
                GetData = () =>
                {
                    return new DictStrObj();
                }
            };
            jobData.AttachJobHandle(jobId, jobHandle);

            try
            {

                // Update the job status to "finishing"
                jobData.UpdateJobStatus(jobId, "started");
                //object result = await taskFunc();
                Task<object> task = taskFunc();
                if (task.Status == TaskStatus.Faulted)
                {
                    jobData.UpdateJobStatus(jobId, "crashed");
                    if (debugMode) AsyncRunMainThread(() => Debug.Log($"6++++++Core Job: Task crashed for job '{jobName}'"));
                    throw new System.Exception($"Job {jobName} crashed on start");
                }
                if (debugMode)
                {
                    AsyncRunMainThread(() => Debug.Log($"6++++++Core Job:Starting task for job '{jobName}' with ID '{jobId}'"));
                }
                jobData.UpdateJobRunning(jobId, "true"); 
                object result = await task;
                jobData.UpdateJobStatus(jobId, "finishing");
                if (debugMode)
                {
                    AsyncRunMainThread(() => Debug.Log($"6++++++Core Job:Task completed for job '{jobName}'"));
                }


                // Invoke the success callback
                AsyncRunMainThread(() => onSuccess?.Invoke(result));
                jobData.UpdateJobStatus(jobId, "finished");
            }
            catch (Exception ex)
            {
                if (debugMode)
                {
                    AsyncRunMainThread(() => {
                                                Debug.LogError($"Task failed for job '{jobName}':\n Exception: {ex.Message}\n {ex.StackTrace}");
                                            });
                }
                AsyncRunMainThread(() => onFailure?.Invoke(ex));
                jobData.UpdateJobStatus(jobId, "failed");
            }
        };

        return asyncTaskFunction;
    }
    
    private static readonly Dictionary<int, string> KnownErrorMessages = new Dictionary<int, string>
    {
        { 0, "Success: No error occurred." },
        { 1, "General error: Indicates a general error in command execution. Check syntax or permissions." },
        { 2, "Misuse of shell built-ins: Often caused by syntax errors or incorrect usage of built-in commands." },
        { 126, "Command invoked cannot execute: Command found but is not executable. Check permissions or file type." },
        { 127, "Command not found: Command does not exist in the system PATH or directory. Verify command and PATH." },
        { 128, "Invalid argument to exit: Command exited with a non-standard argument." },
        { 130, "Script terminated by Control-C: Process interrupted by SIGINT (Ctrl+C)." },
        { 137, "Process killed: Process terminated by SIGKILL. This can occur if memory limits are reached." },
        { 139, "Segmentation fault: A memory access violation occurred. Check memory usage and file validity." },
        { 143, "Terminated: Process killed by SIGTERM, likely from an external kill request." },
        { 255, "General error (often a command execution issue): Indicates a general failure to execute the command. Commonly due to issues like missing permissions, misconfigured paths, or inaccessible files." }
        // Add other codes as needed for your application environment.
    };
    public static string GetErrorCodeMessage(int exitCode)
    {
        
        return KnownErrorMessages.TryGetValue(exitCode, out var message)
            ? message
            : $"Unknown error code {exitCode}. Check command and environment for potential issues.";
}

    public static Func<Task> CreateJobifiedShellTask(
        string jobName,
        string[] command,
        DictStrObj arguments,
        bool isNamedArguments,
        string workingDirectory,
        Action<object> onSuccess,
        Func<object> readProgress,
        Action<object> onFailure,
        Action<object> onProgress,
        bool debugMode,
        string parentId)
    {
        ShellRun.Response response = new ShellRun.Response();
        Process process = null;
        Func<Task<object>> shellTaskFunc = async () =>
        {
            try
            {
                if (debugMode == true)
                { 
                    AsyncRunMainThread(() =>
                    {
                        Debug.Log("4++++AM Running Command:"+ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] +" "+
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
                        Debug.Log(workingDirectory);
                    });
                }
                process = ShellRun.StartProcess(command, arguments, isNamedArguments, workingDirectory);
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());
                
                if (debugMode == true)
                {
                    if (!process.HasExited)
                    {
                        AsyncRunMainThread(() =>
                        {
                            Debug.LogError("4++++Very bad: STILL RUNNING!!!!! Command:"+ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] +" "+
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
                            Debug.LogError("!!! Process is still running.");
                            Debug.LogError("!!! Process is still running.");
                            Debug.LogError("!!! Process is still running.");
                        });
                    }
                    else
                    {
                        AsyncRunMainThread(() =>
                        {
                            Debug.Log("4++++EXITED! Command:"+ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] +" "+
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
                        });


                    }
                }                
                response.Output = await outputTask;
                response.Error = await errorTask;

                if (debugMode == true)
                { 
                    AsyncRunMainThread((object param) =>
                    {
                        DictStrObj data = (DictStrObj)param;
                        Debug.Log("4++++FINISHED COMMAND Command:"+ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[0] + " " +
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]);
                    },new DictStrObj {
                        {"text_value","Some Value"},
                        {"exit_code",process.ExitCode.ToString()},
                        {"output",response.Output.ToString()},
                        {"error",response.Error.ToString()}
                    });

                }
                string completeError = "";
                string systemError = "";
                string processError = response.Error;
                if (process.ExitCode != 0)
                    systemError = GetErrorCodeMessage(process.ExitCode);

                completeError = systemError + processError;
                if (!string.IsNullOrEmpty(completeError) || string.IsNullOrEmpty(response.Output))
                    throw new System.Exception (completeError);

                
                return (object)response.Output;
            }
            finally
            {
                process?.Dispose();
            }
        };

        Func<bool> isRunning = () =>
        {
           // Debug.Log($"Checking run status for { ShellRun.BuildCommandArguments(command, arguments, isNamedArguments: true)[1]} ");
            if (process == null)
                return false;
            try
            {
                return !process.HasExited;
            }
            catch
            {
                return false;
            }
        };

        Func<bool> cancel = () =>
        {
            if (process == null)
                return false;
            try
            {
                process.Kill();
                return true;
            }
            catch
            {
                return false;
            }
        };





        return CreateJobifiedFunc(
            id: null, // Generate a new ID inside CreateJobifiedFunc
            jobName: jobName,
            taskFunc: shellTaskFunc,
            isRunning: isRunning,
            cancel: cancel,
            readProgress:readProgress,
            onSuccess: onSuccess,
            onProgress:onProgress,
            onFailure: onFailure,
            debugMode:debugMode,
            parentId:parentId
        );
    }
/**
SYNC Version (example)
      var prnt = new DictTable {{"output",rec}};
        string[] command = new string[] { venvPythonPath,"propagator/datasource/UtilGit.py","list_local_repos" };
        DictStrStr arguments =new DictStrStr {{"backup_directory",rec["path"]}} ;
        bool isNamedArguments = true; 

        ShellRun.Response r = ShellRun.RunCommand( command, arguments,  isNamedArguments,  setDat.GetPythonWorkDir() );
        List<object> jsonObj = (List<object> )JsonParser.ParseJsonObjects(r.Output);
        string jsonString = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
        if (jsonObj.Count == 0)
            return;
**/


    public static Func<Task> CreateShellTask(
        string[] command,
        DictStrObj arguments,
        bool isNamedArguments,
        string workingDirectory,
        Action<object> onSuccess,
        Action<object> onFailure,
        bool debugMode)
    {
        Process process = null;

        Func<Task> shellTaskFunc = async () =>
        {
            try
            {
                if (debugMode)
                {
                    AsyncRunMainThread(() =>
                    {
                        Debug.Log("Running Command: " + ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[0] + " " +
                                ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[1]);
                        Debug.Log("Working Directory: " + workingDirectory);
                    });
                }

                // Start the process
                process = ShellRun.StartProcess(command, arguments, isNamedArguments, workingDirectory);

                // Read output and error streams asynchronously
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                // Wait for the process to exit
                await Task.Run(() => process.WaitForExit());

                if (debugMode)
                {
                    AsyncRunMainThread(() =>
                    {
                        try
                        {
                            if (process.HasExited)
                                Debug.Log("Process exited successfully.");
                            else
                                Debug.LogError("Process is still running.");
                        }
                        catch(Exception e)
                        {
                                Debug.LogError(e);
                        }
                    });
                }

                // Get the results from the output and error streams
                string output = await outputTask;
                string error = await errorTask;

                if (debugMode)
                {
                    AsyncRunMainThread(param =>
                    {
                        var data = (DictStrObj)param;
                        Debug.Log("Command Finished: " + data["command"]);
                        Debug.Log("Exit Code: " + data["exit_code"]);
                        Debug.Log("Output: " + data["output"]);
                        Debug.Log("Error: " + data["error"]);
                    }, new DictStrObj
                    {
                        {"command", ShellRun.BuildCommandArguments(command, arguments, isNamedArguments)[1]},
                        {"exit_code", process.ExitCode.ToString()},
                        {"output", output},
                        {"error", error}
                    });
                }

                // Check for errors
                string completeError = "";
                string systemError = "";
                string processError = error;
                if (process.ExitCode != 0)
                    systemError = GetErrorCodeMessage(process.ExitCode);

                completeError = systemError + processError;
                //if (!string.IsNullOrEmpty(completeError) || string.IsNullOrEmpty(output))
                if (!string.IsNullOrEmpty(completeError) || process.ExitCode!= 0)
                    throw new System.Exception(completeError);

                // Invoke the success callback
                onSuccess?.Invoke(output);
            }
            catch (Exception ex)
            {
                // Invoke the failure callback
                onFailure?.Invoke(ex);
            }
            finally
            {
                // Clean up the process resources
                process?.Dispose();
            }
        };

        return shellTaskFunc;
    }




}

public class DeceliumJobHandle
{
    public DictStrObj dataframe; // Data associated with the job
    public Func<bool> IsRunning; // Function that returns whether the job is currently running
    public Func<bool> Cancel;    // Function that attempts to cancel the job and returns whether the cancellation was successful
    public Func<bool> Poll; // A job that can trigger an update to data, progress, and any job internals. Typically used to populate GetData and GetProgress
    public Func<DictStrObj> GetData; // Function that returns additional data related to the job

    public Func<object> GetProgress; // Function that returns additional data related to the job
}


public class JobData : StandardData
{
    private static JobData __instance;
    private Dictionary<string, DeceliumJobHandle> __handles = new Dictionary<string, DeceliumJobHandle>();
    private float updateInterval = 6.0f; // Check every 5 seconds
    private float timeSinceLastUpdate = 0.0f;

    private float pollingInterval = 2f; // Check every 5 seconds
    private float timeSinceLastPoll = 0.0f;
    //private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    void Awake()
    {
        __instance = this;
    }
    void Start(){
        
        //StartExampleLongRunningTask("finish",10,100);
        //StartExampleLongRunningTask("cancel",10,2000);
        //StartExampleStepRunningTask("run",10,2000);
        //StartExampleLongRunningTask("run2",15,2000);
        //StartExampleLongRunningTask("crash",1000,10);
        //Task.Delay(2000); // Wait for 1 second
        //Debug.Log("Cancelling Result");
        //bool result = CancelJob("cancel");
        //Debug.Log($"Cancel Result {result}");
        
    }
    private static readonly Mutex _mutex = new Mutex();
    public static JobData Instance()
    {
        return __instance;
    }
    public void LockReserve()
    {
        _mutex.WaitOne();
    }
    public void LockRelease()
    {
        _mutex.ReleaseMutex();
    }
    void StartExampleStepRunningTask(string id,int seconds, int exception_seconds)
    {
        int currentSeconds = 0;
        Func<Task> tsk = JobUtils.CreateStepJob(jobName:$"Example Job {id}",
        stepJob:async (JobResult jobResult) =>
        {
            /// *** IMPORTANT (Job management)
            float progress = (float)currentSeconds*100/((float)seconds*100);
            jobResult.SetProgress(progress,"ran iteration normally");
            if (jobResult.ReadCancel()) return $"id_{id}_cancelled"; 
            if (currentSeconds >= seconds) return $"id_{id}_finished";         // jobResult.SetFinished(); can be used if one needs to return null on finish
            
            // Do the normal work.
            if (currentSeconds >= exception_seconds) 
                throw new System.Exception("I failed as planned");
            ApplicationState.Instance().Enqueue(() => Debug.Log($"{id} is at {currentSeconds.ToString()}"));
            await Task.Delay(1000); // Wait for 1 second
            currentSeconds += 1;
            return null;        
        },debugMode:true);
        Task.Run(tsk);
    }

    void StartExampleLongRunningTask(string id,int seconds, int exception_seconds)
    {
        bool procDoCancel = false;
        int procCount = 0;
        DateTime lastHeartbeat = DateTime.UtcNow;
        TaskCompletionSource<bool> taskStarted = new TaskCompletionSource<bool>();
        // Define the task function
        float progressInt = 0;
        Func<Task<object>> longRunningTask = async () =>
        {
            taskStarted.SetResult(true);                
            for (int i = 0; i <= seconds; i++)
            {
                procCount = i;
                int internali = i;
                string internalid = id;
                ApplicationState.Instance().Enqueue(() => Debug.Log($"{internalid} is at {internali.ToString()}"));

                // Update the heartbeat timestamp
                lastHeartbeat = DateTime.UtcNow;

                if (procDoCancel)
                    break;
                if (i > exception_seconds)
                    throw new System.Exception("I failed badly");
                progressInt = ((float)i/(float)seconds);
                await Task.Delay(1000); // Wait for 1 second
                
            }
            return "id_finished";
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


        Func<object> readProgress = () =>
        {
            return progressInt;
        };


        Action<object> onSuccess = (object obj) => Debug.Log("Long-running task completed successfully.");
        Action<object> onFailure = (object obj) => Debug.LogError($"Long-running task failed: ");
        Action<object> onProgress = (object obj) => Debug.Log($"Long-running task progress: {obj}");

        Func<Task> jobifiedTask = JobUtils.CreateJobifiedFunc(
            id:id,
            jobName: "LongTask",
            taskFunc: longRunningTask,
            isRunning: isRunning,
            cancel: cancel,
            readProgress:readProgress,
            onSuccess: onSuccess,
            onProgress:onProgress,
            onFailure: onFailure,
            debugMode: false,
            parentId:"test"
        );

        Task.Run(jobifiedTask);
        taskStarted.Task.Wait(5000);
    }



    void Update()
    {
        // Increment the timer by the time since the last frame
        timeSinceLastUpdate += Time.deltaTime;
        timeSinceLastPoll  += Time.deltaTime;
        bool doPoll = false;
        bool doIsRunning = false;
        if (timeSinceLastPoll >= pollingInterval)
        {
            doPoll = true;
            timeSinceLastPoll = 0.0f;
        }

        // If enough time has passed, perform the check
        if (timeSinceLastUpdate >= updateInterval)
        {
            doIsRunning = true;
            timeSinceLastUpdate = 0.0f;
        }
        LockReserve();
        try
        {      
            // Check the status of all jobs
            if (doPoll == true || doIsRunning == true)
            {


                UpdateAllJobStatuses(   doPoll:doPoll,
                                        doIsRunning:doIsRunning);
            }
            UpdateDataRevision(1); // Flag that the jobs should update
        }
        finally
        {
            LockRelease(); 
        }

    
    }
    public string StringifyProcessResp(object resp)
    {
        if (resp as System.Exception != null)
        {
            var stringBuilder = new StringBuilder();
            System.Exception exception = (System.Exception)resp;
            while (exception != null)
            {
                stringBuilder.AppendLine(exception.Message);
                stringBuilder.AppendLine(exception.StackTrace);

                //exception = exception.InnerException;
                return "ProcError:"+stringBuilder;
            }
            System.Exception ex = (System.Exception)resp;
           //return  ((System.Exception)resp).ToSummaryString();
           return $"{ex.Message}\n{ex.StackTrace}";
        }
        return resp.ToString();
    } 
    // Check and update the status of all jobs
    private void UpdateAllJobStatuses(bool doPoll,bool doIsRunning)
    {
        LockReserve();
        try
        {      
            // TODO - finish moving poll and run update code into jobs themselves, so they mey register their own frequencies. This paves the way for jobs to 
            // run in a lighter manner.
            List<string> recs = this.ListRecords();
            foreach (string id in recs)
            {
                // Check if the job is running
                bool isRunning = IsJobRunning(id);
                if (doPoll==true)
                {
                    if(isRunning==true)
                    {
                        PollJob(id); // Tell the job to update
                        object progressData = GetJobProgress(id);
                        UpdateJobField(id,  "progress",$"uX-{StringifyProcessResp(progressData)}");
                    }
                }
                if(doIsRunning==true)
                {
                    string flagRunning =  (string)GetRecordField(id,"running");
                    if (isRunning && flagRunning != "true" )
                    {
                        PollJob(id);
                        object progressData = GetJobProgress(id);
                        UpdateJobField(id,  "progress",$"u0-{StringifyProcessResp(progressData)}");
                        UpdateJobRunning(id, "true");
                    }
                    if (!isRunning && flagRunning == "true" )
                    {
                        PollJob(id); // Tell the job to update
                        object progressData = GetJobProgress(id);
                        UpdateJobField(id,  "progress",$"un-{StringifyProcessResp(progressData)}");
                        UpdateJobRunning(id, "false");
                    }
                    if (!isRunning && (string)GetRecordField(id,  "progress") == "")
                    {
                        PollJob(id); // Tell the job to update
                        object progressData = GetJobProgress(id);
                        UpdateJobField(id,  "progress",$"uf-{StringifyProcessResp(progressData)}");
                        UpdateJobRunning(id, "false");
                    }
                }
            }
        }
        finally
        {
            LockRelease();
        }

    }    

    // Attach an existing JobHandle to a job ID
    public bool AttachJobHandle(string id, DeceliumJobHandle handle)
    {
        LockReserve();
        try
        {      
            if (!__handles.ContainsKey(id))
            {
                __handles[id] = handle;
                SetRecordField(id, "status", "started");
                return true;
            }
            return false;
        }
        finally
        {
            LockRelease();
        }

    }

    // Register a new job in the JobData registry
    public bool RegisterJob(string id, string name, string status = "pending",string parentId=null)
    {
        LockReserve();
        try
        {      
            if (string.IsNullOrEmpty(id))
                throw new System.Exception("Job ID cannot be null");
            if (string.IsNullOrEmpty(name))
                throw new System.Exception("Job name cannot be null");
            DictStrObj currentRecord = GetRecord(id);
            if (currentRecord != null && (string)currentRecord["status"]=="running")
            {
                return false;

            }

            // Create a job record and add it to __records
            var jobRecord = new DictStrObj
            {
                { "id", id },
                { "name", name },
                { "parent_id", parentId },
                { "status", status },
                { "running", "unknown" },
                { "progress", "" },
                { "stdout", "" },
                { "stderr", "" }
            };

            SetRecord(jobRecord,keyfield:"id");
            return true;
        }
        finally
        {
            LockRelease();
        }

    }

    // Update the status of a job
    public bool UpdateJobStatus(string id, string status)
    {
        return UpdateJobField(id,"status",status);
    }
    public bool UpdateJobRunning(string id, string status)
    {
        return UpdateJobField(id,"running",status);
    }
    public bool UpdateJobField(string id, string field, string status)
    {
        LockReserve();
        try
        {      
            if (!ContainsRecord(id))
                return false;
            return SetRecordField(id, field, status);
        }
        finally
        {
            LockRelease();
        }

    }    

    // Cancel a job and update its status
    public bool CancelJob(string id)
    {
        LockReserve();
        try
        {      
            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                if ((bool)handle.Cancel?.Invoke() == true)
                {
                    UpdateJobStatus(id, "cancelling");
                    return true;
                }
                return false;
            }
            return false;
        }
        finally
        {
            LockRelease();
        }

    }

    // Check if a job is currently running
    public bool IsJobRunning(string id)
    {
        LockReserve();
        try
        {      
        
            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                if (handle.IsRunning == null)
                    return false;
                return (bool)handle.IsRunning?.Invoke();
            }
            return false;
        }
        finally
        {
            LockRelease();
        }

    }
    public bool PollJob(string id)
    {
        LockReserve();
        try
        {      

            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                if (handle.Poll == null)
                {   
                    return false;
                }
                return (bool)handle.Poll?.Invoke();
            }
            return false;
        }
        finally
        {
            LockRelease();
        }

    }


    // Get the status of a job
    public string GetJobStatus(string id)
    {
        LockReserve();
        try
        {      
            return (string)GetRecordField(id, "status");
        }
        finally
        {
            LockRelease();
        }
    }

    // Get the progress of a job, if applicable
    public object GetJobProgress(string id)
    {
        LockReserve();
        try
        {      
            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                // return $"Interception for {id}";
                return (object)handle.GetProgress?.Invoke();
            }
            return 0f;
        }
        finally
        {
            LockRelease();
        }

    }

    // Get the data associated with a job
    public DictStrObj GetJobData(string id)
    {
        LockReserve();
        try
        {      
            if (__handles.ContainsKey(id))
            {
                var handle = __handles[id];
                return (DictStrObj)handle.GetData?.Invoke();
            }
            return null;
        }
        finally
        {
            LockRelease();
        }

    }

    // List all jobs with a given status
    public List<string> ListJobsByStatus(string status)
    {
        LockReserve();
        try
        {      
            var matchingJobs = new List<string>();
            foreach (var job in ListRecords())
            {
                if ((string)GetRecordField(job, "status") == status)
                {
                    matchingJobs.Add(job);
                }
            }
            return matchingJobs;
        }
        finally
        {
            LockRelease();
        }

    }

    // Get all registered jobs
    public List<string> GetAllJobIds()
    {
        LockReserve();
        try
        {      
            return ListRecords();
        }
        finally
        {
            LockRelease();
        }

    }

    // Remove a job from the registry
    public bool RemoveJob(string id)
    {
        LockReserve();
        try
        {      
            if (DeleteRecord(id))
            {
                if (__handles.ContainsKey(id))
                {
                    __handles.Remove(id);
                }
                return true;
            }
            return false;
        }
        finally
        {
            LockRelease();
        }

    }

    // Print job details for debugging
    public void PrintJobDetails(string id)
    {
        LockReserve();
        try
        {      
            var jobRecord = GetRecord(id);
            if (jobRecord != null)
            {
                Debug.Log($"Job ID: {jobRecord["id"]}, Name: {jobRecord["name"]}, Status: {jobRecord["status"]}");
                Debug.Log($"Stdout: {jobRecord["stdout"]}, Stderr: {jobRecord["stderr"]}");
            }
            else
            {
                Debug.Log("Job not found.");
            }
        }
        finally
        {
            LockRelease();
        }

    }

    public override void  AfterSaveData()
    {

        DictObjTable records = GetRecords();

        SetStatusLabel($"total {records.Keys.Count}");

    }
}

