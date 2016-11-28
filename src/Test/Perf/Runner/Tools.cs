// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;


namespace Roslyn.Test.Performance.Runner
{
    public static class Tools
    {
        /// Runs the script at fileName and returns a task containing the
        /// state of the script.
        public static async Task<ScriptState<object>> RunFile(string fileName)
        {
            var scriptOptions = ScriptOptions.Default.WithFilePath(fileName);
            var text = File.ReadAllText(fileName);
            var prelude = "System.Collections.Generic.List<string> Args = null;";
            var state = await CSharpScript.RunAsync(prelude).ConfigureAwait(false);
            var args = state.GetVariable("Args");

            var newArgs = new List<string>(System.Environment.GetCommandLineArgs());
            newArgs.Add("--from-runner");
            args.Value = newArgs;
            return await state.ContinueWithAsync<object>(text, scriptOptions).ConfigureAwait(false);
        }

        /// Gets all csx file recursively in a given directory
        public static IEnumerable<string> GetAllCsxRecursive(string directoryName)
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

        public static void CopyDirectory(string source, string destination, string argument = @"/mir")
        {
            var result = ShellOut("Robocopy", $"{argument} {source} {destination}", "");

            // Robocopy has a success exit code from 0 - 7
            if (result.Code > 7)
            {
                throw new IOException($"Failed to copy \"{source}\" to \"{destination}\".");
            }
        }

        public static string FirstLine(string input)
        {
            return input.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)[0];
        }
    }
}
