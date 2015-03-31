// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.BuildTasks;
using System;
using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var clientDir = AppDomain.CurrentDomain.BaseDirectory;
            return BuildClient.RunWithConsoleOutput(
                args,
                clientDir: clientDir,
                workingDir: Environment.CurrentDirectory,
                language: RequestLanguage.CSharpCompile,
                fallbackCompiler: x => Csc.Run(clientDir, x));
        }
    }
}
