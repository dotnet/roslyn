// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace BuildValidator
{
    internal class Options
    {
        private Options()
        { }

        public bool ConsoleOutput { get; private set; } = true;
        public bool Verbose { get; private set; }
        public bool IgnoreCompilerVersion { get; private set; }

        public static Options Create(string[] args)
        {
            var options = new Options();

            for (var i = 0; i < args.Length;)
            {
                var arg = args[i++];

                switch (arg)
                {
                    case "/verbose":
                        options.Verbose = true;
                        break;

                    case "/quiet":
                        options.ConsoleOutput = false;
                        break;

                    case "/ignorecompilerversion":
                        options.IgnoreCompilerVersion = true;
                        break;

                        // TODO: allow specifying a path to assemblies to validate
                        // do we have to roll our own command line parsing?
                        // case "/assembliesPath":
                        //     options.IgnoreCompilerVersion = true;
                        //     break;
                }
            }

            return options;
        }
    }

    internal static class TestData
    {
        /*
        internal static string SourceDirectory => @"p:\temp\simple-rebuild";
        internal static string ArtifactsDirectory => @"p:\temp\simple-rebuild\bin\Release\netcoreapp3.1";
        internal static IEnumerable<string> BinaryNames => new[]
        {
            "simple-rebuild.dll"
        };
        internal static string DebugDirectory => @"p:\temp\hw";
        */

        internal static string SourceDirectory => @"p:\runfo";
        internal static string ArtifactsDirectory => @"P:\runfo\";
        internal static IEnumerable<string> BinaryNames => new[]
        {
            @"DevOps.Status\bin\Debug\netcoreapp3.1\DevOps.Util.dll",
            @"DevOps.Status\bin\Debug\netcoreapp3.1\DevOps.Util.DotNet.dll",
            @"DevOps.Status\bin\Debug\netcoreapp3.1\DevOps.Status.dll",
            @"DevOps.Status\bin\Debug\netcoreapp3.1\DevOps.Status.Views.dll",
        };
        internal static string DebugDirectory => @"p:\temp\hw";
    }
}
