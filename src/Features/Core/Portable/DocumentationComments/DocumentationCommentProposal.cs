// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.DocumentationComments;

/// <summary>
/// Represents the set of all edits that will be needed to fill in the documentation comment for a symbol.
/// </summary>
internal sealed record DocumentationCommentProposal
{
    public string SymbolToAnalyze { get; }

    public ImmutableArray<DocumentationCommentProposedEdit> ProposedEdits { get; }

    public DocumentationCommentProposal(string symbolToAnalyze, ImmutableArray<DocumentationCommentProposedEdit> proposedEdits)
    {
        SymbolToAnalyze = symbolToAnalyze;
        ProposedEdits = proposedEdits;
    }
}
