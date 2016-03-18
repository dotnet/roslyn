// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "bin\Microsoft.CodeAnalysis.Scripting.dll"
#r "bin\Microsoft.CodeAnalysis.CSharp.Scripting.dll"

using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// Finds all csi files in a directory recursively, ignoring those that
/// are in the "skip" set.
IEnumerable<string> AllCsiRecursive(string start, HashSet<string> skip)
{
    IEnumerable<string> childFiles =
        from fileName in Directory.EnumerateFiles(start.ToString(), "*.csx")
        select fileName;
    IEnumerable<string> grandChildren =
        from childDir in Directory.EnumerateDirectories(start)
        where !skip.Contains(childDir)
        from fileName in AllCsiRecursive(childDir, skip)
        select fileName;
    return childFiles.Concat(grandChildren).Where((s) => !skip.Contains(s));
}

/// Runs the script at fileName and returns a task containing the
/// state of the script.
async Task<ScriptState<object>> RunFile(string fileName)
{
    var scriptOptions = ScriptOptions.Default.WithFilePath(fileName);
    var text = File.ReadAllText(fileName);
    var prelude = "System.Collections.Generic.List<string> Args = null;";
    var state = await CSharpScript.RunAsync(prelude);
    var args = state.GetVariable("Args");
    args.Value = Args;
    return await state.ContinueWithAsync<object>(text, scriptOptions);
}

