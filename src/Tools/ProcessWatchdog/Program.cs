// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using CommandLine;


namespace ProcessWatchdog
{
    internal sealed class Program
    {
        private Options _options;
        private TimeSpan _timeLimit;

        public Program(Options options)
        {
            _options = options;
        }

        private int Run()
        {
            if (_options.TimeLimit <= 0)
            {
                ConsoleUtils.LogError(ErrorCode.InvalidTimeLimit, Resources.ErrorInvalidTimeLimit, _options.TimeLimit);
                return 1;
            }

            _timeLimit = TimeSpan.FromSeconds(_options.TimeLimit);

            if (_options.PollingInterval <= 0)
            {
                ConsoleUtils.LogError(ErrorCode.InvalidPollingInterval, Resources.ErrorInvalidPollingInterval, _options.PollingInterval);
                return 1;
            }

            if (!File.Exists(_options.ProcDumpPath))
            {
                ConsoleUtils.LogError(ErrorCode.ProcDumpNotFound, Resources.ErrorProcDumpNotFound, _options.ProcDumpPath);
                return 1;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _options.Executable,
                Arguments = _options.Arguments,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process parentProcess = Process.Start(processStartInfo);
            ProcDump procDump = new ProcDump(_options.ProcDumpPath, _options.OutputFolder);

            using (ProcessTracker processTracker = new ProcessTracker(parentProcess, procDump))
            {
                while (!processTracker.AllFinished)
                {
                    if (DateTime.Now - parentProcess.StartTime > _timeLimit)
                    {
                        ConsoleUtils.LogError(
                            ErrorCode.ProcessTimedOut,
                            Resources.ErrorProcessTimedOut,
                            _options.Executable,
                            parentProcess.Id,
                            _options.TimeLimit);

                        if (_options.Screenshot)
                        {
                            string description = Path.GetFileNameWithoutExtension(_options.Executable);
                            ScreenshotSaver.SaveScreen(description, _options.OutputFolder);
                        }

                        processTracker.TerminateAll();
                        return 1;
                    }

                    Thread.Sleep(_options.PollingInterval);

                    processTracker.Update();
                }

                ConsoleUtils.LogMessage(
                    Resources.ProcessExited,
                    _options.Executable,
                    parentProcess.ExitTime - parentProcess.StartTime);
            }

            return 0;
        }

        private static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    options => Run(options),
                    err => 1);
        }

        private  static int Run(Options options)
        {
            // Don't display the banner until after the command line parser has
            // validated the arguments, because when the command line arguments are
            // invalid, the command line parse itself displays an banner. In that case,
            // if we displayed our banner first, you would see two of them.
            Banner();

            var program = new Program(options);
            return program.Run();
        }

        private static void Banner()
        {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            IEnumerable<Attribute> attributes = entryAssembly.GetCustomAttributes();

            string version = entryAssembly.GetName().Version.ToString();
            ConsoleUtils.LogMessage(Resources.Banner, Resources.ApplicationName, version);

            var copyrightAttribute = attributes.Single(a => a is AssemblyCopyrightAttribute) as AssemblyCopyrightAttribute;
            string copyright = copyrightAttribute.Copyright;

            ConsoleUtils.LogMessage(copyright);
            ConsoleUtils.LogMessage(string.Empty);
        }
    }
}
