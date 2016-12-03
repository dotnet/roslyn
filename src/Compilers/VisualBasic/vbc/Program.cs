﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine
{
    public class Program
    {
        public static int Main(string[] args)
            => Main(args, Array.Empty<string>());

        public static int Main(string[] args, string[] extraArgs)
            => DesktopBuildClient.Run(args, extraArgs, RequestLanguage.VisualBasicCompile, Vbc.Run, new DesktopAnalyzerAssemblyLoader());

        public static int Run(string[] args, string clientDir, string workingDir, string sdkDir, string tempDir, TextWriter textWriter, IAnalyzerAssemblyLoader analyzerLoader)
            => Vbc.Run(args, new BuildPaths(clientDir: clientDir, workingDir: workingDir, sdkDir: sdkDir, tempDir: tempDir), textWriter, analyzerLoader);
    }
}
