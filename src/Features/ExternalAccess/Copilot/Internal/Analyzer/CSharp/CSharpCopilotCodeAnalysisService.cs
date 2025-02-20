// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer.CSharp;

[ExportLanguageService(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared]
internal sealed class CSharpCopilotCodeAnalysisService : AbstractCopilotCodeAnalysisService
{
    private IExternalCSharpCopilotCodeAnalysisService? AnalysisService { get; }
    private IExternalCSharpCopilotGenerateDocumentationService? GenerateDocumentationService { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCopilotCodeAnalysisService(
        [Import(AllowDefault = true)] IExternalCSharpCopilotCodeAnalysisService? externalCopilotService,
        [Import(AllowDefault = true)] IExternalCSharpCopilotGenerateDocumentationService? externalCSharpCopilotGenerateDocumentationService,
        IDiagnosticsRefresher diagnosticsRefresher
        ) : base(diagnosticsRefresher)
    {
        if (externalCopilotService is null)
            FatalError.ReportAndCatch(new NullReferenceException("ExternalCSharpCopilotCodeAnalysisService is unavailable."), ErrorSeverity.Diagnostic);

        if (externalCSharpCopilotGenerateDocumentationService is null)
            FatalError.ReportAndCatch(new NullReferenceException("ExternalCSharpCopilotGenerateDocumentationService is unavailable."), ErrorSeverity.Diagnostic);

        AnalysisService = externalCopilotService;
        GenerateDocumentationService = externalCSharpCopilotGenerateDocumentationService;
    }

    protected override Task<ImmutableArray<Diagnostic>> AnalyzeDocumentCoreAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return AnalysisService.AnalyzeDocumentAsync(document, span, promptTitle, cancellationToken);

        return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
    }

    protected override Task<ImmutableArray<string>> GetAvailablePromptTitlesCoreAsync(Document document, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return AnalysisService.GetAvailablePromptTitlesAsync(document, cancellationToken);

        return Task.FromResult(ImmutableArray<string>.Empty);
    }

    protected override Task<ImmutableArray<Diagnostic>> GetCachedDiagnosticsCoreAsync(Document document, string promptTitle, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return AnalysisService.GetCachedDiagnosticsAsync(document, promptTitle, cancellationToken);

        return Task.FromResult(ImmutableArray<Diagnostic>.Empty);
    }

    protected override Task<bool> IsAvailableCoreAsync(CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return AnalysisService.IsAvailableAsync(cancellationToken);

        return Task.FromResult(false);
    }

    protected override Task StartRefinementSessionCoreAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return AnalysisService.StartRefinementSessionAsync(oldDocument, newDocument, primaryDiagnostic, cancellationToken);

        return Task.CompletedTask;
    }

    protected override Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsCoreAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return AnalysisService.GetOnTheFlyDocsAsync(symbolSignature, declarationCode, language, cancellationToken);

        return Task.FromResult((string.Empty, false));
    }

    protected override async Task<ImmutableArray<Diagnostic>> GetDiagnosticsIntersectWithSpanAsync(
        Document document, IReadOnlyList<Diagnostic> diagnostics, TextSpan span, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var filteredDiagnostics);

        // The location of Copilot diagnostics is on the method identifier, we'd like to expand the range to include them
        // if any part of the method intersects with the given span.
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

        foreach (var diagnostic in diagnostics)
        {
            var containingMethod = syntaxFacts.GetContainingMethodDeclaration(root, diagnostic.Location.SourceSpan.Start, useFullSpan: false);
            if (containingMethod?.Span.IntersectsWith(span) is true)
                filteredDiagnostics.Add(diagnostic);
        }

        return filteredDiagnostics.ToImmutable();
    }

    protected override Task<bool> IsFileExcludedCoreAsync(string filePath, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return AnalysisService.IsFileExcludedAsync(filePath, cancellationToken);

        return Task.FromResult(false);
    }

    protected override Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> GetDocumentationCommentCoreAsync(DocumentationCommentProposal proposal, CancellationToken cancellationToken)
    {
        if (GenerateDocumentationService is not null)
            return GenerateDocumentationService.GetDocumentationCommentAsync(new CopilotDocumentationCommentProposalWrapper(proposal), cancellationToken);

        return Task.FromResult<(Dictionary<string, string>?, bool)>((null, false));
    }
}
