using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using DictStrObj = System.Collections.Generic.Dictionary<string, object>;
using DictObjTable = System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, object>>;

using System.Text;
using UnityEngine;

public static class ImageLoader
{
    /// <summary>
    /// Loads an image from a given file path and converts it to a Sprite.
    /// </summary>
    /// <param name="filePath">The full file path of the image.</param>
    /// <param name="pivot">The pivot point of the sprite. Defaults to (0.5, 0.5).</param>
    /// <returns>The loaded Sprite, or null if the file doesn't exist or the image fails to load.</returns>
    public static Sprite LoadSpriteFromFile(string filePath, Vector2 pivot = default)
    {
        if (pivot == default)
            pivot = new Vector2(0.5f, 0.5f); // Default pivot at the center

        if (!File.Exists(filePath))
        {
            throw new System.Exception($"Image file not found at path: {filePath}");
        }

        // Read the file into a byte array
        byte[] fileData = File.ReadAllBytes(filePath);

        // Create a new texture and load the image data
        Texture2D texture = new Texture2D(2, 2); // Minimum size, will resize as needed
        if (texture.LoadImage(fileData))
        {
            // Create a sprite from the texture
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                pivot
            );
        }
        else
        {
            throw new System.Exception($"Failed to load image as Texture2D: {filePath}");
        }
    }
}


public class DJson
{

