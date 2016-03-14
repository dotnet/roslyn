// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            try
            {
                _timeLimit = TimeSpan.Parse(_options.TimeLimit);
            }
            catch (Exception ex)
            {
                ConsoleUtils.LogError(Resources.ErrorInvalidTimeoutInterval, _options.TimeLimit, ex.Message);
                return 1;
            }

            if (_options.PollingInterval <= 0)
            {
                ConsoleUtils.LogError(Resources.ErrorInvalidPollingInterval, _options.PollingInterval);
                return 1;
            }

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = _options.Executable;
            processStartInfo.Arguments = _options.Arguments;

            using (Process process = Process.Start(processStartInfo))
            {
                while (!process.HasExited)
                {
                    if (DateTime.Now - process.StartTime > _timeLimit)
                    {
                        ConsoleUtils.LogError(
                            Resources.ErrorProcessTimedOut,
                            _options.Executable,
                            process.Id,
                            _options.TimeLimit);

                        ScreenshotSaver.SaveScreen(_options.Executable, _options.OutputDirectory);

                        ConsoleUtils.LogMessage(
                            Resources.InfoTerminatingProcess,
                            _options.Executable,
                            process.Id);

                        process.Kill();

                        return 1;
                    }

                    Thread.Sleep(_options.PollingInterval);
                }

                ConsoleUtils.LogMessage(Resources.ProcessExited, _options.Executable, process.ExitTime - process.StartTime);
            }

            return 0;
        }

        private static void Main(string[] args)
        {
            Banner();

            Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    options => Run(options),
                    err => 1);
        }

        private  static int Run(Options options)
        {
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
