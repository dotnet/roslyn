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

namespace Microsoft.CodeAnalysis.Copilot;

/// <summary>
/// Entry point for Copilot features.
/// </summary>
internal interface ICopilotCodeAnalysisService : ILanguageService
{
    /// <summary>
    /// Returns true if on-the-fly documentation is available.
    /// </summary>
    Task<bool> IsOnTheFlyDocsAvailableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns true if the given file is excluded from on-the-fly documentation.
    /// </summary>
    Task<bool> IsFileExcludedFromOnTheFlyDocsAsync(string filePath, CancellationToken cancellationToken);

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
    /// Returns true if documentation-comment generation is available.
    /// </summary>
    Task<bool> IsGenerateDocumentationCommentAvailableAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns true if the given file is excluded from documentation-comment generation.
    /// </summary>
    Task<bool> IsFileExcludedFromDocumentationCommentGenerationAsync(string filePath, CancellationToken cancellationToken);

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
