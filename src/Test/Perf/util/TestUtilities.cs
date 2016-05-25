using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Test.Performance.Utilities
{
    internal class TestUtilities
    {
        //
        // Directory Locating Functions
        //
        public static string _myWorkingFile = null;

        /// <summary>
        /// This method should be called *ONLY* by the csx scripts.
        /// This method *MUST NOT* be called from within the library itself. If this method is called within the library instead of csx scripts
        /// then <paramref name="sourceFilePath"/> will be set to the path of the file when the library is actual built.
        /// For Eg: If SomeLibarayFile.cs calls this method and if the library is built is some build machine where SomeLibarayFile.cs is saved at
        /// Y:/Project/SomeLibarayFile.cs then when the library is used in any machine where there is no Y: drive then we will see errors saying
        /// invalid directory path. Also note that <paramref name="sourceFilePath"/> will be set to "Y:/Project/SomeLibarayFile.cs" and not set to
        /// the path of the file from where the call to SomeLibarayFile.cs which in turn called <see cref="InitUtilitiesFromCsx(string)"/>
        /// </summary>
        public static void InitUtilitiesFromCsx([CallerFilePath] string sourceFilePath = "")
        {
            _myWorkingFile = sourceFilePath;
        }

        /// Returns the directory that houses the currenly executing script.
        public static string MyWorkingDirectory()
        {
            if (_myWorkingFile == null)
            {
                throw new Exception("Tests must call InitUtilitiesFromCsx before doing any path-dependent operations.");
            }
            return Directory.GetParent(_myWorkingFile).FullName;
        }

        public static string GetCPCDirectoryPath()
        {
            return Environment.ExpandEnvironmentVariables(@"%SYSTEMDRIVE%\CPC");
        }

        public static string GetViBenchToJsonExeFilePath()
        {
            return Path.Combine(GetCPCDirectoryPath(), "ViBenchToJson.exe");
        }

        //
        // Process spawning and error handling.
        //

        public class ProcessResult
        {
            public string ExecutablePath { get; set; }
            public string Args { get; set; }
            public int Code { get; set; }
            public string StdOut { get; set; }
            public string StdErr { get; set; }

            public bool Failed => Code != 0;
            public bool Succeeded => !Failed;
        }

        // Shells out, and if the process fails, log the error
        /// and quit the script.
        /// NOTE: <paramref name="workingDirectory"/> should be set when called inside the library and not from csx. see <see cref="InitUtilitiesFromCsx(string)"/>
        /// for more information
        public static void ShellOutVital(
                string file,
                string args,
                bool verbose,
                ILogger logger,
                string workingDirectory = null,
                CancellationToken cancellationToken = default(CancellationToken))
        {
            var result = ShellOut(file, args, verbose, logger, workingDirectory, cancellationToken);
            if (result.Failed)
            {
                LogProcessResult(result, logger);
                throw new System.Exception("ShellOutVital Failed");
            }
        }

        /// NOTE: <paramref name="workingDirectory"/> should be set when called inside the library and not from csx. see <see cref="InitUtilitiesFromCsx(string)"/>
        /// for more information
        public static ProcessResult ShellOut(
                string file,
                string args,
                bool verbose,
                ILogger logger,
                string workingDirectory = null,
                CancellationToken cancellationToken = default(CancellationToken))
        {
            if (workingDirectory == null)
            {
                workingDirectory = MyWorkingDirectory();
            }

            var tcs = new TaskCompletionSource<ProcessResult>();
            var startInfo = new ProcessStartInfo(file, args);
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = workingDirectory;
            var process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            if (cancellationToken != default(CancellationToken))
            {
                cancellationToken.Register(() => process.Kill());
            }

            if (verbose)
            {
                logger.Log($"running \"{file}\" with arguments \"{args}\" from directory {workingDirectory}");
            }

            process.Start();

            var output = new StringWriter();
            var error = new StringWriter();

            process.OutputDataReceived += (s, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    output.WriteLine(e.Data);
                }
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    error.WriteLine(e.Data);
                }
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            return new ProcessResult
            {
                ExecutablePath = file,
                Args = args,
                Code = process.ExitCode,
                StdOut = output.ToString(),
                StdErr = error.ToString(),
            };
        }

        public static string StdoutFrom(string program, bool verbose, ILogger logger, string args = "", string workingDirectory = null)
        {
            var result = ShellOut(program, args, verbose, logger, workingDirectory);
            if (result.Failed)
            {
                LogProcessResult(result, logger);
                throw new Exception("Shelling out failed");
            }
            return result.StdOut.Trim();
        }

        /// Logs a message.
        ///
        /// The actual implementation of this method may change depending on
        /// if the script is being run standalone or through the test runner.
        public static void Log(string info, string logFile)
        {
            System.Console.WriteLine(info);
            if (logFile != null)
            {
                File.AppendAllText(logFile, info + System.Environment.NewLine);
            }
        }

        /// Logs the result of a finished process
        public static void LogProcessResult(ProcessResult result, ILogger logger)
        {
            logger.Log(String.Format("The process \"{0}\" {1} with code {2}",
                $"{result.ExecutablePath} {result.Args}",
                result.Failed ? "failed" : "succeeded",
                result.Code));
            logger.Log($"Standard Out:\n{result.StdOut}");
            logger.Log($"\nStandard Error:\n{result.StdErr}");
        }
    }
}
