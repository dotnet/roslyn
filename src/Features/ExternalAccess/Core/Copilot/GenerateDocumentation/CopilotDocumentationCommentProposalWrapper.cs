// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot;

internal sealed class CopilotDocumentationCommentProposalWrapper
{
    private readonly DocumentationCommentProposal _documentationCommentProposal;
    private readonly ImmutableArray<CopilotDocumentationCommentProposedEditWrapper> _wrappedProposedEdits;

    public CopilotDocumentationCommentProposalWrapper(DocumentationCommentProposal documentationCommentProposal)
    {
        _documentationCommentProposal = documentationCommentProposal;
        _wrappedProposedEdits = _documentationCommentProposal.ProposedEdits.SelectAsArray(e => new CopilotDocumentationCommentProposedEditWrapper(e));

    }

    public string SymbolToAnalyze => _documentationCommentProposal.SymbolToAnalyze;
    public ImmutableArray<CopilotDocumentationCommentProposedEditWrapper> ProposedEdits => _wrappedProposedEdits;
}
