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

    protected override Task<string> GetOnTheFlyDocsPromptCoreAsync(OnTheFlyDocsInfo onTheFlyDocsInfo, CancellationToken cancellationToken)
    {
        if (OnTheFlyDocsService is not null)
            return OnTheFlyDocsService.GetOnTheFlyDocsPromptAsync(new CopilotOnTheFlyDocsInfoWrapper(onTheFlyDocsInfo), cancellationToken);

        return Task.FromResult(string.Empty);
    }

    protected override Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsResponseCoreAsync(string prompt, CancellationToken cancellationToken)
    {
        if (OnTheFlyDocsService is not null)
            return OnTheFlyDocsService.GetOnTheFlyDocsResponseAsync(prompt, cancellationToken);

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
