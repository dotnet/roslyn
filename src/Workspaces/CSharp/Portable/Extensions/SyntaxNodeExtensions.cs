// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using SyntaxNodeOrTokenExtensions = Microsoft.CodeAnalysis.Shared.Extensions.SyntaxNodeOrTokenExtensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxNodeExtensions
    {
        /// <summary>
        /// Returns the list of using directives that affect <paramref name="node"/>. The list will be returned in
        /// top down order.  
        /// </summary>
        public static IEnumerable<UsingDirectiveSyntax> GetEnclosingUsingDirectives(this SyntaxNode node)
        {
            return node.GetAncestorOrThis<CompilationUnitSyntax>().Usings
                       .Concat(node.GetAncestorsOrThis<NamespaceDeclarationSyntax>()
                                   .Reverse()
                                   .SelectMany(n => n.Usings));
        }

        public static IEnumerable<ExternAliasDirectiveSyntax> GetEnclosingExternAliasDirectives(this SyntaxNode node)
        {
            return node.GetAncestorOrThis<CompilationUnitSyntax>().Externs
                       .Concat(node.GetAncestorsOrThis<NamespaceDeclarationSyntax>()
                                   .Reverse()
                                   .SelectMany(n => n.Externs));
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
                        node.IsFoundUnder((PropertyDeclarationSyntax p) => p.Initializer);

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

        public static NamespaceDeclarationSyntax GetInnermostNamespaceDeclarationWithUsings(this SyntaxNode contextNode)
        {
            var usingDirectiveAncestor = contextNode.GetAncestor<UsingDirectiveSyntax>();
            if (usingDirectiveAncestor == null)
            {
                return contextNode.GetAncestorsOrThis<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
            }
            else
            {
                // We are inside a using directive. In this case, we should find and return the first 'parent' namespace with usings.
                var containingNamespace = usingDirectiveAncestor.GetAncestor<NamespaceDeclarationSyntax>();
                if (containingNamespace == null)
                {
                    // We are inside a top level using directive (i.e. one that's directly in the compilation unit).
                    return null;
                }
                else
                {
                    return containingNamespace.GetAncestors<NamespaceDeclarationSyntax>().FirstOrDefault(n => n.Usings.Count > 0);
                }
            }
        }

        public static bool IsBreakableConstruct(this SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DoStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.SwitchStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    return true;
            }

            return false;
        }

        public static bool IsContinuableConstruct(this SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.DoStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                    return true;
            }

            return false;
        }

        public static bool IsReturnableConstruct(this SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.LocalFunctionStatement:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return true;
            }

            return false;
        }

        public static bool SpansPreprocessorDirective<TSyntaxNode>(this IEnumerable<TSyntaxNode> list) where TSyntaxNode : SyntaxNode
            => CSharpSyntaxFactsService.Instance.SpansPreprocessorDirective(list);

        public static TNode ConvertToSingleLine<TNode>(this TNode node, bool useElasticTrivia = false)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                return node;
            }

            var rewriter = new SingleLineRewriter(useElasticTrivia);
            return (TNode)rewriter.Visit(node);
        }

        public static bool IsAsyncSupportingFunctionSyntax(this SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.MethodDeclaration)
                || node.IsAnyLambdaOrAnonymousMethod()
                || node.IsKind(SyntaxKind.LocalFunctionStatement);
        }

        public static bool IsAnyLambda(this SyntaxNode node)
        {
            return
                node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) ||
                node.IsKind(SyntaxKind.SimpleLambdaExpression);
        }

        public static bool IsAnyLambdaOrAnonymousMethod(this SyntaxNode node)
            => node.IsAnyLambda() || node.IsKind(SyntaxKind.AnonymousMethodExpression);

        /// <summary>
        /// Returns true if the passed in node contains an interleaved pp directive.
        /// 
        /// i.e. The following returns false:
        /// 
        ///   void Goo() {
        /// #if true
        /// #endif
        ///   }
        /// 
        /// #if true
        ///   void Goo() {
        ///   }
        /// #endif
        /// 
        /// but these return true:
        /// 
        /// #if true
        ///   void Goo() {
        /// #endif
        ///   }
        /// 
        ///   void Goo() {
        /// #if true
        ///   }
        /// #endif
        /// 
        /// #if true
        ///   void Goo() {
        /// #else
        ///   }
        /// #endif
        /// 
        /// i.e. the method returns true if it contains a PP directive that belongs to a grouping
        /// constructs (like #if/#endif or #region/#endregion), but the grouping construct isn't
        /// entirely contained within the span of the node.
        /// </summary>
        public static bool ContainsInterleavedDirective(this SyntaxNode syntaxNode, CancellationToken cancellationToken)
            => CSharpSyntaxFactsService.Instance.ContainsInterleavedDirective(syntaxNode, cancellationToken);

        /// <summary>
        /// Similar to <see cref="ContainsInterleavedDirective(SyntaxNode, CancellationToken)"/> except that the span to check
        /// for interleaved directives can be specified separately to the node passed in.
        /// </summary>
        public static bool ContainsInterleavedDirective(this SyntaxNode syntaxNode, TextSpan span, CancellationToken cancellationToken)
            => CSharpSyntaxFactsService.Instance.ContainsInterleavedDirective(span, syntaxNode, cancellationToken);

        public static bool ContainsInterleavedDirective(
            this SyntaxToken token,
            TextSpan textSpan,
            CancellationToken cancellationToken)
        {
            return
                ContainsInterleavedDirective(textSpan, token.LeadingTrivia, cancellationToken) ||
                ContainsInterleavedDirective(textSpan, token.TrailingTrivia, cancellationToken);
        }

        private static bool ContainsInterleavedDirective(
            TextSpan textSpan,
            SyntaxTriviaList list,
            CancellationToken cancellationToken)
        {
            foreach (var trivia in list)
            {
                if (textSpan.Contains(trivia.Span))
                {
                    if (ContainsInterleavedDirective(textSpan, trivia, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool ContainsInterleavedDirective(
            TextSpan textSpan,
            SyntaxTrivia trivia,
            CancellationToken cancellationToken)
        {
            if (trivia.HasStructure)
            {
                var structure = trivia.GetStructure();
                var parentSpan = structure.Span;
                if (trivia.GetStructure().IsKind(SyntaxKind.RegionDirectiveTrivia,
                                                 SyntaxKind.EndRegionDirectiveTrivia,
                                                 SyntaxKind.IfDirectiveTrivia,
                                                 SyntaxKind.EndIfDirectiveTrivia))
                {
                    var match = ((DirectiveTriviaSyntax)structure).GetMatchingDirective(cancellationToken);
                    if (match != null)
                    {
                        var matchSpan = match.Span;
                        if (!textSpan.Contains(matchSpan.Start))
                        {
                            // The match for this pp directive is outside
                            // this node.
                            return true;
                        }
                    }
                }
                else if (trivia.GetStructure().IsKind(SyntaxKind.ElseDirectiveTrivia, SyntaxKind.ElifDirectiveTrivia))
                {
                    var directives = ((DirectiveTriviaSyntax)structure).GetMatchingConditionalDirectives(cancellationToken);
                    if (directives != null && directives.Count > 0)
                    {
                        if (!textSpan.Contains(directives[0].SpanStart) ||
                            !textSpan.Contains(directives[directives.Count - 1].SpanStart))
                        {
                            // This else/elif belongs to a pp span that isn't 
                            // entirely within this node.
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Breaks up the list of provided nodes, based on how they are interspersed with pp
        /// directives, into groups.  Within these groups nodes can be moved around safely, without
        /// breaking any pp constructs.
        /// </summary>
        public static IList<IList<TSyntaxNode>> SplitNodesOnPreprocessorBoundaries<TSyntaxNode>(
            this IEnumerable<TSyntaxNode> nodes,
            CancellationToken cancellationToken)
            where TSyntaxNode : SyntaxNode
        {
            var result = new List<IList<TSyntaxNode>>();

            var currentGroup = new List<TSyntaxNode>();
            foreach (var node in nodes)
            {
                var hasUnmatchedInteriorDirective = node.ContainsInterleavedDirective(cancellationToken);
                var hasLeadingDirective = node.GetLeadingTrivia().Any(t => SyntaxFacts.IsPreprocessorDirective(t.Kind()));

                if (hasUnmatchedInteriorDirective)
                {
                    // we have a #if/#endif/#region/#endregion/#else/#elif in
                    // this node that belongs to a span of pp directives that
                    // is not entirely contained within the node.  i.e.:
                    //
                    //   void Goo() {
                    //      #if ...
                    //   }
                    //
                    // This node cannot be moved at all.  It is in a group that
                    // only contains itself (and thus can never be moved).

                    // add whatever group we've built up to now. And reset the 
                    // next group to empty.
                    result.Add(currentGroup);
                    currentGroup = new List<TSyntaxNode>();

                    result.Add(new List<TSyntaxNode> { node });
                }
                else if (hasLeadingDirective)
                {
                    // We have a PP directive before us.  i.e.:
                    // 
                    //   #if ...
                    //      void Goo() {
                    //
                    // That means we start a new group that is contained between
                    // the above directive and the following directive.

                    // add whatever group we've built up to now. And reset the 
                    // next group to empty.
                    result.Add(currentGroup);
                    currentGroup = new List<TSyntaxNode>();

                    currentGroup.Add(node);
                }
                else
                {
                    // simple case.  just add ourselves to the current group
                    currentGroup.Add(node);
                }
            }

            // add the remainder of the final group.
            result.Add(currentGroup);

            // Now, filter out any empty groups.
            result = result.Where(group => !group.IsEmpty()).ToList();
            return result;
        }

        public static ImmutableArray<SyntaxTrivia> GetLeadingBlankLines<TSyntaxNode>(this TSyntaxNode node) where TSyntaxNode : SyntaxNode
            => CSharpSyntaxFactsService.Instance.GetLeadingBlankLines(node);

        public static TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(this TSyntaxNode node) where TSyntaxNode : SyntaxNode
            => CSharpSyntaxFactsService.Instance.GetNodeWithoutLeadingBlankLines(node);

        public static TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(this TSyntaxNode node, out ImmutableArray<SyntaxTrivia> strippedTrivia) where TSyntaxNode : SyntaxNode
            => CSharpSyntaxFactsService.Instance.GetNodeWithoutLeadingBlankLines(node, out strippedTrivia);

        public static ImmutableArray<SyntaxTrivia> GetLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(this TSyntaxNode node) where TSyntaxNode : SyntaxNode
            => CSharpSyntaxFactsService.Instance.GetLeadingBannerAndPreprocessorDirectives(node);

        public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(this TSyntaxNode node) where TSyntaxNode : SyntaxNode
            => CSharpSyntaxFactsService.Instance.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(node);

        public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(this TSyntaxNode node, out ImmutableArray<SyntaxTrivia> strippedTrivia) where TSyntaxNode : SyntaxNode
            => CSharpSyntaxFactsService.Instance.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(node, out strippedTrivia);

        public static bool IsAnyAssignExpression(this SyntaxNode node)
            => SyntaxFacts.IsAssignmentExpression(node.Kind());

        public static bool IsCompoundAssignExpression(this SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.AddAssignmentExpression:
                case SyntaxKind.SubtractAssignmentExpression:
                case SyntaxKind.MultiplyAssignmentExpression:
                case SyntaxKind.DivideAssignmentExpression:
                case SyntaxKind.ModuloAssignmentExpression:
                case SyntaxKind.AndAssignmentExpression:
                case SyntaxKind.ExclusiveOrAssignmentExpression:
                case SyntaxKind.OrAssignmentExpression:
                case SyntaxKind.LeftShiftAssignmentExpression:
                case SyntaxKind.RightShiftAssignmentExpression:
                    return true;
            }

            return false;
        }

        public static bool IsLeftSideOfAssignExpression(this SyntaxNode node)
        {
            return node.IsParentKind(SyntaxKind.SimpleAssignmentExpression) &&
                ((AssignmentExpressionSyntax)node.Parent).Left == node;
        }

        public static bool IsLeftSideOfAnyAssignExpression(this SyntaxNode node)
        {
            return node is
            {
                Parent: AssignmentExpressionSyntax { Left: node }
            }
&& node.Parent.IsAnyAssignExpression();
        }

        public static bool IsRightSideOfAnyAssignExpression(this SyntaxNode node)
        {
            return node.Parent.IsAnyAssignExpression() &&
                ((AssignmentExpressionSyntax)node.Parent).Right == node;
        }

        public static bool IsVariableDeclaratorValue(this SyntaxNode node)
        {
            return
                node.IsParentKind(SyntaxKind.EqualsValueClause) &&
                node.Parent.IsParentKind(SyntaxKind.VariableDeclarator) &&
                ((EqualsValueClauseSyntax)node.Parent).Value == node;
        }

        public static BlockSyntax FindInnermostCommonBlock(this IEnumerable<SyntaxNode> nodes)
        {
            return nodes.FindInnermostCommonNode<BlockSyntax>();
        }

        public static IEnumerable<SyntaxNode> GetAncestorsOrThis(this SyntaxNode node, Func<SyntaxNode, bool> predicate)
        {
            var current = node;
            while (current != null)
            {
                if (predicate(current))
                {
                    yield return current;
                }

                current = current.Parent;
            }
        }

        /// <summary>
        /// Returns child node or token that contains given position.
        /// </summary>
        /// <remarks>
        /// This is a copy of <see cref="SyntaxNode.ChildThatContainsPosition"/> that also returns the index of the child node.
        /// </remarks>
        internal static SyntaxNodeOrToken ChildThatContainsPosition(this SyntaxNode self, int position, out int childIndex)
        {
            var childList = self.ChildNodesAndTokens();

            var left = 0;
            var right = childList.Count - 1;

            while (left <= right)
            {
                var middle = left + ((right - left) / 2);
                var node = childList[middle];

                var span = node.FullSpan;
                if (position < span.Start)
                {
                    right = middle - 1;
                }
                else if (position >= span.End)
                {
                    left = middle + 1;
                }
                else
                {
                    childIndex = middle;
                    return node;
                }
            }

            // we could check up front that index is within FullSpan,
            // but we wan to optimize for the common case where position is valid.
            Debug.Assert(!self.FullSpan.Contains(position), "Position is valid. How could we not find a child?");
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        public static (SyntaxToken openBrace, SyntaxToken closeBrace) GetParentheses(this SyntaxNode node)
        {
            switch (node)
            {
                case ParenthesizedExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case MakeRefExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case RefTypeExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case RefValueExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case CheckedExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case DefaultExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case TypeOfExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case SizeOfExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case ArgumentListSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case CastExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case WhileStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case DoStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case ForStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case CommonForEachStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case UsingStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case FixedStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case LockStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case IfStatementSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case SwitchStatementSyntax n when n.OpenParenToken != default: return (n.OpenParenToken, n.CloseParenToken);
                case TupleExpressionSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case CatchDeclarationSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case AttributeArgumentListSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case ConstructorConstraintSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                case ParameterListSyntax n: return (n.OpenParenToken, n.CloseParenToken);
                default: return default;
            }
        }

        public static (SyntaxToken openBrace, SyntaxToken closeBrace) GetBrackets(this SyntaxNode node)
        {
            switch (node)
            {
                case ArrayRankSpecifierSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                case BracketedArgumentListSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                case ImplicitArrayCreationExpressionSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                case AttributeListSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                case BracketedParameterListSyntax n: return (n.OpenBracketToken, n.CloseBracketToken);
                default: return default;
            }
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

        public static SyntaxNode WithModifiers(this SyntaxNode member, SyntaxTokenList modifiers)
        {
            switch (member)
            {
                case MemberDeclarationSyntax memberDecl: return memberDecl.WithModifiers(modifiers);
                case AccessorDeclarationSyntax accessor: return accessor.WithModifiers(modifiers);
            }

            return null;
        }

        public static bool CheckTopLevel(this SyntaxNode node, TextSpan span)
        {
            switch (node)
            {
                case BlockSyntax block:
                    return block.ContainsInBlockBody(span);
                case ArrowExpressionClauseSyntax expressionBodiedMember:
                    return expressionBodiedMember.ContainsInExpressionBodiedMemberBody(span);
                case FieldDeclarationSyntax field:
                    {
                        foreach (var variable in field.Declaration.Variables)
                        {
                            if (variable.Initializer != null && variable.Initializer.Span.Contains(span))
                            {
                                return true;
                            }
                        }

                        break;
                    }

                case GlobalStatementSyntax global:
                    return true;
                case ConstructorInitializerSyntax constructorInitializer:
                    return constructorInitializer.ContainsInArgument(span);
            }

            return false;
        }

        public static bool ContainsInArgument(this ConstructorInitializerSyntax initializer, TextSpan textSpan)
        {
            if (initializer == null)
            {
                return false;
            }

            return initializer.ArgumentList.Arguments.Any(a => a.Span.Contains(textSpan));
        }

        public static bool ContainsInBlockBody(this BlockSyntax block, TextSpan textSpan)
        {
            if (block == null)
            {
                return false;
            }

            var blockSpan = TextSpan.FromBounds(block.OpenBraceToken.Span.End, block.CloseBraceToken.SpanStart);
            return blockSpan.Contains(textSpan);
        }

        public static bool ContainsInExpressionBodiedMemberBody(this ArrowExpressionClauseSyntax expressionBodiedMember, TextSpan textSpan)
        {
            if (expressionBodiedMember == null)
            {
                return false;
            }

            var expressionBodiedMemberBody = TextSpan.FromBounds(expressionBodiedMember.Expression.SpanStart, expressionBodiedMember.Expression.Span.End);
            return expressionBodiedMemberBody.Contains(textSpan);
        }

        public static IEnumerable<MemberDeclarationSyntax> GetMembers(this SyntaxNode node)
        {
            switch (node)
            {
                case CompilationUnitSyntax compilation:
                    return compilation.Members;
                case NamespaceDeclarationSyntax @namespace:
                    return @namespace.Members;
                case TypeDeclarationSyntax type:
                    return type.Members;
                case EnumDeclarationSyntax @enum:
                    return @enum.Members;
            }

            return SpecializedCollections.EmptyEnumerable<MemberDeclarationSyntax>();
        }

        public static ConditionalAccessExpressionSyntax GetParentConditionalAccessExpression(this SyntaxNode node)
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

        public static ConditionalAccessExpressionSyntax GetInnerMostConditionalAccessExpression(this SyntaxNode node)
        {
            if (!(node is ConditionalAccessExpressionSyntax))
            {
                return null;
            }

            var result = (ConditionalAccessExpressionSyntax)node;
            while (result.WhenNotNull is ConditionalAccessExpressionSyntax)
            {
                result = (ConditionalAccessExpressionSyntax)result.WhenNotNull;
            }

            return result;
        }

        public static bool IsInExpressionTree(
            this SyntaxNode node, SemanticModel semanticModel,
            INamedTypeSymbol expressionTypeOpt, CancellationToken cancellationToken)
        {
            if (expressionTypeOpt != null)
            {
                for (var current = node; current != null; current = current.Parent)
                {
                    if (current.IsAnyLambda())
                    {
                        var typeInfo = semanticModel.GetTypeInfo(current, cancellationToken);
                        if (expressionTypeOpt.Equals(typeInfo.ConvertedType?.OriginalDefinition))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static SyntaxNode WithPrependedNonIndentationTriviaFrom(
            this SyntaxNode to, SyntaxNode from)
        {
            // get all the preceding trivia from the 'from' node, not counting the leading
            // indentation trivia is has.
            var finalTrivia = from.GetLeadingTrivia().ToList();
            while (finalTrivia.Count > 0 && finalTrivia.Last().Kind() == SyntaxKind.WhitespaceTrivia)
            {
                finalTrivia.RemoveAt(finalTrivia.Count - 1);
            }

            // Also, add on the trailing trivia if there are trailing comments.
            var hasTrailingComments = from.GetTrailingTrivia().Any(t => t.IsRegularComment());
            if (hasTrailingComments)
            {
                finalTrivia.AddRange(from.GetTrailingTrivia());
            }

            // Merge this trivia with the existing trivia on the node.  Format in case
            // we added comments and need them indented properly.
            return to.WithPrependedLeadingTrivia(finalTrivia)
                     .WithAdditionalAnnotations(Formatter.Annotation);
        }

        public static bool IsInDeconstructionLeft(this SyntaxNode node, out SyntaxNode deconstructionLeft)
        {
            SyntaxNode previous = null;
            for (var current = node; current != null; current = current.Parent)
            {
                if ((current is AssignmentExpressionSyntax assignment && previous == assignment.Left && assignment.IsDeconstruction()) ||
                    (current is ForEachVariableStatementSyntax @foreach && previous == @foreach.Variable))
                {
                    deconstructionLeft = previous;
                    return true;
                }

                if (current is StatementSyntax)
                {
                    break;
                }

                previous = current;
            }

            deconstructionLeft = null;
            return false;
        }

        public static T WithCommentsFrom<T>(this T node, SyntaxToken leadingToken, SyntaxToken trailingToken)
            where T : SyntaxNode
            => node.WithCommentsFrom(
                SyntaxNodeOrTokenExtensions.GetTrivia(leadingToken),
                SyntaxNodeOrTokenExtensions.GetTrivia(trailingToken));

        public static T WithCommentsFrom<T>(
            this T node,
            IEnumerable<SyntaxToken> leadingTokens,
            IEnumerable<SyntaxToken> trailingTokens)
            where T : SyntaxNode
            => node.WithCommentsFrom(leadingTokens.GetTrivia(), trailingTokens.GetTrivia());

        public static T WithCommentsFrom<T>(
            this T node,
            IEnumerable<SyntaxTrivia> leadingTrivia,
            IEnumerable<SyntaxTrivia> trailingTrivia,
            params SyntaxNodeOrToken[] trailingNodesOrTokens)
            where T : SyntaxNode
            => node
                .WithLeadingTrivia(leadingTrivia.Concat(node.GetLeadingTrivia()).FilterComments(addElasticMarker: false))
                .WithTrailingTrivia(
                    node.GetTrailingTrivia().Concat(SyntaxNodeOrTokenExtensions.GetTrivia(trailingNodesOrTokens).Concat(trailingTrivia)).FilterComments(addElasticMarker: false));

        public static T KeepCommentsAndAddElasticMarkers<T>(this T node) where T : SyntaxNode
            => node
            .WithTrailingTrivia(node.GetTrailingTrivia().FilterComments(addElasticMarker: true))
            .WithLeadingTrivia(node.GetLeadingTrivia().FilterComments(addElasticMarker: true));
    }
}
