// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    public class Program
    {
        public static int Main(string[] args)
            => CoreClrBuildClient.Run(args, RequestLanguage.CSharpCompile, Csc.Run);
    }
}
