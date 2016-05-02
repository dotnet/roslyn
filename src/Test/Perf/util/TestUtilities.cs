using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Performance.Utilities;

namespace Roslyn.Test.Performance.Utilities
{
    public class TestUtilities
    {
        //
        // Directory Locating Functions
        //
        public static string _myWorkingFile = null;

        public static void InitUtilities([CallerFilePath] string sourceFilePath = "")
        {
            _myWorkingFile = sourceFilePath;
        }

        /// Returns the directory that houses the currenly executing script.
        public static string MyWorkingDirectory()
        {
            if (_myWorkingFile == null)
            {
                throw new Exception("Tests must call InitUtilities before doing any path-dependent operations.");
            }
            return Directory.GetParent(_myWorkingFile).FullName;
        }

        /// Returns the directory that you can put artifacts like
        /// etl traces or compiled binaries
        public static string MyArtifactsDirectory()
        {
            var path = Path.Combine(MyWorkingDirectory(), "artifacts");
            Directory.CreateDirectory(path);
            return path;
        }

        public static string MyTempDirectory()
        {
            var workingDir = MyWorkingDirectory();
            var path = Path.Combine(workingDir, "temp");
            Directory.CreateDirectory(path);
            return path;
        }

        public static string RoslynDirectory()
        {
            var workingDir = MyWorkingDirectory();
            var binaryDebug = Path.Combine("Binaries", "Debug").ToString();
            int binaryDebugIndex = workingDir.IndexOf(binaryDebug, StringComparison.OrdinalIgnoreCase);
            if (binaryDebugIndex != -1)
            {
                return workingDir.Substring(0, binaryDebugIndex);
            }

            var binaryRelease= Path.Combine("Binaries", "Release").ToString();
            return workingDir.Substring(0, workingDir.IndexOf(binaryRelease, StringComparison.OrdinalIgnoreCase));
        }

        public static string CscPath()
        {
            return Path.Combine(MyBinaries(), "csc.exe");
        }

        public static string MyBinaries()
        {
            var workingDir = MyWorkingDirectory();
            // We may be a release or debug build
            var debug = workingDir.IndexOf("debug", StringComparison.CurrentCultureIgnoreCase);
            if (debug != -1)
                return workingDir.Substring(0, debug + "debug".Length);

            var release = workingDir.IndexOf("release", StringComparison.CurrentCultureIgnoreCase);
            if (release != -1)
                return workingDir.Substring(0, release + "release".Length);

            throw new Exception("You are attempting to run performance test from the src directory. Run it from binaries");
        }

        public static string PerfDirectory()
        {
            return Path.Combine(RoslynDirectory(), "src", "Test", "Perf");
        }

        public static string BinDirectory()
        {
            return Path.Combine(RoslynDirectory(), "Binaries");
        }

        public static string BinDebugDirectory()
        {
            return Path.Combine(BinDirectory(), "Debug");
        }

        public static string BinReleaseDirectory()
        {
            return Path.Combine(BinDirectory(), "Release");
        }

        public static string DebugCscPath()
        {
            return Path.Combine(BinDebugDirectory(), "csc.exe");
        }

        public static string ReleaseCscPath()
        {
            return Path.Combine(BinReleaseDirectory(), "csc.exe");
        }

        public static string DebugVbcPath()
        {
            return Path.Combine(BinDebugDirectory(), "vbc.exe");
        }

        public static string ReleaseVbcPath()
        {
            return Path.Combine(BinReleaseDirectory(), "vbc.exe");
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

        /// Shells out, and if the process fails, log the error
        /// and quit the script.
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

        public static string StdoutFrom(string program, bool verbose, ILogger logger, string args = "")
        {
            var result = ShellOut(program, args, verbose, logger);
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
