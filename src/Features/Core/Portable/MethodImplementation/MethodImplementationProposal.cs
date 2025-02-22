// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.MethodImplementation;

internal sealed record MethodImplementationProposal
{
    public string MethodName { get; }
    public string ReturnType { get; }
    public string ContainingType { get; }
    public string Accessibility { get; }
    public ImmutableArray<string> Modifiers { get; }
    public ImmutableArray<MethodImplementationParameterInfo> Parameters { get; }
    public string PreviousTokenText { get; }
    public string NextTokenText { get; }
    public ImmutableArray<MethodImplementationProposedEdit> ProposedEdits { get; }

    public MethodImplementationProposal(
        string methodName,
        string returnType,
        string containingType,
        string accessibility,
        ImmutableArray<string> modifiers,
        ImmutableArray<MethodImplementationParameterInfo> parameters,
        string previousTokenText,
        string nextTokenText,
        ImmutableArray<MethodImplementationProposedEdit> proposedEdits
        )
    {
        MethodName = methodName;
        ReturnType = returnType;
        ContainingType = containingType;
        Accessibility = accessibility;
        Modifiers = modifiers;
        Parameters = parameters;
        PreviousTokenText = previousTokenText;
        NextTokenText = nextTokenText;
        ProposedEdits = proposedEdits;
    }
}
