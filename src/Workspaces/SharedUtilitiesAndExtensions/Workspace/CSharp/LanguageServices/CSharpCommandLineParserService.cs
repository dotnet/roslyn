// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp;

[ExportLanguageService(typeof(ICommandLineParserService), LanguageNames.CSharp), Shared]
internal sealed class CSharpCommandLineParserService : ICommandLineParserService
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpCommandLineParserService()
    {
    }

    public CommandLineArguments Parse(IEnumerable<string> arguments, string? baseDirectory, bool isInteractive, string? sdkDirectory)
    {
#if SCRIPTING
        var parser = isInteractive ? CSharpCommandLineParser.Interactive : CSharpCommandLineParser.Default;
#else
        var parser = CSharpCommandLineParser.Default;
#endif
        return parser.Parse(arguments, baseDirectory, sdkDirectory);
    }
}
