// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

internal abstract class OrdinaryInstanceConstructorDeclarationBody(ConstructorDeclarationSyntax constructor)
    : InstanceConstructorDeclarationBody
{
    public ConstructorDeclarationSyntax Constructor
        => constructor;

    public SyntaxNode Body
        => (SyntaxNode?)constructor.Body ?? constructor.ExpressionBody?.Expression!;

    public sealed override SyntaxNode? ExplicitBody
        => Body;

    public sealed override SyntaxNode EncompassingAncestor
        => constructor;

    public sealed override SyntaxNode? MatchRoot
        => constructor;

    public sealed override SyntaxNode? ParameterClosure
        => constructor;

    public override OneOrMany<SyntaxNode> RootNodes
        => OneOrMany.Create<SyntaxNode>(constructor);
}
