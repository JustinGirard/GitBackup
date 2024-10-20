using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using DictStrStr = System.Collections.Generic.Dictionary<string, string>;
using DictTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>;

using System.Threading.Tasks;

using UnityEngine;


public class JsonParser
{
    public static object ParseJsonObjects(string input)
    {
        List<object> parsedResults = new List<object>();
        int length = input.Length;
        bool inString = false;
        bool escape = false;

        for (int i = 0; i < length; i++)
        {
            char c = input[i];

            if (escape)
            {
                escape = false;
            }
            else if (c == '\\')
            {
                escape = true;
            }
            else if (c == '"')
            {
                inString = !inString;
            }
            else if (!inString && (c == '{' || c == '['))
            {
                int endIndex = FindMatchingBrace(input, i);
                if (endIndex != -1)
                {
                    int substringLength = endIndex - i + 1;
                    string jsonSubstring = input.Substring(i, substringLength);
                    try
                    {
                        JToken token = JToken.Parse(jsonSubstring);
                        object result = ConvertJToken(token);
                        if (result != null)
                        {
                            parsedResults.Add(result);
                        }
                    }
                    catch (JsonReaderException)
                    {
                        // Ignore parsing errors
                    }
                    i = endIndex; // Move to the end of the current JSON substring
                }
            }
        }

        // If only one top-level JSON was found, return it directly; otherwise, return a list of results.
        return parsedResults.Count == 1 ? parsedResults[0] : parsedResults;
    }

    private static int FindMatchingBrace(string input, int startIndex)
    {
        char openingBrace = input[startIndex];
        char closingBrace = (openingBrace == '{') ? '}' : ']';
        int length = input.Length;
        bool inString = false;
        bool escape = false;
        int nestingLevel = 1;

        for (int i = startIndex + 1; i < length; i++)
        {
            char c = input[i];

            if (escape)
            {
                escape = false;
            }
            else if (c == '\\')
            {
                escape = true;
            }
            else if (c == '"')
            {
                inString = !inString;
            }
            else if (!inString)
            {
                if (c == openingBrace)
                {
                    nestingLevel++;
                }
                else if (c == closingBrace)
                {
                    nestingLevel--;
                    if (nestingLevel == 0)
                    {
                        return i;
                    }
                }
            }
        }

        return -1; // No matching closing brace/bracket found
    }

    private static object ConvertJToken(JToken token)
    {
        if (token.Type == JTokenType.Object)
        {
            var dict = new Dictionary<string, object>();
            foreach (JProperty property in token.Children<JProperty>())
            {
                dict.Add(property.Name, ConvertJToken(property.Value));
            }
            return dict;
        }
        else if (token.Type == JTokenType.Array)
        {
            var list = new List<object>();
            foreach (JToken item in token.Children())
            {
                list.Add(ConvertJToken(item));
            }
            return list;
        }
        else
        {
            // Return the primitive type (string, number, etc.)
            return token.ToObject<object>();
        }
    }
}



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


public static Process StartProcess(string[] command, DictStrStr arguments, bool isNamedArguments = false, string workingDirectory = null)
{
    Process process = new Process();

    try
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

        process.Start();
    }
    catch (Exception ex)
    {
        process.Dispose();
        throw new Exception($"Exception occurred while starting process: {ex.Message}");
    }

    return process;
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
