using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using Roslyn.Test.Performance.Utilities;
using System.Threading.Tasks;
using static Roslyn.Test.Performance.Utilities.TestUtilities;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;


namespace Roslyn.Test.Performance.Runner
{
    public class Tools
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


        public static void UploadTraces(string sourceFolderPath, string destinationFolderPath)
        {
            RuntimeSettings.logger.Log("Uploading traces");
            if (Directory.Exists(sourceFolderPath))
            {
                var directoriesToUpload = new DirectoryInfo(sourceFolderPath).GetDirectories("DataBackup*");
                if (directoriesToUpload.Count() == 0)
                {
                    RuntimeSettings.logger.Log($"There are no trace directory starting with DataBackup in {sourceFolderPath}");
                    return;
                }

                var perfResultDestinationFolderName = string.Format("PerfResults-{0:yyyy-MM-dd_hh-mm-ss-tt}", DateTime.Now);

                var destination = Path.Combine(destinationFolderPath, perfResultDestinationFolderName);
                foreach (var directoryToUpload in directoriesToUpload)
                {
                    var destinationDataBackupDirectory = Path.Combine(destination, directoryToUpload.Name);
                    if (Directory.Exists(destinationDataBackupDirectory))
                    {
                        Directory.CreateDirectory(destinationDataBackupDirectory);
                    }

                    CopyDirectory(directoryToUpload.FullName, RuntimeSettings.logger, destinationDataBackupDirectory);
                }

                foreach (var file in new DirectoryInfo(sourceFolderPath).GetFiles().Where(f => f.Name.StartsWith("ConsumptionTemp", StringComparison.OrdinalIgnoreCase) || f.Name.StartsWith("Roslyn-", StringComparison.OrdinalIgnoreCase)))
                {
                    File.Copy(file.FullName, Path.Combine(destination, file.Name));
                }
            }
            else
            {
                RuntimeSettings.logger.Log($"sourceFolderPath: {sourceFolderPath} does not exist");
            }
        }

        public static void CopyDirectory(string source, ILogger logger, string destination, string argument = @"/mir")
        {
            var result = ShellOut("Robocopy", $"{argument} {source} {destination}", workingDirectory: "");

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