    public static void SafeCopyKey(string key, DictStrObj dest, Dictionary<string, object> src)
    {
        if (src == null)
        {
            throw new ArgumentNullException(nameof(src), "Source dictionary is null.");
        }

        if (src.TryGetValue(key, out var value))
        {
            if (value is string strValue)
            {
                dest[key] = strValue;
            }
            else if  (value is DateTime dtTime)
            {
                 dest[key] = dtTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            else
            {
                throw new InvalidCastException($"Value for key '{key}' is not a string.");
            }
        }
        else
        {
            throw new KeyNotFoundException($"Key '{key}' is missing in the source dictionary.");
        }
    }

    public static string Stringify(object jsonObject, int indentLevel = 0,bool debug=false)
    {
        var sb = new StringBuilder();
        BuildJsonTreeString(jsonObject, sb, indentLevel,debug);
        return sb.ToString();
    }
    public static (bool isValid, string error) ValidateJsonSchema(Dictionary<string, object> json, Dictionary<string, object> schema)
    {
        foreach (var key in schema.Keys)
        {
            // Check if the key exists in the json object
            if (!json.ContainsKey(key))
            {
                return (false, $"Missing required key: {key}");
            }

            // Get the expected type from the schema
            var expectedType = schema[key];
            var actualValue = json[key];

            // Check for nested dictionary structure
            if (expectedType is Dictionary<string, object> nestedSchema)
            {
                if (actualValue is not Dictionary<string, object> nestedJson)
                {
                    return (false, $"Expected '{key}' to be a nested object, but got {actualValue.GetType()}");
                }
                
                // Recursive call for nested validation
                var (isNestedValid, nestedError) = ValidateJsonSchema(nestedJson, nestedSchema);
                if (!isNestedValid)
                {
                    return (false, nestedError); // Return first encountered error in nested object
                }
            }
            else
            {
                // Check the type of the current field
                if (!IsValidType(actualValue, expectedType))
                {
                    return (false, $"Invalid type for '{key}': Expected {expectedType}, got {actualValue.GetType()}");
                }
            }
        }

        // If all checks passed, return true
        return (true, "");
    }

    private static bool IsValidType(object value, object expectedType)
    {
        return expectedType switch
        {
            "string" => value is string,
            "int" => value is int,
            "bool" => value is bool,
            _ => throw new InvalidOperationException($"Unknown type in schema: {expectedType}")
        };
    }

    // Example usage

    private static void BuildJsonTreeString(object jsonObjectIn, StringBuilder sb, int indentLevel,bool debug = false)
    {
        string indent = new string(' ', indentLevel * 4);
        object jsonObject = jsonObjectIn;
        if (jsonObjectIn is Dictionary<string, Dictionary<string, object>>)
        {
            jsonObject = new Dictionary<string, object>();
            foreach(string key2 in ((Dictionary<string, Dictionary<string, object>>)jsonObjectIn).Keys)
            {
                ((Dictionary<string, object>)jsonObject)[key2] =  ((Dictionary<string, Dictionary<string, object>>)jsonObjectIn)[key2];
            }
        }


        if (jsonObject is Dictionary<string, object> dict)
        {
            sb.AppendLine($"{indent}{{");  // Opening brace for a dictionary
            foreach (var kvp in dict)
            {
                sb.Append($"{indent}    \"{kvp.Key}\": ");
                BuildJsonTreeString(kvp.Value, sb, indentLevel + 1);
            }
            sb.AppendLine($"{indent}}}");  // Closing brace for a dictionary
        }
        else if (jsonObject is List<object> list)
        {
            sb.AppendLine($"{indent}[");  // Opening bracket for a list
            foreach (var item in list)
            {
                BuildJsonTreeString(item, sb, indentLevel + 1);
            }
            sb.AppendLine($"{indent}]");  // Closing bracket for a list
        }
        else
        {

            // Print value directly if it’s a primitive type
            if (jsonObject is string)
            {
                sb.AppendLine($"\"{jsonObject}\"");
            }
            else
            {
                sb.AppendLine($"{jsonObject.ToString()}");
            }
        }
    }
    public static Dictionary<string, string> ToDictStrStr(Dictionary<string, object> input)
    {
        var result = new Dictionary<string, string>();
        foreach (var kvp in input)
        {
            result[kvp.Key] = (string)kvp.Value; // Hard cast, will throw if incompatible
        }
        return result;
    }

    public static Dictionary<string, object> Parse(string json)
    {
        // Deserialize the JSON to a JObject first
        var jsonObject = JsonConvert.DeserializeObject<JObject>(json);

        // Recursively convert JObject to a Dictionary
        return ParseJObject(jsonObject);
    }

    private static Dictionary<string, object> ParseJObject(JObject jObject)
    {
        var dictionary = new Dictionary<string, object>();

        foreach (var property in jObject.Properties())
        {
            var value = property.Value;

            if (value is JObject nestedObject)
            {
                // Recursively convert JObject to Dictionary
                dictionary[property.Name] = ParseJObject(nestedObject);
            }
            else if (value is JArray array)
            {
                // Convert JArray to a List of objects
                dictionary[property.Name] = ParseJArray(array);
            }
            else
            {
                // Add simple values directly
                dictionary[property.Name] = ((JValue)value).Value;
            }
        }

        return dictionary;
    }

    private static List<object> ParseJArray(JArray array)
    {
        var list = new List<object>();

        foreach (var item in array)
        {
            if (item is JObject nestedObject)
            {
                // Recursively convert JObject to Dictionary
                list.Add(ParseJObject(nestedObject));
            }
            else if (item is JArray nestedArray)
            {
                // Recursively convert nested JArray to List
                list.Add(ParseJArray(nestedArray));
            }
            else
            {
                // Add simple values directly
                list.Add(((JValue)item).Value);
            }
        }

        return list;
    }
}


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

    public static DictObjTable ParseJsonToDictTable(string json)
    {
        var result = new DictObjTable();

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
            var innerDict = new DictStrObj();
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
    public static string BuildJsonFromDictTable(DictObjTable records)
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
    public static Response RunCommand(string[] command,  DictStrObj arguments, bool isNamedArguments = false, string workingDirectory = null)
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


    public static Process StartProcess(string[] command, DictStrObj arguments, bool isNamedArguments = false, string workingDirectory = null)
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
            process.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
            process.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;            

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

   
    public static string[] BuildCommandArguments(string[] commands, DictStrObj arguments, bool isNamedArguments)
    {
        // The command is the first element in the commands array (e.g., "ls" or "git")
        string command = commands[0];
        //if (command.Contains(" ")) command = $"\"{command}\"";
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
