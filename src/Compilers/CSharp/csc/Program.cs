// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                return Main(args, SpecializedCollections.EmptyArray<string>());
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                System.Console.WriteLine(ex.StackTrace);
                return 1;
            }
        }

        public static int Main(string[] args, string[] extraArgs)
            => DesktopBuildClient.Run(args, extraArgs, RequestLanguage.CSharpCompile, Csc.Run, new DesktopAnalyzerAssemblyLoader());

        public static int Run(string[] args, string clientDir, string workingDir, string sdkDir, TextWriter textWriter, IAnalyzerAssemblyLoader analyzerLoader)
            => Csc.Run(args, new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: sdkDir), textWriter, analyzerLoader);
    }
}
