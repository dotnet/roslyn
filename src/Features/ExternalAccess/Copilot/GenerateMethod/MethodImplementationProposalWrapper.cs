// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MethodImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal sealed class MethodImplementationProposalWrapper
    {
        private readonly MethodImplementationProposal _methodImplementationProposal;
        private readonly ImmutableArray<MethodImplementationParameterContextWrapper> _wrappedParameters;
        private readonly ImmutableArray<MethodImplementationReferenceContextWrapper> _wrappedReferenceContexts;

        public MethodImplementationProposalWrapper(MethodImplementationProposal methodImplementationProposal)
        {
            _methodImplementationProposal = methodImplementationProposal;
            _wrappedParameters = _methodImplementationProposal.Parameters.SelectAsArray(p => new MethodImplementationParameterContextWrapper(p));
            _wrappedReferenceContexts = _methodImplementationProposal.TopReferences.SelectAsArray(r => new MethodImplementationReferenceContextWrapper(r));
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
        public ImmutableArray<MethodImplementationParameterContextWrapper> Parameters => _wrappedParameters;
        public ImmutableArray<MethodImplementationReferenceContextWrapper> ReferenceContexts => _wrappedReferenceContexts;
    }
}
