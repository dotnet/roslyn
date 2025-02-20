// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MethodImplementation;

/// <summary>
/// Represents the set of all edits that will be needed to fill in the method implementation for a symbol.
/// </summary>
internal sealed record MethodImplementationProposal
{
    public string SymbolToAnalyze { get; }

    public ImmutableArray<MethodImplementationProposedEdit> ProposedEdits { get; }

    public MethodImplementationProposal(string symbolToAnalyze, ImmutableArray<MethodImplementationProposedEdit> proposedEdits)
    {
        SymbolToAnalyze = symbolToAnalyze;
        ProposedEdits = proposedEdits;
    }
}
