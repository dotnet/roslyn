// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.MethodImplementation;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot
{
    internal sealed class MethodProposalWrapper(MethodImplementationProposal proposal)
    {
        public string MethodName => proposal.MethodName;
        public string? MethodBody => proposal.MethodBody;
        public string? ExpressionBody => proposal.ExpressionBody;
        public string ReturnType => proposal.ReturnType;
        public string ContainingType => proposal.ContainingType;
        public string Accessibility => proposal.Accessibility;
        public ImmutableArray<string> Modifiers => proposal.Modifiers;
        public int ReferenceCount => proposal.ReferenceCount;
        public string PreviousTokenText => proposal.PreviousTokenText;
        public string NextTokenText => proposal.NextTokenText;
        public string LanguageVersion => proposal.LanguageVersion;
        public bool IsExpressionBody => proposal.IsExpressionBody;
        public ImmutableArray<ParameterContextWrapper> Parameters => proposal.Parameters.SelectAsArray(p => new ParameterContextWrapper(p));
        public ImmutableArray<ReferenceContextWrapper> ReferenceContexts => proposal.TopReferences.SelectAsArray(r => new ReferenceContextWrapper(r));
    }
}
