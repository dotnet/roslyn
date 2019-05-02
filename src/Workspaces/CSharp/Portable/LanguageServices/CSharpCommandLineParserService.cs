// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [ExportLanguageService(typeof(ICommandLineParserService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpCommandLineParserService : ICommandLineParserService
    {
#pragma warning disable RS0033 // Importing constructor should be [Obsolete]
        [ImportingConstructor]
#pragma warning restore RS0033 // Importing constructor should be [Obsolete]
        public CSharpCommandLineParserService()
        {
        }

        public CommandLineArguments Parse(IEnumerable<string> arguments, string baseDirectory, bool isInteractive, string sdkDirectory)
        {
#if SCRIPTING
            var parser = isInteractive ? CSharpCommandLineParser.Interactive : CSharpCommandLineParser.Default;
#else
            var parser = CSharpCommandLineParser.Default;
#endif
            return parser.Parse(arguments, baseDirectory, sdkDirectory);
        }
    }
}
