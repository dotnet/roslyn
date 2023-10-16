// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CodeAnalysisSuggestions;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
internal sealed partial class CodeAnalysisSuggestionsCodeRefactoringProvider
    : CodeRefactoringProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeAnalysisSuggestionsCodeRefactoringProvider()
    {
    }

    public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;

        var options = document.Project.Solution.Services.ExportProvider.GetExports<IGlobalOptionService>().FirstOrDefault()?.Value;
        if (options == null)
            return;

        var configSummary = GetAnalyzerConfigSummary(document.Project, options, cancellationToken);
        if (configSummary == null)
            return;

        if (ShouldShowSuggestions(configSummary, codeQuality: true, options, out var codeQualityIdsToSkip))
        {

        }

        if (ShouldShowSuggestions(configSummary, codeQuality: false, options, out var codeStyleIdsToSkip))
        {

        }
    }
}
