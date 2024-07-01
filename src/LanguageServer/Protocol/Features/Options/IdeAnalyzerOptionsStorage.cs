// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal static class IdeAnalyzerOptionsStorage
{
    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, Project project)
        => GetIdeAnalyzerOptions(globalOptions, project.Services);

    public static IdeAnalyzerOptions GetIdeAnalyzerOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
    {
        var language = languageServices.Language;

        // avoid throwing for languages other than C# and VB:
        var supportsLanguageSpecificOptions = languageServices.GetService<ISyntaxFormattingService>() != null;

        return new()
        {
            CrashOnAnalyzerException = globalOptions.GetOption(CrashOnAnalyzerException),
            CleanCodeGenerationOptions = supportsLanguageSpecificOptions ? globalOptions.GetCleanCodeGenerationOptions(languageServices) : null,
            CodeStyleOptions = supportsLanguageSpecificOptions ? globalOptions.GetCodeStyleOptions(languageServices) : null,
        };
    }

    public static readonly Option2<bool> CrashOnAnalyzerException = new(
        "dotnet_crash_on_analyzer_exception", IdeAnalyzerOptions.CommonDefault.CrashOnAnalyzerException);
}
