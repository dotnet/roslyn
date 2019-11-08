// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal static class Extensions
    {
        public static ExpressionSyntax? GetUnparenthesizedExpression(this SyntaxNode? node)
        {
            if (!(node is ParenthesizedExpressionSyntax parenthesizedExpression))
            {
                return node as ExpressionSyntax;
            }

            return GetUnparenthesizedExpression(parenthesizedExpression.Expression);
        }

        public static StatementSyntax? GetStatementUnderContainer(this SyntaxNode node)
        {
            Contract.ThrowIfNull(node);

            for (SyntaxNode? current = node; current is object; current = current.Parent)
            {
                if (current.Parent != null &&
                    current.Parent.IsStatementContainerNode())
                {
                    return current as StatementSyntax;
                }
            }

            return null;
        }

        public static StatementSyntax GetParentLabeledStatementIfPossible(this SyntaxNode node)
        {
            return (StatementSyntax)((node.Parent is LabeledStatementSyntax) ? node.Parent : node);
        }

        public static bool IsStatementContainerNode([NotNullWhen(returnValue: true)] this SyntaxNode? node)
        {
            return node is BlockSyntax || node is SwitchSectionSyntax;
        }

        public static BlockSyntax? GetBlockBody(this SyntaxNode? node)
        {
            switch (node)
            {
                case BaseMethodDeclarationSyntax m: return m.Body;
                case AccessorDeclarationSyntax a: return a.Body;
                case SimpleLambdaExpressionSyntax s: return s.Body as BlockSyntax;
                case ParenthesizedLambdaExpressionSyntax p: return p.Body as BlockSyntax;
                case AnonymousMethodExpressionSyntax a: return a.Block;
                default: return null;
            }
        }

        public static bool UnderValidContext(this SyntaxNode node)
        {
            Contract.ThrowIfNull(node);

            bool predicate(SyntaxNode n)
            {
                if (n is BaseMethodDeclarationSyntax ||
                    n is AccessorDeclarationSyntax ||
                    n is BlockSyntax ||
                    n is GlobalStatementSyntax)
                {
                    return true;
                }

                if (n is ConstructorInitializerSyntax constructorInitializer)
                {
                    return constructorInitializer.ContainsInArgument(node.Span);
                }

                return false;
            }

            if (!node.GetAncestorsOrThis<SyntaxNode>().Any(predicate))
            {
                return false;
            }

            if (node.FromScript() || node.GetAncestor<TypeDeclarationSyntax>() != null)
            {
                return true;
            }

            return false;
        }

        public static bool UnderValidContext(this SyntaxToken token)
        {
            return token.GetAncestors<SyntaxNode>().Any(n => n.CheckTopLevel(token.Span));
        }

        public static bool PartOfConstantInitializerExpression(this SyntaxNode node)
        {
            return node.PartOfConstantInitializerExpression<FieldDeclarationSyntax>(n => n.Modifiers) ||
                   node.PartOfConstantInitializerExpression<LocalDeclarationStatementSyntax>(n => n.Modifiers);
        }

        private static bool PartOfConstantInitializerExpression<T>(this SyntaxNode node, Func<T, SyntaxTokenList> modifiersGetter) where T : SyntaxNode
        {
            var decl = node.GetAncestor<T>();
            if (decl == null)
            {
                return false;
            }

            if (!modifiersGetter(decl).Any(t => t.Kind() == SyntaxKind.ConstKeyword))
            {
                return false;
            }

            // we are under decl with const modifier, check we are part of initializer expression
            var equal = node.GetAncestor<EqualsValueClauseSyntax>();
            if (equal == null)
            {
                return false;
            }

            return equal.Value != null && equal.Value.Span.Contains(node.Span);
        }

        public static bool ContainArgumentlessThrowWithoutEnclosingCatch(this IEnumerable<SyntaxToken> tokens, TextSpan textSpan)
        {
            foreach (var token in tokens)
            {
                if (token.Kind() != SyntaxKind.ThrowKeyword)
                {
                    continue;
                }

                if (!(token.Parent is ThrowStatementSyntax throwStatement) || throwStatement.Expression != null)
                {
                    continue;
                }

                var catchClause = token.GetAncestor<CatchClauseSyntax>();
                if (catchClause == null || !textSpan.Contains(catchClause.Span))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainPreprocessorCrossOver(this IEnumerable<SyntaxToken> tokens, TextSpan textSpan)
        {
            var activeRegions = 0;
            var activeIfs = 0;

            foreach (var trivia in tokens.GetAllTrivia())
            {
                if (!textSpan.Contains(trivia.Span))
                {
                    continue;
                }

                switch (trivia.Kind())
                {
                    case SyntaxKind.RegionDirectiveTrivia:
                        activeRegions++;
                        break;
                    case SyntaxKind.EndRegionDirectiveTrivia:
                        if (activeRegions <= 0)
                        {
                            return true;
                        }

                        activeRegions--;
                        break;
                    case SyntaxKind.IfDirectiveTrivia:
                        activeIfs++;
                        break;
                    case SyntaxKind.EndIfDirectiveTrivia:
                        if (activeIfs <= 0)
                        {
                            return true;
                        }

                        activeIfs--;
                        break;
                    case SyntaxKind.ElseDirectiveTrivia:
                    case SyntaxKind.ElifDirectiveTrivia:
                        if (activeIfs <= 0)
                        {
                            return true;
                        }

                        break;
                }
            }

            return activeIfs != 0 || activeRegions != 0;
        }

        public static IEnumerable<SyntaxTrivia> GetAllTrivia(this IEnumerable<SyntaxToken> tokens)
        {
            foreach (var token in tokens)
            {
                foreach (var trivia in token.LeadingTrivia)
                {
                    yield return trivia;
                }

                foreach (var trivia in token.TrailingTrivia)
                {
                    yield return trivia;
                }
            }
        }

        public static bool HasSyntaxAnnotation(this HashSet<SyntaxAnnotation> set, SyntaxNode node)
        {
            return set.Any(a => node.GetAnnotatedNodesAndTokens(a).Any());
        }

        public static bool HasHybridTriviaBetween(this SyntaxToken token1, SyntaxToken token2)
        {
            if (token1.TrailingTrivia.Any(t => !t.IsElastic()))
            {
                return true;
            }

            if (token2.LeadingTrivia.Any(t => !t.IsElastic()))
            {
                return true;
            }

            return false;
        }

        public static bool IsArrayInitializer([NotNullWhen(returnValue: true)] this SyntaxNode? node)
        {
            return node is InitializerExpressionSyntax { Parent: EqualsValueClauseSyntax _ };
        }

        public static bool IsExpressionInCast([NotNullWhen(returnValue: true)] this SyntaxNode? node)
        {
            return node is ExpressionSyntax && node.Parent is CastExpressionSyntax;
        }

        public static bool IsExpression([NotNullWhen(returnValue: true)] this SyntaxNode? node)
        {
            return node is ExpressionSyntax;
        }

        public static bool IsObjectType(this ITypeSymbol? type)
        {
            return type == null || type.SpecialType == SpecialType.System_Object;
        }

        public static bool BetweenFieldAndNonFieldMember(this SyntaxToken token1, SyntaxToken token2)
        {
            if (token1.RawKind != (int)SyntaxKind.SemicolonToken || !(token1.Parent is FieldDeclarationSyntax))
            {
                return false;
            }

            var field = token2.GetAncestor<FieldDeclarationSyntax>();
            return field == null;
        }
    }
}
