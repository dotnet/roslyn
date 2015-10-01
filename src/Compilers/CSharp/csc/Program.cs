// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.BuildTasks;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    public class Program
    {
        public static int Main(string[] args) 
            => Main(args, SpecializedCollections.EmptyArray<string>());

        public static int Main(string[] args, string[] extraArgs)
            => BuildClient.RunWithConsoleOutput(
                BuildClient.GetCommandLineArgs(args).Concat(extraArgs),
                clientDir: AppDomain.CurrentDomain.BaseDirectory,
                workingDir: Directory.GetCurrentDirectory(),
                sdkDir: RuntimeEnvironment.GetRuntimeDirectory(),
                analyzerLoader: new SimpleAnalyzerAssemblyLoader(),
                language: RequestLanguage.CSharpCompile,
                fallbackCompiler: Csc.Run);
    }
}
