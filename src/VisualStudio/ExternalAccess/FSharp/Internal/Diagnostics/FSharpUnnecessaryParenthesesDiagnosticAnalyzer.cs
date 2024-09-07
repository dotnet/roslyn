// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Diagnostics;

[Shared]
[ExportLanguageService(typeof(FSharpUnnecessaryParenthesesDiagnosticAnalyzerService), LanguageNames.FSharp)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FSharpUnnecessaryParenthesesDiagnosticAnalyzerService(IFSharpUnnecessaryParenthesesDiagnosticAnalyzer analyzer) : ILanguageService
{
    public Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(DiagnosticDescriptor descriptor, Document document, CancellationToken cancellationToken)
        => analyzer.AnalyzeSyntaxAsync(descriptor, document, cancellationToken);
}

[DiagnosticAnalyzer(LanguageNames.FSharp)]
internal sealed class FSharpUnnecessaryParenthesesDiagnosticAnalyzer : DocumentDiagnosticAnalyzer, IBuiltInAnalyzer
{
    private readonly DiagnosticDescriptor _descriptor =
        new(
            IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId,
            new LocalizableResourceString(nameof(AnalyzersResources.Remove_unnecessary_parentheses), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            new LocalizableResourceString(nameof(AnalyzersResources.Parentheses_can_be_removed), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            DiagnosticCategory.Style,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            customTags: FSharpDiagnosticCustomTags.Unnecessary);

    public FSharpUnnecessaryParenthesesDiagnosticAnalyzer() => SupportedDiagnostics = [_descriptor];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    public override Task<ImmutableArray<Diagnostic>> AnalyzeSemanticsAsync(Document document, CancellationToken cancellationToken)
        => Task.FromResult<ImmutableArray<Diagnostic>>([]);

    public override Task<ImmutableArray<Diagnostic>> AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
        => document.Project.Services.GetService<FSharpUnnecessaryParenthesesDiagnosticAnalyzerService>()?.AnalyzeSyntaxAsync(_descriptor, document, cancellationToken)
        ?? Task.FromResult<ImmutableArray<Diagnostic>>([]);

    public bool IsHighPriority => false;

    public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;
}
