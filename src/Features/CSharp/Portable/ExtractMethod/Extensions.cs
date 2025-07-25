// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal static class Extensions
{
    extension(SyntaxNode node)
    {
        public StatementSyntax? GetStatementUnderContainer()
        {
            Contract.ThrowIfNull(node);

            for (var current = node; current is object; current = current.Parent)
            {
                if (current.Parent != null &&
                    current.Parent.IsStatementContainerNode())
                {
                    return current as StatementSyntax;
                }
            }

            return null;
        }

        public StatementSyntax GetParentLabeledStatementIfPossible()
            => (StatementSyntax)((node.Parent is LabeledStatementSyntax) ? node.Parent : node);

        public bool UnderValidContext()
        {
            Contract.ThrowIfNull(node);

            if (!node.GetAncestorsOrThis<SyntaxNode>().Any(predicate))
            {
                return false;
            }

            return true;

            bool predicate(SyntaxNode n)
            {
                if (n is BaseMethodDeclarationSyntax or
                    AccessorDeclarationSyntax or
                    BlockSyntax or
                    GlobalStatementSyntax or
                    CompilationUnitSyntax)
                {
                    return true;
                }

                if (n is ConstructorInitializerSyntax constructorInitializer)
                {
                    return constructorInitializer.ContainsInArgument(node.Span);
                }

                return false;
            }
        }

        public bool ContainedInValidType()
        {
            Contract.ThrowIfNull(node);
            foreach (var ancestor in node.AncestorsAndSelf())
            {
                if (ancestor is TypeDeclarationSyntax)
                {
                    return true;
                }

                if (ancestor is NamespaceDeclarationSyntax)
                {
                    return false;
                }
            }

            return true;
        }

        public bool PartOfConstantInitializerExpression()
        {
            return node.PartOfConstantInitializerExpression<FieldDeclarationSyntax>(n => n.Modifiers) ||
                   node.PartOfConstantInitializerExpression<LocalDeclarationStatementSyntax>(n => n.Modifiers);
        }

        private bool PartOfConstantInitializerExpression<T>(Func<T, SyntaxTokenList> modifiersGetter) where T : SyntaxNode
        {
            var decl = node.GetAncestor<T>();
            if (decl == null)
            {
                return false;
            }

            if (!modifiersGetter(decl).Any(SyntaxKind.ConstKeyword))
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
    }

    extension([NotNullWhen(true)] SyntaxNode? node)
    {
        public bool IsStatementContainerNode()
        => node is BlockSyntax or SwitchSectionSyntax or GlobalStatementSyntax;

        public bool IsArrayInitializer()
            => node is InitializerExpressionSyntax && node.Parent is EqualsValueClauseSyntax;

        public bool IsExpressionInCast()
            => node is ExpressionSyntax && node.Parent is CastExpressionSyntax;
    }

    extension(SyntaxNode? node)
    {
        public BlockSyntax? GetBlockBody()
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
    }

    extension(ConstructorInitializerSyntax initializer)
    {
        public bool ContainsInArgument(TextSpan textSpan)
        {
            if (initializer == null)
            {
                return false;
            }

            return initializer.ArgumentList.Arguments.Any(a => a.Span.Contains(textSpan));
        }
    }

    extension(IEnumerable<SyntaxToken> tokens)
    {
        public bool ContainArgumentlessThrowWithoutEnclosingCatch(TextSpan textSpan)
        {
            foreach (var token in tokens)
            {
                if (token.Kind() != SyntaxKind.ThrowKeyword)
                {
                    continue;
                }

                if (token.Parent is not ThrowStatementSyntax throwStatement || throwStatement.Expression != null)
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

        public bool ContainPreprocessorCrossOver(TextSpan textSpan)
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

        public IEnumerable<SyntaxTrivia> GetAllTrivia()
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
    }

    extension(HashSet<SyntaxAnnotation> set)
    {
        public bool HasSyntaxAnnotation(SyntaxNode node)
        => set.Any(a => node.GetAnnotatedNodesAndTokens(a).Any());
    }

    extension(SyntaxToken token1)
    {
        public bool HasHybridTriviaBetween(SyntaxToken token2)
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

        public bool BetweenFieldAndNonFieldMember(SyntaxToken token2)
        {
            if (token1.RawKind != (int)SyntaxKind.SemicolonToken || !(token1.Parent is FieldDeclarationSyntax))
            {
                return false;
            }

            var field = token2.GetAncestor<FieldDeclarationSyntax>();
            return field == null;
        }
    }

    extension(ITypeSymbol? type)
    {
        public bool IsObjectType()
        => type == null || type.SpecialType == SpecialType.System_Object;
    }
}
