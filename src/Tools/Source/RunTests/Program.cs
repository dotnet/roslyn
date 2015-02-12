// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal sealed class Program
    {
        internal static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintUsage();
                return 1;
            }

            var xunitPath = args[0];
            var skipCount = 1;
            var test64 = false;
            if (StringComparer.OrdinalIgnoreCase.Equals(args[1], "-test64"))
            {
                skipCount = 2;
                test64 = true;
            }

            var list = new List<string>(args.Skip(skipCount));
            if (list.Count == 0)
            {
                PrintUsage();
                return 1;
            }

            var xunit = test64
                ? Path.Combine(xunitPath, "xunit.console.exe")
                : Path.Combine(xunitPath, "xunit.console.x86.exe");

            var testRunner = new TestRunner(xunit, test64);
            var start = DateTime.Now;
            Console.WriteLine("Running {0} tests", list.Count);
            var result = testRunner.RunAll(list).Result;
            var span = DateTime.Now - start;
            if (!result)
            {
                ConsoleUtil.WriteLine(ConsoleColor.Red, "Test failures encountered: {0}", span);
                return 1;
            }

            Console.WriteLine("All tests passed: {0}", span);
            return 0;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("runtests [xunit-console-runner] [assembly1] [assembly2] [...]");
        }
    }
}
