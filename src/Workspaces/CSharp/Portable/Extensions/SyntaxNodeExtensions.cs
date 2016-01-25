// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class SyntaxNodeExtensions
    {
        public static bool IsParentKind(this SyntaxNode node, SyntaxKind kind)
        {
            return node != null && CodeAnalysis.CSharpExtensions.IsKind(node.Parent, kind);
        }

        public static bool IsKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2;
        }

        public static bool IsKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3;
        }

        public static bool IsKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4;
        }

        public static bool IsKind(this SyntaxNode node, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3, SyntaxKind kind4, SyntaxKind kind5)
        {
            if (node == null)
            {
                return false;
            }

            var csharpKind = node.Kind();
            return csharpKind == kind1 || csharpKind == kind2 || csharpKind == kind3 || csharpKind == kind4 || csharpKind == kind5;
        }

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
                    return node.IsFoundUnder((PropertyDeclarationSyntax p) => p.Initializer);

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

        // Matches the following:
        //
        // (whitespace* newline)+ 
        private static readonly Matcher<SyntaxTrivia> s_oneOrMoreBlankLines;

        // Matches the following:
        // 
        // (whitespace* (single-comment|multi-comment) whitespace* newline)+ OneOrMoreBlankLines
        private static readonly Matcher<SyntaxTrivia> s_bannerMatcher;

        // Used to match the following:
        //
        // <start-of-file> (whitespace* (single-comment|multi-comment) whitespace* newline)+ blankLine*
        private static readonly Matcher<SyntaxTrivia> s_fileBannerMatcher;

        static SyntaxNodeExtensions()
        {
            var whitespace = Matcher.Repeat(Match(SyntaxKind.WhitespaceTrivia, "\\b"));
            var endOfLine = Match(SyntaxKind.EndOfLineTrivia, "\\n");
            var singleBlankLine = Matcher.Sequence(whitespace, endOfLine);

            var shebangComment = Match(SyntaxKind.ShebangDirectiveTrivia, "#!");
            var singleLineComment = Match(SyntaxKind.SingleLineCommentTrivia, "//");
            var multiLineComment = Match(SyntaxKind.MultiLineCommentTrivia, "/**/");
            var anyCommentMatcher = Matcher.Choice(shebangComment, singleLineComment, multiLineComment);

            var commentLine = Matcher.Sequence(whitespace, anyCommentMatcher, whitespace, endOfLine);

            s_oneOrMoreBlankLines = Matcher.OneOrMore(singleBlankLine);
            s_bannerMatcher =
                Matcher.Sequence(
                    Matcher.OneOrMore(commentLine),
                    s_oneOrMoreBlankLines);
            s_fileBannerMatcher =
                Matcher.Sequence(
                    Matcher.OneOrMore(commentLine),
                    Matcher.Repeat(singleBlankLine));
        }

        private static Matcher<SyntaxTrivia> Match(SyntaxKind kind, string description)
        {
            return Matcher.Single<SyntaxTrivia>(t => t.Kind() == kind, description);
        }

        /// <summary>
        /// Returns all of the trivia to the left of this token up to the previous token (concatenates
        /// the previous token's trailing trivia and this token's leading trivia).
        /// </summary>
        public static IEnumerable<SyntaxTrivia> GetAllPrecedingTriviaToPreviousToken(this SyntaxToken token)
        {
            var prevToken = token.GetPreviousToken(includeSkipped: true);
            if (prevToken.Kind() == SyntaxKind.None)
            {
                return token.LeadingTrivia;
            }

            return prevToken.TrailingTrivia.Concat(token.LeadingTrivia);
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
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                    return true;
            }

            return false;
        }

        public static bool SpansPreprocessorDirective<TSyntaxNode>(
            this IEnumerable<TSyntaxNode> list)
            where TSyntaxNode : SyntaxNode
        {
            if (list == null || list.IsEmpty())
            {
                return false;
            }

            var tokens = list.SelectMany(n => n.DescendantTokens());

            // todo: we need to dive into trivia here.
            return tokens.SpansPreprocessorDirective();
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            params SyntaxTrivia[] trivia) where T : SyntaxNode
        {
            if (trivia.Length == 0)
            {
                return node;
            }

            return node.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            SyntaxTriviaList trivia) where T : SyntaxNode
        {
            if (trivia.Count == 0)
            {
                return node;
            }

            return node.WithLeadingTrivia(trivia.Concat(node.GetLeadingTrivia()));
        }

        public static T WithPrependedLeadingTrivia<T>(
            this T node,
            IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
        {
            return node.WithPrependedLeadingTrivia(trivia.ToSyntaxTriviaList());
        }

        public static T WithAppendedTrailingTrivia<T>(
            this T node,
            params SyntaxTrivia[] trivia) where T : SyntaxNode
        {
            if (trivia.Length == 0)
            {
                return node;
            }

            return node.WithAppendedTrailingTrivia((IEnumerable<SyntaxTrivia>)trivia);
        }

        public static T WithAppendedTrailingTrivia<T>(
            this T node,
            SyntaxTriviaList trivia) where T : SyntaxNode
        {
            if (trivia.Count == 0)
            {
                return node;
            }

            return node.WithTrailingTrivia(node.GetTrailingTrivia().Concat(trivia));
        }

        public static T WithAppendedTrailingTrivia<T>(
            this T node,
            IEnumerable<SyntaxTrivia> trivia) where T : SyntaxNode
        {
            return node.WithAppendedTrailingTrivia(trivia.ToSyntaxTriviaList());
        }

        public static T With<T>(
            this T node,
            IEnumerable<SyntaxTrivia> leadingTrivia,
            IEnumerable<SyntaxTrivia> trailingTrivia) where T : SyntaxNode
        {
            return node.WithLeadingTrivia(leadingTrivia).WithTrailingTrivia(trailingTrivia);
        }

        public static TNode ConvertToSingleLine<TNode>(this TNode node)
            where TNode : SyntaxNode
        {
            if (node == null)
            {
                return node;
            }

            var rewriter = new SingleLineRewriter();
            return (TNode)rewriter.Visit(node);
        }

        public static bool IsAnyArgumentList(this SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.ArgumentList) ||
                   node.IsKind(SyntaxKind.AttributeArgumentList) ||
                   node.IsKind(SyntaxKind.BracketedArgumentList) ||
                   node.IsKind(SyntaxKind.TypeArgumentList);
        }

        public static bool IsAnyLambda(this SyntaxNode node)
        {
            return
                node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) ||
                node.IsKind(SyntaxKind.SimpleLambdaExpression);
        }

        public static bool IsAnyLambdaOrAnonymousMethod(this SyntaxNode node)
        {
            return node.IsAnyLambda() || node.IsKind(SyntaxKind.AnonymousMethodExpression);
        }

        /// <summary>
        /// Returns true if the passed in node contains an interleaved pp directive.
        /// 
        /// i.e. The following returns false:
        /// 
        ///   void Foo() {
        /// #if true
        /// #endif
        ///   }
        /// 
        /// #if true
        ///   void Foo() {
        ///   }
        /// #endif
        /// 
        /// but these return true:
        /// 
        /// #if true
        ///   void Foo() {
        /// #endif
        ///   }
        /// 
        ///   void Foo() {
        /// #if true
        ///   }
        /// #endif
        /// 
        /// #if true
        ///   void Foo() {
        /// #else
        ///   }
        /// #endif
        /// 
        /// i.e. the method returns true if it contains a PP directive that belongs to a grouping
        /// constructs (like #if/#endif or #region/#endregion), but the grouping construct isn't
        /// entirely contained within the span of the node.
        /// </summary>
        public static bool ContainsInterleavedDirective(
            this SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            // Check if this node contains a start, middle or end pp construct whose matching construct is
            // not contained within this node.  If so, this node must be pinned and cannot move.

            var span = syntaxNode.Span;
            foreach (var token in syntaxNode.DescendantTokens())
            {
                if (ContainsInterleavedDirective(span, token, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsInterleavedDirective(
            TextSpan textSpan,
            SyntaxToken token,
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
                    //   void Foo() {
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
                    //      void Foo() {
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

        public static IEnumerable<SyntaxTrivia> GetLeadingBlankLines<TSyntaxNode>(
            this TSyntaxNode node)
            where TSyntaxNode : SyntaxNode
        {
            IEnumerable<SyntaxTrivia> blankLines;
            node.GetNodeWithoutLeadingBlankLines(out blankLines);
            return blankLines;
        }

        public static TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(
            this TSyntaxNode node)
            where TSyntaxNode : SyntaxNode
        {
            IEnumerable<SyntaxTrivia> blankLines;
            return node.GetNodeWithoutLeadingBlankLines(out blankLines);
        }

        public static TSyntaxNode GetNodeWithoutLeadingBlankLines<TSyntaxNode>(
            this TSyntaxNode node, out IEnumerable<SyntaxTrivia> strippedTrivia)
            where TSyntaxNode : SyntaxNode
        {
            var leadingTriviaToKeep = new List<SyntaxTrivia>(node.GetLeadingTrivia());

            var index = 0;
            s_oneOrMoreBlankLines.TryMatch(leadingTriviaToKeep, ref index);

            strippedTrivia = new List<SyntaxTrivia>(leadingTriviaToKeep.Take(index));

            return node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index));
        }

        public static IEnumerable<SyntaxTrivia> GetLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
            this TSyntaxNode node)
            where TSyntaxNode : SyntaxNode
        {
            IEnumerable<SyntaxTrivia> leadingTrivia;
            node.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(out leadingTrivia);
            return leadingTrivia;
        }

        public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
            this TSyntaxNode node)
            where TSyntaxNode : SyntaxNode
        {
            IEnumerable<SyntaxTrivia> strippedTrivia;
            return node.GetNodeWithoutLeadingBannerAndPreprocessorDirectives(out strippedTrivia);
        }

        public static TSyntaxNode GetNodeWithoutLeadingBannerAndPreprocessorDirectives<TSyntaxNode>(
            this TSyntaxNode node, out IEnumerable<SyntaxTrivia> strippedTrivia)
            where TSyntaxNode : SyntaxNode
        {
            var leadingTrivia = node.GetLeadingTrivia();

            // Rules for stripping trivia: 
            // 1) If there is a pp directive, then it (and all preceding trivia) *must* be stripped.
            //    This rule supersedes all other rules.
            // 2) If there is a doc comment, it cannot be stripped.  Even if there is a doc comment,
            //    followed by 5 new lines, then the doc comment still must stay with the node.  This
            //    rule does *not* supersede rule 1.
            // 3) Single line comments in a group (i.e. with no blank lines between them) belong to
            //    the node *iff* there is no blank line between it and the following trivia.

            List<SyntaxTrivia> leadingTriviaToStrip, leadingTriviaToKeep;

            int ppIndex = -1;
            for (int i = leadingTrivia.Count - 1; i >= 0; i--)
            {
                if (SyntaxFacts.IsPreprocessorDirective(leadingTrivia[i].Kind()))
                {
                    ppIndex = i;
                    break;
                }
            }

            if (ppIndex != -1)
            {
                // We have a pp directive.  it (and all previous trivia) must be stripped.
                leadingTriviaToStrip = new List<SyntaxTrivia>(leadingTrivia.Take(ppIndex + 1));
                leadingTriviaToKeep = new List<SyntaxTrivia>(leadingTrivia.Skip(ppIndex + 1));
            }
            else
            {
                leadingTriviaToKeep = new List<SyntaxTrivia>(leadingTrivia);
                leadingTriviaToStrip = new List<SyntaxTrivia>();
            }

            // Now, consume as many banners as we can.  s_fileBannerMatcher will only be matched at
            // the start of the file.
            var index = 0;
            while (
                s_oneOrMoreBlankLines.TryMatch(leadingTriviaToKeep, ref index) ||
                s_bannerMatcher.TryMatch(leadingTriviaToKeep, ref index) ||
                (node.FullSpan.Start == 0 && s_fileBannerMatcher.TryMatch(leadingTriviaToKeep, ref index)))
            {
            }

            leadingTriviaToStrip.AddRange(leadingTriviaToKeep.Take(index));

            strippedTrivia = leadingTriviaToStrip;
            return node.WithLeadingTrivia(leadingTriviaToKeep.Skip(index));
        }

        public static bool IsAnyAssignExpression(this SyntaxNode node)
        {
            return SyntaxFacts.IsAssignmentExpression(node.Kind());
        }

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
            return node.Parent.IsAnyAssignExpression() &&
                ((AssignmentExpressionSyntax)node.Parent).Left == node;
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

            int left = 0;
            int right = childList.Count - 1;

            while (left <= right)
            {
                int middle = left + ((right - left) / 2);
                SyntaxNodeOrToken node = childList[middle];

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

        public static SyntaxNode GetParent(this SyntaxNode node)
        {
            return node != null ? node.Parent : null;
        }

        public static ValueTuple<SyntaxToken, SyntaxToken> GetBraces(this SyntaxNode node)
        {
            var namespaceNode = node as NamespaceDeclarationSyntax;
            if (namespaceNode != null)
            {
                return ValueTuple.Create(namespaceNode.OpenBraceToken, namespaceNode.CloseBraceToken);
            }

            var baseTypeNode = node as BaseTypeDeclarationSyntax;
            if (baseTypeNode != null)
            {
                return ValueTuple.Create(baseTypeNode.OpenBraceToken, baseTypeNode.CloseBraceToken);
            }

            var accessorListNode = node as AccessorListSyntax;
            if (accessorListNode != null)
            {
                return ValueTuple.Create(accessorListNode.OpenBraceToken, accessorListNode.CloseBraceToken);
            }

            var blockNode = node as BlockSyntax;
            if (blockNode != null)
            {
                return ValueTuple.Create(blockNode.OpenBraceToken, blockNode.CloseBraceToken);
            }

            var switchStatementNode = node as SwitchStatementSyntax;
            if (switchStatementNode != null)
            {
                return ValueTuple.Create(switchStatementNode.OpenBraceToken, switchStatementNode.CloseBraceToken);
            }

            var anonymousObjectCreationExpression = node as AnonymousObjectCreationExpressionSyntax;
            if (anonymousObjectCreationExpression != null)
            {
                return ValueTuple.Create(anonymousObjectCreationExpression.OpenBraceToken, anonymousObjectCreationExpression.CloseBraceToken);
            }

            var initializeExpressionNode = node as InitializerExpressionSyntax;
            if (initializeExpressionNode != null)
            {
                return ValueTuple.Create(initializeExpressionNode.OpenBraceToken, initializeExpressionNode.CloseBraceToken);
            }

            return new ValueTuple<SyntaxToken, SyntaxToken>();
        }

        public static ValueTuple<SyntaxToken, SyntaxToken> GetParentheses(this SyntaxNode node)
        {
            return node.TypeSwitch(
                (ParenthesizedExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (MakeRefExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (RefTypeExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (RefValueExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (CheckedExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (DefaultExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (TypeOfExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (SizeOfExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ArgumentListSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (CastExpressionSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (WhileStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (DoStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ForStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ForEachStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (UsingStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (FixedStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (LockStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (IfStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (SwitchStatementSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (CatchDeclarationSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (AttributeArgumentListSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ConstructorConstraintSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (ParameterListSyntax n) => ValueTuple.Create(n.OpenParenToken, n.CloseParenToken),
                (SyntaxNode n) => default(ValueTuple<SyntaxToken, SyntaxToken>));
        }

        public static ValueTuple<SyntaxToken, SyntaxToken> GetBrackets(this SyntaxNode node)
        {
            return node.TypeSwitch(
                (ArrayRankSpecifierSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (BracketedArgumentListSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (ImplicitArrayCreationExpressionSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (AttributeListSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (BracketedParameterListSyntax n) => ValueTuple.Create(n.OpenBracketToken, n.CloseBracketToken),
                (SyntaxNode n) => default(ValueTuple<SyntaxToken, SyntaxToken>));
        }

        public static bool IsEmbeddedStatementOwner(this SyntaxNode node)
        {
            return
                   node is DoStatementSyntax ||
                   node is ElseClauseSyntax ||
                   node is FixedStatementSyntax ||
                   node is ForEachStatementSyntax ||
                   node is ForStatementSyntax ||
                   node is IfStatementSyntax ||
                   node is LabeledStatementSyntax ||
                   node is LockStatementSyntax ||
                   node is UsingStatementSyntax ||
                   node is WhileStatementSyntax;
        }

        public static StatementSyntax GetEmbeddedStatement(this SyntaxNode node)
        {
            return node.TypeSwitch(
                (DoStatementSyntax n) => n.Statement,
                (ElseClauseSyntax n) => n.Statement,
                (FixedStatementSyntax n) => n.Statement,
                (ForEachStatementSyntax n) => n.Statement,
                (ForStatementSyntax n) => n.Statement,
                (IfStatementSyntax n) => n.Statement,
                (LabeledStatementSyntax n) => n.Statement,
                (LockStatementSyntax n) => n.Statement,
                (UsingStatementSyntax n) => n.Statement,
                (WhileStatementSyntax n) => n.Statement,
                (SyntaxNode n) => null);
        }

        public static SyntaxTokenList GetModifiers(this SyntaxNode member)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.EnumDeclaration:
                        return ((EnumDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                        return ((TypeDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).Modifiers;
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                        return ((AccessorDeclarationSyntax)member).Modifiers;
                }
            }

            return default(SyntaxTokenList);
        }

        public static SyntaxNode WithModifiers(this SyntaxNode member, SyntaxTokenList modifiers)
        {
            if (member != null)
            {
                switch (member.Kind())
                {
                    case SyntaxKind.EnumDeclaration:
                        return ((EnumDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.StructDeclaration:
                        return ((TypeDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.DelegateDeclaration:
                        return ((DelegateDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.FieldDeclaration:
                        return ((FieldDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.EventFieldDeclaration:
                        return ((EventFieldDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.ConstructorDeclaration:
                        return ((ConstructorDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.DestructorDeclaration:
                        return ((DestructorDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.PropertyDeclaration:
                        return ((PropertyDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.EventDeclaration:
                        return ((EventDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.IndexerDeclaration:
                        return ((IndexerDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.OperatorDeclaration:
                        return ((OperatorDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.ConversionOperatorDeclaration:
                        return ((ConversionOperatorDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.MethodDeclaration:
                        return ((MethodDeclarationSyntax)member).WithModifiers(modifiers);
                    case SyntaxKind.GetAccessorDeclaration:
                    case SyntaxKind.SetAccessorDeclaration:
                    case SyntaxKind.AddAccessorDeclaration:
                    case SyntaxKind.RemoveAccessorDeclaration:
                        return ((AccessorDeclarationSyntax)member).WithModifiers(modifiers);
                }
            }

            return null;
        }

        public static bool CheckTopLevel(this SyntaxNode node, TextSpan span)
        {
            var block = node as BlockSyntax;
            if (block != null)
            {
                return block.ContainsInBlockBody(span);
            }

            var expressionBodiedMember = node as ArrowExpressionClauseSyntax;
            if (expressionBodiedMember != null)
            {
                return expressionBodiedMember.ContainsInExpressionBodiedMemberBody(span);
            }

            var field = node as FieldDeclarationSyntax;
            if (field != null)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    if (variable.Initializer != null && variable.Initializer.Span.Contains(span))
                    {
                        return true;
                    }
                }
            }

            var global = node as GlobalStatementSyntax;
            if (global != null)
            {
                return true;
            }

            var constructorInitializer = node as ConstructorInitializerSyntax;
            if (constructorInitializer != null)
            {
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
            var compilation = node as CompilationUnitSyntax;
            if (compilation != null)
            {
                return compilation.Members;
            }

            var @namespace = node as NamespaceDeclarationSyntax;
            if (@namespace != null)
            {
                return @namespace.Members;
            }

            var type = node as TypeDeclarationSyntax;
            if (type != null)
            {
                return type.Members;
            }

            var @enum = node as EnumDeclarationSyntax;
            if (@enum != null)
            {
                return @enum.Members;
            }

            return SpecializedCollections.EmptyEnumerable<MemberDeclarationSyntax>();
        }

        public static IEnumerable<SyntaxNode> GetBodies(this SyntaxNode node)
        {
            var constructor = node as ConstructorDeclarationSyntax;
            if (constructor != null)
            {
                var result = SpecializedCollections.SingletonEnumerable<SyntaxNode>(constructor.Body).WhereNotNull();
                var initializer = constructor.Initializer;
                if (initializer != null)
                {
                    result = result.Concat(initializer.ArgumentList.Arguments.Select(a => (SyntaxNode)a.Expression).WhereNotNull());
                }

                return result;
            }

            var method = node as BaseMethodDeclarationSyntax;
            if (method != null)
            {
                return SpecializedCollections.SingletonEnumerable<SyntaxNode>(method.Body).WhereNotNull();
            }

            var property = node as BasePropertyDeclarationSyntax;
            if (property != null && property.AccessorList != null)
            {
                return property.AccessorList.Accessors.Select(a => a.Body).WhereNotNull();
            }

            var @enum = node as EnumMemberDeclarationSyntax;
            if (@enum != null)
            {
                if (@enum.EqualsValue != null)
                {
                    return SpecializedCollections.SingletonEnumerable(@enum.EqualsValue.Value).WhereNotNull();
                }
            }

            var field = node as BaseFieldDeclarationSyntax;
            if (field != null)
            {
                return field.Declaration.Variables.Where(v => v.Initializer != null).Select(v => v.Initializer.Value).WhereNotNull();
            }

            return SpecializedCollections.EmptyEnumerable<SyntaxNode>();
        }

        public static ConditionalAccessExpressionSyntax GetParentConditionalAccessExpression(this SyntaxNode node)
        {
            var parent = node.Parent;
            while (parent != null)
            {
                // Because the syntax for conditional access is right associate, we cannot
                // simply take the first ancestor ConditionalAccessExpression. Instead, we 
                // must walk upward until we find the ConditionalAccessExpression whose
                // OperatorToken appears left of the MemberBinding.
                if (parent.IsKind(SyntaxKind.ConditionalAccessExpression) &&
                    ((ConditionalAccessExpressionSyntax)parent).OperatorToken.Span.End <= node.SpanStart)
                {
                    return (ConditionalAccessExpressionSyntax)parent;
                }

                parent = parent.Parent;
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
    }
}
