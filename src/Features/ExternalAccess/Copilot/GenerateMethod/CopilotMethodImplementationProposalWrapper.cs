// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MethodImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal sealed class CopilotMethodImplementationProposalWrapper
    {
        private readonly MethodImplementationProposal _methodImplementationProposal;
        private readonly ImmutableArray<CopilotMethodImplementationProposedEditWrapper> _wrappedProposedEdits;

        public CopilotMethodImplementationProposalWrapper(MethodImplementationProposal methodImplementationProposal)
        {
            _methodImplementationProposal = methodImplementationProposal;
            _wrappedProposedEdits = _methodImplementationProposal.ProposedEdits.SelectAsArray(e => new CopilotMethodImplementationProposedEditWrapper(e));
        }

        public string SymbolToAnalyze => _methodImplementationProposal.SymbolToAnalyze;
        public ImmutableArray<CopilotMethodImplementationProposedEditWrapper> ProposedEdits => _wrappedProposedEdits;
    }
}
