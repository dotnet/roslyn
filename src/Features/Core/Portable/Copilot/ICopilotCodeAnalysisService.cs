// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
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
    /// Method to fetch the on-the-fly documentation based on a a symbols <paramref name="symbolSignature"/>
    /// and the code for the symbols in <paramref name="declarationCode"/>.
    /// <para>
    /// <paramref name="symbolSignature"/> is a formatted string representation of an <see cref="ISymbol"/>.<br/>
    /// <paramref name="declarationCode"/> is a list of a code definitions from an <see cref="ISymbol"/>.
    /// <paramref name="language"/> is the language of the originating <see cref="ISymbol"/>.
    /// </para>
    /// </summary>
    Task<string> GetOnTheFlyDocsAsync(string symbolSignature, ImmutableArray<string> declarationCode, string language, CancellationToken cancellationToken);

    /// <summary>
    /// Determines if there are any exclusions in the workspace.
    /// </summary>
    Task<bool> IsAnyExclusionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Determines if the given <paramref name="filePath"/> is excluded in the workspace.
    /// </summary>
    Task<bool> IsFileExcludedAsync(string filePath, CancellationToken cancellationToken);

}
