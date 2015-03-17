// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.BuildTasks;
using static Microsoft.CodeAnalysis.CompilerServer.BuildProtocolConstants.RequestLanguage;

namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine
{
    public class Vbc2
    {
        public static int Main(string[] args)
            => BuildClient.RunWithConsoleOutput(args, VisualBasicCompile, Program.Main);
    }
}
