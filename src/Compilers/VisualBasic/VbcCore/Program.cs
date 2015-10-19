﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine
{
    public class Program
    {
        public static int Main(string[] args)
            => Vbc.Run(args: args,
                       clientDirectory: AppContext.BaseDirectory,
                       sdkDirectory: @"C:\Windows\Microsoft.NET\Framework\v4.0.30319",
                       analyzerLoader: CoreClrAnalyzerAssemblyLoader.CreateAndSetDefault());
    }
}
