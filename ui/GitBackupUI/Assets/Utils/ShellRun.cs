using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

public class ShellRun
{
    public class Response
    {
        public string Output { get; set; }
        public string Error { get; set; }
    }

    public static DictTable ParseJsonToDictTable(string json)
    {
        var result = new DictTable();

        // Remove outer braces and split into individual entries
        json = json.Trim().TrimStart('{').TrimEnd('}');

        var outerEntries = json.Split(new string[] { "}," }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (var outerEntry in outerEntries)
        {
            // Split outer entry into key (profile name) and value (inner dictionary)
            var parts = outerEntry.Split(new string[] { ":{" }, System.StringSplitOptions.None);

            if (parts.Length != 2)
                continue;

            string profileName = parts[0].Trim(' ', '\"');
            string innerJson = parts[1].Trim(' ', '}');

            // Now we need to parse the inner dictionary
            var innerDict = new DictStrStr();
            var innerEntries = innerJson.Split(new string[] { "\",\"" }, System.StringSplitOptions.None);

            foreach (var innerEntry in innerEntries)
            {
                var keyValue = innerEntry.Split(new string[] { "\":\"" }, System.StringSplitOptions.None);
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim('\"');
                    string value = keyValue[1].Trim('\"');
                    innerDict[key] = value;
                }
            }

            // Add the parsed inner dictionary to the result table
            result[profileName] = innerDict;
        }

        return result;
    }


    // using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;
    public static string BuildJsonFromDictTable(DictTable records)
    {
        System.Text.StringBuilder jsonBuilder = new System.Text.StringBuilder();
        jsonBuilder.Append("{");

        bool isFirstOuter = true;

        foreach (var outerEntry in records)
        {
            if (!isFirstOuter)
            {
                jsonBuilder.Append(",");
            }

            jsonBuilder.Append($"\"{outerEntry.Key}\":{{"); // Profile name as key

            bool isFirstInner = true;
            foreach (var innerEntry in outerEntry.Value)
            {
                if (!isFirstInner)
                {
                    jsonBuilder.Append(",");
                }

                jsonBuilder.Append($"\"{innerEntry.Key}\":\"{innerEntry.Value}\""); // Field name and value
                isFirstInner = false;
            }

            jsonBuilder.Append("}");
            isFirstOuter = false;
        }

        jsonBuilder.Append("}");

        return jsonBuilder.ToString();
    }


    /// <summary>
    /// Runs a shell command with the specified arguments.
    /// </summary>
    /// <param name="command">The shell command to execute.</param>
    /// <param name="arguments">The arguments to pass to the command. These can be named or unnamed.</param>
    /// <param name="isNamedArguments">If true, the arguments are treated as named key-value pairs.</param>
    /// <param name="workingDirectory">The working directory for the command execution. If null, it defaults to the current directory.</param>
    /// <returns>A Response object containing the output and error.</returns>
    public static Response RunCommand(string[] command,  DictStrStr arguments, bool isNamedArguments = false, string workingDirectory = null)
    {
        var response = new Response();

        try
        {
            using (Process process = new Process())
            {
                string[] commandAndArgs = BuildCommandArguments(command, arguments, isNamedArguments);
                process.StartInfo.FileName = commandAndArgs[0]; 
                process.StartInfo.Arguments = commandAndArgs[1];
                process.StartInfo.RedirectStandardOutput = true;                  
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                if (workingDirectory != null)
                {
                    process.StartInfo.WorkingDirectory = workingDirectory;
                }
                // Print the full command
                process.Start();

                process.WaitForExit();
                // Block until the process exits
                response.Output = process.StandardOutput.ReadToEnd();
                response.Error = process.StandardError.ReadToEnd();

            }
        }
        catch (Exception ex)
        {
            response.Error = $"Exception occurred: {ex.Message}";
        }

        return response;
    }

   
    public static string[] BuildCommandArguments(string[] commands, Dictionary<string, string> arguments, bool isNamedArguments)
    {
        // The command is the first element in the commands array (e.g., "ls" or "git")
        string command = commands[0];

        // Build the arguments string by joining the remaining commands (if any)
        string argumentStr = "";
        
        // Manually append the remaining commands (from commands[1] onwards)
        if (commands.Length > 1)
        {
            for (int i = 1; i < commands.Length; i++)
            {
                argumentStr += commands[i] + " ";
            }
        }

        // Append named arguments (e.g., arg1=val1) if applicable
        if (isNamedArguments && arguments != null)
        {
            foreach (var argument in arguments)
            {
                argumentStr += $"{argument.Key}=\'{argument.Value}\' ";
            }
        }
        // Append positional arguments (e.g., "value1 value2") if not using named arguments
        else if (arguments != null)
        {
            foreach (var argument in arguments.Values)
            {
                argumentStr += $"{argument} ";
            }
        }

        // Trim trailing whitespace from the argument string
        argumentStr = argumentStr.Trim();

        // Return the command and arguments as separate array elements
        return new string[] { command, argumentStr };
    }


}
