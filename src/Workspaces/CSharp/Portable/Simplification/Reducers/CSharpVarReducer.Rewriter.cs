// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal partial class CSharpVarReducer
{
    private class Rewriter : AbstractReductionRewriter
    {
        public Rewriter(ObjectPool<IReductionRewriter> pool)
            : base(pool)
        {
        }

        private SyntaxNode ProcessTypeSyntax(TypeSyntax typeSyntax)
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            // Only simplify if us, or a parent, was marked as needing simplification.
            if (!alwaysSimplify && !typeSyntax.HasAnnotation(Simplifier.Annotation))
            {
                return typeSyntax;
            }

            // Definitely do not simplify us if we were told to not simplify.
            if (typeSyntax.HasAnnotation(SimplificationHelpers.DoNotSimplifyAnnotation))
            {
                return typeSyntax;
            }

            var typeStyle = CSharpUseImplicitTypeHelper.Instance.AnalyzeTypeName(
                typeSyntax, this.SemanticModel, this.Options, this.CancellationToken);

            if (!typeStyle.IsStylePreferred || !typeStyle.CanConvert())
            {
                return typeSyntax;
            }

            return SyntaxFactory.IdentifierName("var")
                .WithLeadingTrivia(typeSyntax.GetLeadingTrivia())
                .WithTrailingTrivia(typeSyntax.GetTrailingTrivia());
        }

        public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node) => ProcessTypeSyntax(node);
        public override SyntaxNode VisitArrayType(ArrayTypeSyntax node) => ProcessTypeSyntax(node);
        public override SyntaxNode VisitGenericName(GenericNameSyntax node) => ProcessTypeSyntax(node);
        public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node) => ProcessTypeSyntax(node);
        public override SyntaxNode VisitNullableType(NullableTypeSyntax node) => ProcessTypeSyntax(node);
        public override SyntaxNode VisitPointerType(PointerTypeSyntax node) => ProcessTypeSyntax(node);
        public override SyntaxNode VisitPredefinedType(PredefinedTypeSyntax node) => ProcessTypeSyntax(node);
        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node) => ProcessTypeSyntax(node);
        public override SyntaxNode VisitTupleType(TupleTypeSyntax node) => ProcessTypeSyntax(node);
    }
}
