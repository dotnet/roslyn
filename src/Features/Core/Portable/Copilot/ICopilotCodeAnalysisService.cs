// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

/// <summary>
/// Service to compute and cache Copilot code analysis suggestions, and also acts as an
/// entry point for other Copilot code analysis features.
/// </summary>
internal interface ICopilotCodeAnalysisService : ILanguageService
{
    /// <summary>
    /// Returns true if the Copilot service is available for making Copilot code analysis requests.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns one or more prompt titles for Copilot code analysis.
    /// First prompt title is for built-in Copilot code analysis prompt.
    /// Copilot analyzer may support additional prompts for different kinds of code analysis.
    /// </summary>
    /// <remarks>
    /// A prompt's title serves as the ID of the prompt, which can be used to selectively trigger analysis and retrive cached results.
    /// </remarks>
    Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken);

    /// <summary>
    /// Method to trigger Copilot code analysis on the given <paramref name="document"/>,
    /// which may be scoped to a specific <paramref name="span"/> within the document.
    /// <paramref name="promptTitle"/> indicates the kind of Copilot analysis to execute.
    /// </summary>
    /// <remarks>
    /// A prompt's title serves as the ID of the prompt, which can be used to selectively trigger analysis and retrive cached results.
    /// </remarks>
    Task AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken);

    /// <summary>
    /// Method to fetch already computed and cached Copilot code analysis diagnostics for the
    /// given <paramref name="document"/> and <paramref name="promptTitles"/>.
    /// This method does not invoke any Copilot code analysis requests, and hence is
    /// relatively cheap.
    /// </summary>
    /// <remarks>
    /// A prompt's title serves as the ID of the prompt, which can be used to selectively trigger analysis and retrive cached results.
    /// </remarks>
    Task<ImmutableArray<Diagnostic>> GetCachedDocumentDiagnosticsAsync(Document document, TextSpan? span, ImmutableArray<string> promptTitles, CancellationToken cancellationToken);

    /// <summary>
    /// Method to start a Copilot refinement session on top of the changes between the given
    /// <paramref name="oldDocument"/> and <paramref name="newDocument"/>.
    /// <paramref name="primaryDiagnostic"/> represents an optional diagnostic where the change is originated,
    /// which might be used to provide additional context to Copilot for the refinement session.
    /// </summary>
    Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the prompt 
    /// </summary>
    /// <param name="onTheFlyDocsInfo">Type containing code and other context about the symbol being examined.</param>
    /// <returns></returns>
    Task<string> GetOnTheFlyDocsPromptAsync(OnTheFlyDocsInfo onTheFlyDocsInfo, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the response from Copilot summarizing what a symbol is being used for and whether or not the quota has exceeded.
    /// </summary>
    /// <param name="prompt">The input text used to generate the response.</param>
    Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsResponseAsync(string prompt, CancellationToken cancellationToken);

    /// <summary>
    /// Determines if the given <paramref name="filePath"/> is excluded in the workspace.
    /// </summary>
    Task<bool> IsFileExcludedAsync(string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// Method to retrieve the documentation comment for a given <paramref name="proposal"/>
    /// </summary>
    /// <param name="proposal">The documentation comment that has been broken down into its individual pieces.</param>
    Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> GetDocumentationCommentAsync(DocumentationCommentProposal proposal, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if the feature for implementing <see cref="System.NotImplementedException"/> is available.
    /// </summary>
    Task<bool> IsImplementNotImplementedExceptionsAvailableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Implements methods or properties containing <see cref="System.NotImplementedException"/> throws in the given <paramref name="document"/>.
    /// </summary>
    /// <returns>A dictionary mapping the original syntax nodes to their implementation details.</returns>
    Task<ImmutableDictionary<SyntaxNode, ImplementationDetails>> ImplementNotImplementedExceptionsAsync(
        Document document,
        ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties,
        CancellationToken cancellationToken);
}
