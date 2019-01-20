// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static SyntaxFactory;

    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private sealed class Rewriter : Visitor<SyntaxNode>
        {
            private bool _isPattern = false;

            private Rewriter() { }

            public static ExpressionSyntax Rewrite(AnalyzedNode analyzedNode)
            {
                return (ExpressionSyntax)new Rewriter().Visit(analyzedNode);
            }

            public override SyntaxNode VisitPatternMatch(PatternMatch node)
            {
                if (_isPattern)
                {
                    return Subpattern(NameColon((IdentifierNameSyntax)node.Expression), AsPattern(Visit(node.Pattern)));
                }
                else
                {
                    _isPattern = true;
                    var result = IsPatternExpression(node.Expression, AsPattern(Visit(node.Pattern)));
                    _isPattern = false;
                    return result;
                }
            }

            private static PatternSyntax AsPattern(SyntaxNode node)
            {
                switch (node)
                {
                    case PatternSyntax n:
                        return n;
                    case SubpatternSyntax n:
                        return RecursivePattern(null, null, PropertyPatternClause(SingletonSeparatedList(n)), null);
                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value.Kind());
                }
            }

            private void VisitPatternConjuction(Conjuction node, ArrayBuilder<SubpatternSyntax> nodes, ref TypeSyntax type, ref SyntaxToken identifier)
            {
                VisitPatternConjuctionOperand(node.Left, nodes, ref type, ref identifier);
                VisitPatternConjuctionOperand(node.Right, nodes, ref type, ref identifier);
            }

            private void VisitPatternConjuctionOperand(AnalyzedNode node, ArrayBuilder<SubpatternSyntax> nodes, ref TypeSyntax type, ref SyntaxToken identifier)
            {
                switch (node)
                {
                    case Conjuction n:
                        VisitPatternConjuction(n, nodes, ref type, ref identifier);
                        break;
                    case TypePattern n:
                        type = n.Type;
                        break;
                    case VarPattern n:
                        identifier = n.Identifier;
                        break;
                    default:
                        nodes.Add((SubpatternSyntax)Visit(node));
                        break;
                }
            }

            public override SyntaxNode VisitConjuction(Conjuction node)
            {
                if (_isPattern)
                {
                    var nodes = ArrayBuilder<SubpatternSyntax>.GetInstance();
                    TypeSyntax type = null;
                    SyntaxToken identifier = default;
                    VisitPatternConjuction(node, nodes, ref type, ref identifier);

                    return RecursivePattern(
                        type,
                        deconstructionPatternClause: null,
                        PropertyPatternClause(SeparatedList(nodes.ToArrayAndFree())),
                        identifier.IsKind(SyntaxKind.None) ? null : SingleVariableDesignation(identifier));
                }
                else
                {
                    return BinaryExpression(SyntaxKind.LogicalAndExpression,
                        (ExpressionSyntax)Visit(node.Left),
                        (ExpressionSyntax)Visit(node.Right));
                }
            }

            public override SyntaxNode VisitConstantPattern(ConstantPattern node)
            {
                return ConstantPattern(node.Expression);
            }

            public override SyntaxNode VisitTypePattern(TypePattern node)
            {
                return DeclarationPattern(node.Type, DiscardDesignation());
            }

            public override SyntaxNode VisitNotNullPattern(NotNullPattern node)
            {
                return RecursivePattern(null, null, PropertyPatternClause(SeparatedList<SubpatternSyntax>()), null);
            }

            public override SyntaxNode VisitVarPattern(VarPattern node)
            {
                throw new NotImplementedException();
            }
        }
    }
}
