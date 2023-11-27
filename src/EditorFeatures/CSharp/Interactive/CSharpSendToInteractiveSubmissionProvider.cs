// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Interactive;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Interactive
{
    [Export(typeof(ISendToInteractiveSubmissionProvider))]
    internal sealed class CSharpSendToInteractiveSubmissionProvider
        : AbstractSendToInteractiveSubmissionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpSendToInteractiveSubmissionProvider()
        {
        }

        protected override bool CanParseSubmission(string code)
        {
            var options = CSharpInteractiveEvaluatorLanguageInfoProvider.Instance.ParseOptions;
            var tree = SyntaxFactory.ParseSyntaxTree(SourceText.From(code, encoding: null, SourceHashAlgorithms.Default), options);
            return tree.HasCompilationUnitRoot &&
                !tree.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        protected override IEnumerable<TextSpan> GetExecutableSyntaxTreeNodeSelection(TextSpan selectionSpan, SyntaxNode root)
        {
            var expandedNode = GetSyntaxNodeForSubmission(selectionSpan, root);
            return expandedNode != null
                ? [expandedNode.Span]
                : Array.Empty<TextSpan>();
        }

        /// <summary>
        /// Finds a <see cref="SyntaxNode"/> that should be submitted to REPL.
        /// </summary>
        /// <param name="selectionSpan">Selection that user has originally made.</param>
        /// <param name="root">Root of the syntax tree.</param>
        private static SyntaxNode? GetSyntaxNodeForSubmission(TextSpan selectionSpan, SyntaxNode root)
        {
            GetSelectedTokens(selectionSpan, root, out var startToken, out var endToken);

            // Ensure that the first token comes before the last token.
            // Otherwise selection did not contain any tokens.
            if (startToken != endToken && startToken.Span.End > endToken.SpanStart)
                return null;

            if (startToken == endToken)
            {
                return GetSyntaxNodeForSubmission(startToken.GetRequiredParent());
            }

            var startNode = GetSyntaxNodeForSubmission(startToken.GetRequiredParent());
            var endNode = GetSyntaxNodeForSubmission(endToken.GetRequiredParent());

            // If there is no SyntaxNode worth sending to the REPL return null.
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
            return GetSyntaxNodeForSubmission(startNode.GetCommonRoot(endNode));
        }

        /// <summary>
        /// Finds a <see cref="SyntaxNode"/> that should be submitted to REPL.
        /// </summary>
        /// <param name="node">The currently selected node.</param>
        private static SyntaxNode? GetSyntaxNodeForSubmission(SyntaxNode node)
        {
            SyntaxNode? candidate = node.GetAncestorOrThis<StatementSyntax>();
            if (candidate != null)
            {
                return candidate;
            }

            candidate = node.GetAncestorsOrThis<SyntaxNode>()
                .Where(IsSubmissionNode).FirstOrDefault();
            if (candidate != null)
            {
                return candidate;
            }

            return null;
        }

        /// <summary>Returns <c>true</c> if <c>node</c> could be treated as a REPL submission.</summary>
        private static bool IsSubmissionNode(SyntaxNode node)
        {
            var kind = node.Kind();
            return SyntaxFacts.IsTypeDeclaration(kind)
                || SyntaxFacts.IsGlobalMemberDeclaration(kind)
                || node.IsKind(SyntaxKind.UsingDirective);
        }

        private static void GetSelectedTokens(
            TextSpan selectionSpan,
            SyntaxNode root,
            out SyntaxToken startToken,
            out SyntaxToken endToken)
        {
            endToken = root.FindTokenOnLeftOfPosition(selectionSpan.End);
            startToken = selectionSpan.Length == 0
                ? endToken
                : root.FindTokenOnRightOfPosition(selectionSpan.Start);
        }
    }
}
