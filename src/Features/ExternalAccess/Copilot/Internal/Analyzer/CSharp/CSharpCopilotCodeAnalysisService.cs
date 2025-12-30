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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer.CSharp;

[ExportLanguageService(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared]
internal sealed class CSharpCopilotCodeAnalysisService : AbstractCopilotCodeAnalysisService
{
    private IExternalCSharpCopilotCodeAnalysisService? AnalysisService { get; }
    private IExternalCSharpCopilotGenerateDocumentationService? GenerateDocumentationService { get; }
    private IExternalCSharpOnTheFlyDocsService? OnTheFlyDocsService { get; }
    private IExternalCSharpCopilotGenerateImplementationService? GenerateImplementationService { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCopilotCodeAnalysisService(
        [Import(AllowDefault = true)] IExternalCSharpCopilotCodeAnalysisService? externalCopilotService,
        [Import(AllowDefault = true)] IExternalCSharpCopilotGenerateDocumentationService? externalCSharpCopilotGenerateDocumentationService,
        [Import(AllowDefault = true)] IExternalCSharpOnTheFlyDocsService? externalCSharpOnTheFlyDocsService,
        [Import(AllowDefault = true)] IExternalCSharpCopilotGenerateImplementationService? externalCSharpCopilotGenerateImplementationService,
        IDiagnosticsRefresher diagnosticsRefresher
        ) : base(diagnosticsRefresher)
    {
        if (externalCopilotService is null)
            FatalError.ReportAndCatch(new ArgumentNullException(nameof(externalCopilotService)), ErrorSeverity.Diagnostic);

        if (externalCSharpCopilotGenerateDocumentationService is null)
            FatalError.ReportAndCatch(new ArgumentNullException(nameof(externalCSharpCopilotGenerateDocumentationService)), ErrorSeverity.Diagnostic);

        if (externalCSharpOnTheFlyDocsService is null)
            FatalError.ReportAndCatch(new ArgumentNullException(nameof(externalCSharpOnTheFlyDocsService)), ErrorSeverity.Diagnostic);

        if (externalCSharpCopilotGenerateImplementationService is null)
            FatalError.ReportAndCatch(new ArgumentNullException(nameof(externalCSharpCopilotGenerateImplementationService)), ErrorSeverity.Diagnostic);

        AnalysisService = externalCopilotService;
        GenerateDocumentationService = externalCSharpCopilotGenerateDocumentationService;
        OnTheFlyDocsService = externalCSharpOnTheFlyDocsService;
        GenerateImplementationService = externalCSharpCopilotGenerateImplementationService;
    }

    protected override async Task<ImmutableArray<Diagnostic>> AnalyzeDocumentCoreAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return await AnalysisService.AnalyzeDocumentAsync(document, span, promptTitle, cancellationToken).ConfigureAwait(false);

        return ImmutableArray<Diagnostic>.Empty;
    }

    protected override async Task<ImmutableArray<string>> GetAvailablePromptTitlesCoreAsync(Document document, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return await AnalysisService.GetAvailablePromptTitlesAsync(document, cancellationToken).ConfigureAwait(false);

        return ImmutableArray<string>.Empty;
    }

    protected override async Task<ImmutableArray<Diagnostic>> GetCachedDiagnosticsCoreAsync(Document document, string promptTitle, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return await AnalysisService.GetCachedDiagnosticsAsync(document, promptTitle, cancellationToken).ConfigureAwait(false);

        return ImmutableArray<Diagnostic>.Empty;
    }

    protected override async Task<bool> IsAvailableCoreAsync(CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return await AnalysisService.IsAvailableAsync(cancellationToken).ConfigureAwait(false);

        return false;
    }

    protected override async Task StartRefinementSessionCoreAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            await AnalysisService.StartRefinementSessionAsync(oldDocument, newDocument, primaryDiagnostic, cancellationToken).ConfigureAwait(false);
    }

    protected override async Task<string> GetOnTheFlyDocsPromptCoreAsync(OnTheFlyDocsInfo onTheFlyDocsInfo, CancellationToken cancellationToken)
    {
        if (OnTheFlyDocsService is not null)
            return await OnTheFlyDocsService.GetOnTheFlyDocsPromptAsync(new CopilotOnTheFlyDocsInfoWrapper(onTheFlyDocsInfo), cancellationToken).ConfigureAwait(false);

        return string.Empty;
    }

    protected override async Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsResponseCoreAsync(string prompt, CancellationToken cancellationToken)
    {
        if (OnTheFlyDocsService is not null)
            return await OnTheFlyDocsService.GetOnTheFlyDocsResponseAsync(prompt, cancellationToken).ConfigureAwait(false);

        return (string.Empty, false);
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

    protected override async Task<bool> IsFileExcludedCoreAsync(string filePath, CancellationToken cancellationToken)
    {
        if (AnalysisService is not null)
            return await AnalysisService.IsFileExcludedAsync(filePath, cancellationToken).ConfigureAwait(false);

        return false;
    }

    protected override async Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> GetDocumentationCommentCoreAsync(DocumentationCommentProposal proposal, CancellationToken cancellationToken)
    {
        if (GenerateDocumentationService is not null)
            return await GenerateDocumentationService.GetDocumentationCommentAsync(new CopilotDocumentationCommentProposalWrapper(proposal), cancellationToken).ConfigureAwait(false);

        return (null, false);
    }

    protected override bool IsImplementNotImplementedExceptionsAvailableCore()
    {
        return GenerateImplementationService is not null;
    }

    protected override async Task<ImmutableDictionary<SyntaxNode, ImplementationDetails>> ImplementNotImplementedExceptionsCoreAsync(
        Document document,
        ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(GenerateImplementationService);
        var nodeToWrappers = await GenerateImplementationService.ImplementNotImplementedExceptionsAsync(document, methodOrProperties, cancellationToken).ConfigureAwait(false);

        var resultBuilder = ImmutableDictionary.CreateBuilder<SyntaxNode, ImplementationDetails>();
        foreach (var nodeToWrapper in nodeToWrappers)
        {
            resultBuilder.Add(
                nodeToWrapper.Key,
                new ImplementationDetails
                {
                    ReplacementNode = nodeToWrapper.Value.ReplacementNode,
                    Message = nodeToWrapper.Value.Message
                });
        }

        return resultBuilder.ToImmutable();
    }
}
