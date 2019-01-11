// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private sealed class Analyzer : CSharpSyntaxVisitor<AnalyzedNode>
        {
            private static readonly Analyzer s_instance = new Analyzer();

            private Analyzer() { }

            public static AnalyzedNode Analyze(BinaryExpressionSyntax node)
            {
                return s_instance.VisitBinaryExpression(node);
            }

            public override AnalyzedNode VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                var left = node.Left;
                var right = node.Right;

                switch (node.Kind())
                {
                    case SyntaxKind.EqualsExpression when IsIdentifierOrSimpleMemberAccses(left):
                        return new PatternMatch(left, new ConstantPattern(right));

                    case SyntaxKind.IsExpression:
                        return new PatternMatch(left, new TypePattern((TypeSyntax)right));

                    case SyntaxKind.NotEqualsExpression when right.IsKind(SyntaxKind.NullLiteralExpression):
                        return new PatternMatch(left, new NotNullPattern());

                    case SyntaxKind.LogicalAndExpression
                        when Visit(left) is AnalyzedNode analyzedLeft &&
                            Visit(right) is AnalyzedNode analyzedRight:
                        return new Conjuction(analyzedLeft, analyzedRight);
                }

                return null;
            }

            private static bool IsIdentifierOrSimpleMemberAccses(ExpressionSyntax node)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.IdentifierName:
                        return true;

                    case SyntaxKind.SimpleMemberAccessExpression:
                        var memberAccess = (MemberAccessExpressionSyntax)node;
                        if (!memberAccess.Name.IsKind(SyntaxKind.IdentifierName))
                        {
                            break;
                        }

                        return IsIdentifierOrSimpleMemberAccses(memberAccess.Expression);
                }

                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitIsPatternExpression(IsPatternExpressionSyntax node)
            {
                return new PatternMatch(node.Expression, new SourcePattern(node.Pattern));
            }
        }
    }
}
