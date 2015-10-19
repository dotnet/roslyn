// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    public class Program
    {
        public static int Main(string[] args)
            => Csc.Run(args: args,
                       clientDirectory: AppContext.BaseDirectory,
                       sdkDirectory: null,
                       analyzerLoader: CoreClrAnalyzerAssemblyLoader.CreateAndSetDefault());
    }
}
