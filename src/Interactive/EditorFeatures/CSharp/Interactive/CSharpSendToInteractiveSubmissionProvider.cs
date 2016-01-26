// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Interactive;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Interactive
{
    [Export(typeof(ISendToInteractiveSubmissionProvider))]
    internal sealed class CSharpSendToInteractiveSubmissionProvider
        : SendToInteractiveSubmissionProvider
    {
        protected override bool CanParseSubmission(string code)
        {
            SourceText sourceCode = SourceText.From(code);
            ParseOptions options = CSharpParseOptions.Default.WithKind(SourceCodeKind.Script);
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(sourceCode, options);
            if (tree == null)
            {
                return false;
            }

            return tree.HasCompilationUnitRoot && !tree.GetDiagnostics().Any();
        }

        protected override IEnumerable<TextSpan> GetExecutableSyntaxTreeNodeSelection(TextSpan selectionSpan, SourceText source, SyntaxNode root, SemanticModel model)
        {
            SyntaxNode expandedNode = GetExecutableSyntaxTreeNode(selectionSpan, source, root, model);
            return expandedNode != null
                ? new TextSpan[] { expandedNode.Span }
                : Array.Empty<TextSpan>();
        }

        private SyntaxNode GetExecutableSyntaxTreeNode(TextSpan selectionSpan, SourceText source, SyntaxNode root, SemanticModel model)
        {
            Tuple<SyntaxToken, SyntaxToken> tokens = GetSelectedTokens(selectionSpan, root);
            var startToken = tokens.Item1;
            var endToken = tokens.Item2;
            if (startToken != endToken && startToken.Span.End > endToken.SpanStart)
            {
                return null;
            }

            // If a selection falls within a single executable statement then execute that statement.
            var startNode = GetGlobalExecutableStatement(startToken);
            var endNode = GetGlobalExecutableStatement(endToken);
            if (startNode == null || endNode == null)
            {
                return null;
            }

            // If one of the nodes is an ancestor of another node return that node.
            if (startNode.Span.Contains(endNode.Span))
            {
                return startNode;
            }
            else if (endNode.Span.Contains(startNode.Span))
            {
                return endNode;
            }

            // Selection spans multiple statements.
            // In this case find common parent and find a span of statements within that parent.
            var commonNode = root.FindNode(TextSpan.FromBounds(startNode.Span.Start, endNode.Span.End));
            return commonNode;
        }

        private static SyntaxNode GetGlobalExecutableStatement(SyntaxToken token)
        {
            return GetGlobalExecutableStatement(token.Parent);
        }

        private static SyntaxNode GetGlobalExecutableStatement(SyntaxNode node)
        {
            SyntaxNode candidate = node.GetAncestorOrThis<StatementSyntax>();
            if (candidate != null)
            {
                return candidate;
            }

            candidate = node.GetAncestorsOrThis<SyntaxNode>()
                .Where(n => IsGlobalExecutableStatement(n)).FirstOrDefault();
            if (candidate != null)
            {
                return candidate;
            }

            return null;
        }

        private static bool IsGlobalExecutableStatement(SyntaxNode node)
        {
            var kind = node.Kind();
            return SyntaxFacts.IsTypeDeclaration(kind)
                || SyntaxFacts.IsGlobalMemberDeclaration(kind)
                || node.IsKind(SyntaxKind.UsingDirective);
        }

        private Tuple<SyntaxToken, SyntaxToken> GetSelectedTokens(TextSpan selectionSpan, SyntaxNode root)
        {
            if (selectionSpan.Length == 0)
            {
                var selectedToken = root.FindTokenOnLeftOfPosition(selectionSpan.End);
                return Tuple.Create(
                    selectedToken,
                    selectedToken);
            }
            else
            {
                // For a selection find the first and the last token of the selection.
                // Ensure that the first token comes before the last token.
                return Tuple.Create(
                    root.FindTokenOnRightOfPosition(selectionSpan.Start),
                    root.FindTokenOnLeftOfPosition(selectionSpan.End));
            }
        }
    }
}
