// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using CommandLine;


namespace ProcessWatchdog
{
    internal sealed class Program
    {
        private Options _options;
        private TimeSpan _timeout;
        private bool _exited;
        private DateTime _startTime;

        public Program(Options options)
        {
            _options = options;
        }

        private int Run()
        {
            try
            {
                _timeout = TimeSpan.Parse(_options.Timeout);
            }
            catch (Exception ex)
            {
                ConsoleUtils.ReportError(Resources.ErrorInvalidTimeoutInterval, _options.Timeout, ex.Message);
                return 1;
            }

            var processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = _options.Executable;
            processStartInfo.Arguments = _options.Arguments;

            using (Process process = Process.Start(processStartInfo))
            {
                while (!process.HasExited)
                {
                    if (DateTime.Now - process.StartTime > _timeout)
                    {
                        ConsoleUtils.ReportError(Resources.ErrorProcessTimedOut, _options.Executable, _options.Timeout);
                        return 1;
                    }

                    Thread.Sleep(1000);
                }

                Console.WriteLine(Resources.ProcessExited, _options.Executable, process.ExitTime - process.StartTime);
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
            Console.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.Banner, Resources.ApplicationName, version));

            var copyrightAttribute = attributes.Single(a => a is AssemblyCopyrightAttribute) as AssemblyCopyrightAttribute;
            string copyright = copyrightAttribute.Copyright;

            Console.WriteLine(copyright);
            Console.WriteLine();
        }
    }
}
