// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.QuickInfo;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.Analyzer;

/// <summary>
/// Provides access to Copilot features implemented outside Roslyn.
/// </summary>
internal abstract class AbstractCopilotCodeAnalysisService : ICopilotCodeAnalysisService
{
    protected abstract Task<bool> IsOnTheFlyDocsAvailableCoreAsync(CancellationToken cancellationToken);
    protected abstract Task<bool> IsFileExcludedFromOnTheFlyDocsCoreAsync(string filePath, CancellationToken cancellationToken);
    protected abstract Task<string> GetOnTheFlyDocsPromptCoreAsync(OnTheFlyDocsInfo onTheFlyDocsInfo, CancellationToken cancellationToken);
    protected abstract Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsResponseCoreAsync(string prompt, CancellationToken cancellationToken);
    protected abstract Task<bool> IsGenerateDocumentationCommentAvailableCoreAsync(CancellationToken cancellationToken);
    protected abstract Task<bool> IsFileExcludedFromDocumentationCommentGenerationCoreAsync(string filePath, CancellationToken cancellationToken);
    protected abstract Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> GetDocumentationCommentCoreAsync(DocumentationCommentProposal proposal, CancellationToken cancellationToken);
    protected abstract Task<ImmutableDictionary<SyntaxNode, ImplementationDetails>> ImplementNotImplementedExceptionsCoreAsync(Document document, ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties, CancellationToken cancellationToken);
    protected abstract Task<bool> IsImplementNotImplementedExceptionsAvailableCoreAsync(CancellationToken cancellationToken);

    public Task<bool> IsOnTheFlyDocsAvailableAsync(CancellationToken cancellationToken)
        => IsOnTheFlyDocsAvailableCoreAsync(cancellationToken);

    public Task<bool> IsFileExcludedFromOnTheFlyDocsAsync(string filePath, CancellationToken cancellationToken)
        => IsFileExcludedFromOnTheFlyDocsCoreAsync(filePath, cancellationToken);

    public async Task<string> GetOnTheFlyDocsPromptAsync(OnTheFlyDocsInfo onTheFlyDocsInfo, CancellationToken cancellationToken)
    {
        return await GetOnTheFlyDocsPromptCoreAsync(onTheFlyDocsInfo, cancellationToken).ConfigureAwait(false);
    }
    public async Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsResponseAsync(string prompt, CancellationToken cancellationToken)
    {
        return await GetOnTheFlyDocsResponseCoreAsync(prompt, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> IsGenerateDocumentationCommentAvailableAsync(CancellationToken cancellationToken)
        => IsGenerateDocumentationCommentAvailableCoreAsync(cancellationToken);

    public Task<bool> IsFileExcludedFromDocumentationCommentGenerationAsync(string filePath, CancellationToken cancellationToken)
        => IsFileExcludedFromDocumentationCommentGenerationCoreAsync(filePath, cancellationToken);

    public async Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> GetDocumentationCommentAsync(DocumentationCommentProposal proposal, CancellationToken cancellationToken)
    {
        return await GetDocumentationCommentCoreAsync(proposal, cancellationToken).ConfigureAwait(false);
    }

    public Task<bool> IsImplementNotImplementedExceptionsAvailableAsync(CancellationToken cancellationToken)
        => IsImplementNotImplementedExceptionsAvailableCoreAsync(cancellationToken);

    public async Task<ImmutableDictionary<SyntaxNode, ImplementationDetails>> ImplementNotImplementedExceptionsAsync(
        Document document,
        ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties,
        CancellationToken cancellationToken)
    {
        return await ImplementNotImplementedExceptionsCoreAsync(document, methodOrProperties, cancellationToken).ConfigureAwait(false);
    }
}
