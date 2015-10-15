// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.CommandLine
{
    public class Csc2
    {
        public static int Main(string[] args)
            => Program.Main(args, new[] {"/shared" });
    }
}
