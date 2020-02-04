// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxNodeExtensions
    {
        public static bool IsKind<TNode>([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind, [NotNullWhen(returnValue: true)] out TNode? result)
            where TNode : SyntaxNode
        {
            if (node.IsKind(kind))
            {
#if !CODE_STYLE
                result = (TNode)node;
#else
                // The CodeStyle layer is referencing an older, unannotated version of Roslyn which doesn't know that IsKind guarantees the non-nullness
                // of node. So we have to silence it here.
                result = (TNode)node!;
#endif
                return true;
            }

            result = null;
            return false;
        }

        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind)
            => CodeAnalysis.CSharpExtensions.IsKind(node?.Parent, kind);

        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2)
            => IsKind(node?.Parent, kind1, kind2);

        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
            => IsKind(node?.Parent, kind1, kind2, kind3);

        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4)
            => IsKind(node?.Parent, kind1, kind2, kind3, kind4);

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5, SyntaxKind kind6)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5 || csharpKind == kind6;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5, SyntaxKind kind6, SyntaxKind kind7)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5 || csharpKind == kind6 || csharpKind == kind7;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this SyntaxNode? node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5, SyntaxKind kind6, SyntaxKind kind7, SyntaxKind kind8, SyntaxKind kind9, SyntaxKind kind10, SyntaxKind kind11)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5 || csharpKind == kind6 || csharpKind == kind7 || csharpKind == kind8 || csharpKind == kind9 || csharpKind == kind10 || csharpKind == kind11;
        }

        public static IEnumerable<SyntaxTrivia> GetAllPrecedingTriviaToPreviousToken(
            this SyntaxNode node, SourceText? sourceText = null,
            bool includePreviousTokenTrailingTriviaOnlyIfOnSameLine = false)
            => node.GetFirstToken().GetAllPrecedingTriviaToPreviousToken(
                sourceText, includePreviousTokenTrailingTriviaOnlyIfOnSameLine);

        /// <summary>
        /// Returns all of the trivia to the left of this token up to the previous token (concatenates
        /// the previous token's trailing trivia and this token's leading trivia).
        /// </summary>
        public static IEnumerable<SyntaxTrivia> GetAllPrecedingTriviaToPreviousToken(
            this SyntaxToken token, SourceText? sourceText = null,
            bool includePreviousTokenTrailingTriviaOnlyIfOnSameLine = false)
        {
            var prevToken = token.GetPreviousToken(includeSkipped: true);
            if (prevToken.Kind() == SyntaxKind.None)
            {
                return token.LeadingTrivia;
            }

            Contract.ThrowIfTrue(sourceText == null && includePreviousTokenTrailingTriviaOnlyIfOnSameLine, "If we are including previous token trailing trivia, we need the text too.");
            if (includePreviousTokenTrailingTriviaOnlyIfOnSameLine &&
                !sourceText!.AreOnSameLine(prevToken, token))
            {
                return token.LeadingTrivia;
            }

            return prevToken.TrailingTrivia.Concat(token.LeadingTrivia);
        }

        public static bool IsAnyArgumentList([NotNullWhen(returnValue: true)] this SyntaxNode? node)
        {
            return node.IsKind(SyntaxKind.ArgumentList) ||
                   node.IsKind(SyntaxKind.AttributeArgumentList) ||
                   node.IsKind(SyntaxKind.BracketedArgumentList) ||
                   node.IsKind(SyntaxKind.TypeArgumentList);
        }

        public static (SyntaxToken openBrace, SyntaxToken closeBrace) GetBraces(this SyntaxNode node)
        {
            switch (node)
            {
                case NamespaceDeclarationSyntax namespaceNode:
                    return (namespaceNode.OpenBraceToken, namespaceNode.CloseBraceToken);
                case BaseTypeDeclarationSyntax baseTypeNode:
                    return (baseTypeNode.OpenBraceToken, baseTypeNode.CloseBraceToken);
                case AccessorListSyntax accessorListNode:
                    return (accessorListNode.OpenBraceToken, accessorListNode.CloseBraceToken);
                case BlockSyntax blockNode:
                    return (blockNode.OpenBraceToken, blockNode.CloseBraceToken);
                case SwitchStatementSyntax switchStatementNode:
                    return (switchStatementNode.OpenBraceToken, switchStatementNode.CloseBraceToken);
                case AnonymousObjectCreationExpressionSyntax anonymousObjectCreationExpression:
                    return (anonymousObjectCreationExpression.OpenBraceToken, anonymousObjectCreationExpression.CloseBraceToken);
                case InitializerExpressionSyntax initializeExpressionNode:
                    return (initializeExpressionNode.OpenBraceToken, initializeExpressionNode.CloseBraceToken);
                case SwitchExpressionSyntax switchExpression:
                    return (switchExpression.OpenBraceToken, switchExpression.CloseBraceToken);
                case PropertyPatternClauseSyntax property:
                    return (property.OpenBraceToken, property.CloseBraceToken);
            }

            return default;
        }

        public static bool IsEmbeddedStatementOwner([NotNullWhen(returnValue: true)] this SyntaxNode? node)
        {
            return node is DoStatementSyntax ||
                   node is ElseClauseSyntax ||
                   node is FixedStatementSyntax ||
                   node is CommonForEachStatementSyntax ||
                   node is ForStatementSyntax ||
                   node is IfStatementSyntax ||
                   node is LabeledStatementSyntax ||
                   node is LockStatementSyntax ||
                   node is UsingStatementSyntax ||
                   node is WhileStatementSyntax;
        }

        public static StatementSyntax? GetEmbeddedStatement(this SyntaxNode node)
            => node switch
            {
                DoStatementSyntax n => n.Statement,
                ElseClauseSyntax n => n.Statement,
                FixedStatementSyntax n => n.Statement,
                CommonForEachStatementSyntax n => n.Statement,
                ForStatementSyntax n => n.Statement,
                IfStatementSyntax n => n.Statement,
                LabeledStatementSyntax n => n.Statement,
                LockStatementSyntax n => n.Statement,
                UsingStatementSyntax n => n.Statement,
                WhileStatementSyntax n => n.Statement,
                _ => null,
            };

        public static ConditionalAccessExpressionSyntax? GetParentConditionalAccessExpression(this SyntaxNode node)
        {
            var current = node;
            while (current?.Parent != null)
            {
                if (current.IsParentKind(SyntaxKind.ConditionalAccessExpression) &&
                    ((ConditionalAccessExpressionSyntax)current.Parent).WhenNotNull == current)
                {
                    return (ConditionalAccessExpressionSyntax)current.Parent;
                }

                current = current.Parent;
            }

            return null;
        }

        public static bool IsInStaticContext(this SyntaxNode node)
        {
            // this/base calls are always static.
            if (node.FirstAncestorOrSelf<ConstructorInitializerSyntax>() != null)
            {
                return true;
            }

            var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (memberDeclaration == null)
            {
                return false;
            }

            switch (memberDeclaration.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.IndexerDeclaration:
                    return memberDeclaration.GetModifiers().Any(SyntaxKind.StaticKeyword);

                case SyntaxKind.PropertyDeclaration:
                    return memberDeclaration.GetModifiers().Any(SyntaxKind.StaticKeyword) ||
                        node.IsFoundUnder((PropertyDeclarationSyntax p) => p.Initializer!);

                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    // Inside a field one can only access static members of a type (unless it's top-level).
                    return !memberDeclaration.Parent.IsKind(SyntaxKind.CompilationUnit);

                case SyntaxKind.DestructorDeclaration:
                    return false;
            }

            // Global statements are not a static context.
            if (node.FirstAncestorOrSelf<GlobalStatementSyntax>() != null)
            {
                return false;
            }

            // any other location is considered static
            return true;
        }

        public static bool IsUnsafeContext(this SyntaxNode node)
        {
            if (node.GetAncestor<UnsafeStatementSyntax>() != null)
            {
                return true;
            }

            return node.GetAncestors<MemberDeclarationSyntax>().Any(
                m => m.GetModifiers().Any(SyntaxKind.UnsafeKeyword));
        }

        public static bool IsLeftSideOfAssignExpression(this SyntaxNode node)
        {
            return node.IsParentKind(SyntaxKind.SimpleAssignmentExpression) &&
                ((AssignmentExpressionSyntax)node.Parent!).Left == node;
        }

        public static TNode ConvertToSingleLine<TNode>(this TNode node, bool useElasticTrivia = false)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                return node!;
            }

            var rewriter = new SingleLineRewriter(useElasticTrivia);
            return (TNode)rewriter.Visit(node);
        }

        public static SyntaxTokenList GetModifiers(this SyntaxNode member)
        {
            switch (member)
            {
                case MemberDeclarationSyntax memberDecl: return memberDecl.Modifiers;
                case AccessorDeclarationSyntax accessor: return accessor.Modifiers;
            }

            return default;
        }
    }
}
