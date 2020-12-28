﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class SelectionValidator
    {
        protected static readonly SelectionResult NullSelection = new NullSelectionResult();

        protected readonly SemanticDocument SemanticDocument;
        protected readonly TextSpan OriginalSpan;
        protected readonly OptionSet Options;

        protected SelectionValidator(
            SemanticDocument document,
            TextSpan textSpan,
            OptionSet options)
        {
            Contract.ThrowIfNull(document);

            SemanticDocument = document;
            OriginalSpan = textSpan;
            Options = options;
        }

        public bool ContainsValidSelection
        {
            get
            {
                return !OriginalSpan.IsEmpty;
            }
        }

        public abstract Task<SelectionResult> GetValidSelectionAsync(CancellationToken cancellationToken);
        public abstract IEnumerable<SyntaxNode> GetOuterReturnStatements(SyntaxNode commonRoot, IEnumerable<SyntaxNode> jumpsOutOfRegion);
        public abstract bool IsFinalSpanSemanticallyValidSpan(SyntaxNode node, TextSpan textSpan, IEnumerable<SyntaxNode> returnStatements, CancellationToken cancellationToken);
        public abstract bool ContainsNonReturnExitPointsStatements(IEnumerable<SyntaxNode> jumpsOutOfRegion);

        protected bool IsFinalSpanSemanticallyValidSpan(
            SemanticModel semanticModel, TextSpan textSpan, Tuple<SyntaxNode, SyntaxNode> range, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(range);

            var controlFlowAnalysisData = semanticModel.AnalyzeControlFlow(range.Item1, range.Item2);

            // there must be no control in and out of given span
            if (controlFlowAnalysisData.EntryPoints.Any())
            {
                return false;
            }

            // check something like continue, break, yield break, yield return, and etc
            if (ContainsNonReturnExitPointsStatements(controlFlowAnalysisData.ExitPoints))
            {
                return false;
            }

            // okay, there is no branch out, check whether next statement can be executed normally
            var returnStatements = GetOuterReturnStatements(range.Item1.GetCommonRoot(range.Item2), controlFlowAnalysisData.ExitPoints);
            if (!returnStatements.Any())
            {
                if (!controlFlowAnalysisData.EndPointIsReachable)
                {
                    // REVIEW: should we just do extract method regardless or show some warning to user?
                    // in dev10, looks like we went ahead and did the extract method even if selection contains
                    // unreachable code.
                }

                return true;
            }

            // okay, only branch was return. make sure we have all return in the selection.

            // check for special case, if end point is not reachable, we don't care the selection
            // actually contains all return statements. we just let extract method go through
            // and work like we did in dev10
            if (!controlFlowAnalysisData.EndPointIsReachable)
            {
                return true;
            }

            // there is a return statement, and current position is reachable. let's check whether this is a case where that is okay
            return IsFinalSpanSemanticallyValidSpan(semanticModel.SyntaxTree.GetRoot(cancellationToken), textSpan, returnStatements, cancellationToken);
        }

        protected static Tuple<SyntaxNode, SyntaxNode> GetStatementRangeContainingSpan<T>(
            SyntaxNode root, TextSpan textSpan, CancellationToken cancellationToken) where T : SyntaxNode
        {
            // use top-down approach to find smallest statement range that contains given span.
            // this approach is more expansive than bottom-up approach I used before but way simpler and easy to understand
            var token1 = root.FindToken(textSpan.Start);
            var token2 = root.FindTokenFromEnd(textSpan.End);

            var commonRoot = token1.GetCommonRoot(token2).GetAncestorOrThis<T>() ?? root;

            var firstStatement = (T)null;
            var lastStatement = (T)null;

            var spine = new List<T>();

            foreach (var stmt in commonRoot.DescendantNodesAndSelf().OfType<T>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // quick skip check.
                // - not containing at all
                if (stmt.Span.End < textSpan.Start)
                {
                    continue;
                }

                // quick exit check
                // - passed candidate statements
                if (textSpan.End < stmt.SpanStart)
                {
                    break;
                }

                if (stmt.SpanStart <= textSpan.Start)
                {
                    // keep track spine
                    spine.Add(stmt);
                }

                if (textSpan.End <= stmt.Span.End && spine.Any(s => s.Parent == stmt.Parent))
                {
                    // malformed code or selection can make spine to have more than an elements
                    firstStatement = spine.First(s => s.Parent == stmt.Parent);
                    lastStatement = stmt;

                    spine.Clear();
                }
            }

            if (firstStatement == null || lastStatement == null)
            {
                return null;
            }

            return new Tuple<SyntaxNode, SyntaxNode>(firstStatement, lastStatement);
        }

        protected static Tuple<SyntaxNode, SyntaxNode> GetStatementRangeContainedInSpan<T>(
            SyntaxNode root, TextSpan textSpan, CancellationToken cancellationToken) where T : SyntaxNode
        {
            // use top-down approach to find largest statement range contained in the given span
            // this method is a bit more expensive than bottom-up approach, but way more simpler than the other approach.
            var token1 = root.FindToken(textSpan.Start);
            var token2 = root.FindTokenFromEnd(textSpan.End);

            var commonRoot = token1.GetCommonRoot(token2).GetAncestorOrThis<T>() ?? root;

            T firstStatement = null;
            T lastStatement = null;

            foreach (var stmt in commonRoot.DescendantNodesAndSelf().OfType<T>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (firstStatement == null && stmt.SpanStart >= textSpan.Start)
                {
                    firstStatement = stmt;
                }

                if (firstStatement != null && stmt.Span.End <= textSpan.End && stmt.Parent == firstStatement.Parent)
                {
                    lastStatement = stmt;
                }
            }

            if (firstStatement == null || lastStatement == null)
            {
                return null;
            }

            return new Tuple<SyntaxNode, SyntaxNode>(firstStatement, lastStatement);
        }
    }
}
