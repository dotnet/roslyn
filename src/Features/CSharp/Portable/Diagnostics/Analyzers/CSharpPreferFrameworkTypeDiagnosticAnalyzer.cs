// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PreferFrameworkType;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class CSharpPreferFrameworkTypeDiagnosticAnalyzer :
    PreferFrameworkTypeDiagnosticAnalyzerBase<
        SyntaxKind,
        ExpressionSyntax,
        TypeSyntax,
        IdentifierNameSyntax,
        PredefinedTypeSyntax>
{
    protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get; } =
        [SyntaxKind.PredefinedType, SyntaxKind.IdentifierName];

    ///<remarks>
    /// every predefined type keyword except <c>void</c> can be replaced by its framework type in code.
    ///</remarks>
    protected override bool IsPredefinedTypeReplaceableWithFrameworkType(PredefinedTypeSyntax node)
        => node.Keyword.Kind() != SyntaxKind.VoidKeyword;

    // Only offer to change nint->System.IntPtr when it would preserve semantics exactly.
    protected override bool IsIdentifierNameReplaceableWithFrameworkType(SemanticModel semanticModel, IdentifierNameSyntax node)
        => (node.IsNint || node.IsNuint) &&
           semanticModel.SyntaxTree.Options.LanguageVersion() >= LanguageVersion.CSharp9 &&
           semanticModel.Compilation.SupportsRuntimeCapability(RuntimeCapability.NumericIntPtr);

    protected override bool IsInMemberAccessOrCrefReferenceContext(ExpressionSyntax node)
        => node.IsDirectChildOfMemberAccessExpression() || node.InsideCrefReference();
}
