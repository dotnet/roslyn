// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

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
    public bool IsCopilotAvailable { get; }

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

}
