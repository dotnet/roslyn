// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    public class Program
    {
        public static int Main(string[] args) 
            => Main(args, SpecializedCollections.EmptyArray<string>());

        public static int Main(string[] args, string[] extraArgs)
            => DesktopBuildClient.Run(args, extraArgs, RequestLanguage.CSharpCompile, Csc.Run, new SimpleAnalyzerAssemblyLoader());
    }
}
