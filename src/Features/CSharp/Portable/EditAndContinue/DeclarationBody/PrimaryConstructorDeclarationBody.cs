// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue;

/// <summary>
/// Implicit initializer: class [|C(int a, int b)|] : B;
/// Explicit initializer: class C(int a, int b) : [|B(expr)|];
/// </summary>
internal abstract class PrimaryConstructorDeclarationBody(TypeDeclarationSyntax typeDeclaration)
    : InstanceConstructorDeclarationBody
{
    public TypeDeclarationSyntax TypeDeclaration
        => typeDeclaration;

    public sealed override SyntaxNode? ExplicitBody
        => null;

    public sealed override OneOrMany<SyntaxNode> RootNodes
        => OneOrMany.Create(InitializerActiveStatement);

    public sealed override SyntaxNode? ParameterClosure
        => typeDeclaration;

    public sealed override TextSpan Envelope
        => InitializerActiveStatementSpan;
}
