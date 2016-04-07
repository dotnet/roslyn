// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#load "test_util.csx"

#r "../infra/bin/Microsoft.CodeAnalysis.Scripting.dll"
#r "../infra/bin/Microsoft.CodeAnalysis.CSharp.Scripting.dll"

using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

/// Runs the script at fileName and returns a task containing the
/// state of the script.
async Task<ScriptState<object>> RunFile(string fileName)
{
    var scriptOptions = ScriptOptions.Default.WithFilePath(fileName);
    var text = File.ReadAllText(fileName);
    var prelude = "System.Collections.Generic.List<string> Args = null;";
    var state = await CSharpScript.RunAsync(prelude);
    var args = state.GetVariable("Args");
    
    var newArgs = new List<string>(Args);
    newArgs.Add("--from-runner");
    args.Value = newArgs;
    return await state.ContinueWithAsync<object>(text, scriptOptions);
}

/// Gets all csx file recursively in a given directory
IEnumerable<string> GetAllCsxRecursive(string directoryName)
{
    foreach (var fileName in Directory.EnumerateFiles(directoryName, "*.csx"))
    {
        yield return fileName;
    }


    foreach (var childDir in Directory.EnumerateDirectories(directoryName))
    {
        foreach (var fileName in GetAllCsxRecursive(childDir))
        {
            yield return fileName;
        }
    }
}