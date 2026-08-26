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
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer.CSharp;

[ExportLanguageService(typeof(ICopilotCodeAnalysisService), LanguageNames.CSharp), Shared]
internal sealed class CSharpCopilotCodeAnalysisService : AbstractCopilotCodeAnalysisService
{
    private IExternalCSharpCopilotGenerateDocumentationService? GenerateDocumentationService { get; }
    private IExternalCSharpOnTheFlyDocsService? OnTheFlyDocsService { get; }
    private IExternalCSharpCopilotGenerateImplementationService? GenerateImplementationService { get; }

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCopilotCodeAnalysisService(
        [Import(AllowDefault = true)] IExternalCSharpCopilotGenerateDocumentationService? externalCSharpCopilotGenerateDocumentationService,
        [Import(AllowDefault = true)] IExternalCSharpOnTheFlyDocsService? externalCSharpOnTheFlyDocsService,
        [Import(AllowDefault = true)] IExternalCSharpCopilotGenerateImplementationService? externalCSharpCopilotGenerateImplementationService)
    {
        if (externalCSharpCopilotGenerateDocumentationService is null)
            FatalError.ReportAndCatch(new ArgumentNullException(nameof(externalCSharpCopilotGenerateDocumentationService)), ErrorSeverity.Diagnostic);

        if (externalCSharpOnTheFlyDocsService is null)
            FatalError.ReportAndCatch(new ArgumentNullException(nameof(externalCSharpOnTheFlyDocsService)), ErrorSeverity.Diagnostic);

        if (externalCSharpCopilotGenerateImplementationService is null)
            FatalError.ReportAndCatch(new ArgumentNullException(nameof(externalCSharpCopilotGenerateImplementationService)), ErrorSeverity.Diagnostic);

        GenerateDocumentationService = externalCSharpCopilotGenerateDocumentationService;
        OnTheFlyDocsService = externalCSharpOnTheFlyDocsService;
        GenerateImplementationService = externalCSharpCopilotGenerateImplementationService;
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
