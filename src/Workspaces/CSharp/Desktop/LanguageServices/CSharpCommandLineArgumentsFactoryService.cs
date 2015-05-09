// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ICommandLineArgumentsFactoryService), LanguageNames.CSharp), Shared]
    internal class CSharpCommandLineArgumentsFactoryService : ICommandLineArgumentsFactoryService
    {
        public CommandLineArguments CreateCommandLineArguments(IEnumerable<string> arguments, string baseDirectory, bool isInteractive)
        {
            var parser = isInteractive ? CSharpCommandLineParser.Interactive : CSharpCommandLineParser.Default;
            return parser.Parse(arguments, baseDirectory, RuntimeEnvironment.GetRuntimeDirectory());
        }
    }
}
