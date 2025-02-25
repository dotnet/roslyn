// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.MethodImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal sealed class MethodImplementationProposalWrapper
    {
        private readonly MethodImplementationProposal _methodImplementationProposal;

        public MethodImplementationProposalWrapper(MethodImplementationProposal methodImplementationProposal)
        {
            _methodImplementationProposal = methodImplementationProposal;
        }

        public string MethodName => _methodImplementationProposal.MethodName;
        public string MethodBody => _methodImplementationProposal.MethodBody;
        public string ReturnType => _methodImplementationProposal.ReturnType;
        public string ContainingType => _methodImplementationProposal.ContainingType;
        public string Accessibility => _methodImplementationProposal.Accessibility;
        public ImmutableArray<string> Modifiers => _methodImplementationProposal.Modifiers;
        public int ReferenceCount => _methodImplementationProposal.ReferenceCount;
        public string PreviousTokenText => _methodImplementationProposal.PreviousTokenText;
        public string NextTokenText => _methodImplementationProposal.NextTokenText;
        public string LanguageVersion => _methodImplementationProposal.LanguageVersion;
        public ImmutableArray<MethodImplementationParameterContextWrapper> Parameters
            => _methodImplementationProposal.Parameters.Select(p => new MethodImplementationParameterContextWrapper(p)).ToImmutableArray();
        public ImmutableArray<MethodImplementationReferenceContextWrapper> ReferenceContexts
            => _methodImplementationProposal.TopReferences.Select(r => new MethodImplementationReferenceContextWrapper(r)).ToImmutableArray();
    }
}
