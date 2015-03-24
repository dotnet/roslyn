// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.BuildTasks;
using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants;

namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine
{
    public class Program
    {
        public static int Main(string[] args)
            => BuildClient.RunWithConsoleOutput(
                args,
                clientDir: AppContext.BaseDirectory,
                workingDir: Directory.GetCurrentDirectory(),
                sdkDir: @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\mscorlib.dll",
                language: RequestLanguage.VisualBasicCompile,
                fallbackCompiler: Vbc.Run);
    }
}
