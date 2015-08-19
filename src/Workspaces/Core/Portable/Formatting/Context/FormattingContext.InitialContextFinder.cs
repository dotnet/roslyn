// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class FormattingContext
    {
        private class InitialContextFinder
        {
            private readonly TokenStream _tokenStream;
            private readonly ChainedFormattingRules _formattingRules;
            private readonly SyntaxNode _rootNode;
            private readonly SyntaxToken _lastToken;

            public InitialContextFinder(
                TokenStream tokenStream,
                ChainedFormattingRules formattingRules,
                SyntaxNode rootNode,
                SyntaxToken lastToken)
            {
                Contract.ThrowIfNull(tokenStream);
                Contract.ThrowIfNull(formattingRules);
                Contract.ThrowIfNull(rootNode);

                _tokenStream = tokenStream;
                _formattingRules = formattingRules;
                _rootNode = rootNode;
                _lastToken = lastToken;
            }

            public ValueTuple<List<IndentBlockOperation>, List<SuppressOperation>> Do(SyntaxToken startToken, SyntaxToken endToken)
            {
                // we are formatting part of document, try to find initial context that formatting will be based on such as
                // initial indentation and etc.
                using (Logger.LogBlock(FunctionId.Formatting_ContextInitialization, CancellationToken.None))
                {
                    // first try to set initial indentation information
                    var initialIndentationOperations = this.GetInitialIndentBlockOperations(startToken, endToken);

                    // second try to set suppress wrapping regions
                    var initialSuppressOperations = GetInitialSuppressOperations(startToken, endToken);
                    if (initialSuppressOperations != null)
                    {
                        Debug.Assert(
                            initialSuppressOperations.IsEmpty() ||
                            initialSuppressOperations.All(
                                o => o.TextSpan.Contains(startToken.SpanStart) ||
                                     o.TextSpan.Contains(endToken.SpanStart)));
                    }

                    return ValueTuple.Create(initialIndentationOperations, initialSuppressOperations);
                }
            }

            private List<IndentBlockOperation> GetInitialIndentBlockOperations(SyntaxToken startToken, SyntaxToken endToken)
            {
                var span = TextSpan.FromBounds(startToken.SpanStart, endToken.Span.End);
                var node = startToken.GetCommonRoot(endToken).GetParentWithBiggerSpan();
                var previous = default(SyntaxNode);

                // starting from the common node, move up to the parent
                var operations = new List<IndentBlockOperation>();
                var list = new List<IndentBlockOperation>();
                while (node != null)
                {
                    // get all operations for the nodes that contains the formatting span, but not ones contained by the span
                    node.DescendantNodesAndSelf(n => n != previous && n.Span.IntersectsWith(span) && !span.Contains(n.Span))
                        .Do(n =>
                            {
                                _formattingRules.AddIndentBlockOperations(list, n, _lastToken);
                                foreach (var element in list)
                                {
                                    if (element != null)
                                    {
                                        operations.Add(element);
                                    }
                                }

                                list.Clear();
                            });

                    // found some. use these as initial indentation
                    if (operations.Any(o => o.TextSpan.Contains(span)))
                    {
                        break;
                    }

                    previous = node;
                    node = node.Parent;
                }

                // make sure operations we have has effects over the formatting span
                operations.RemoveAll(o => o == null || !o.TextSpan.IntersectsWith(span));

                // we couldn't find anything
                // return initial location so that we can get base indentation correctly
                if (operations.Count == 0)
                {
                    operations.Add(new IndentBlockOperation(
                        startToken: _rootNode.GetFirstToken(includeZeroWidth: true),
                        endToken: _rootNode.GetLastToken(includeZeroWidth: true),
                        textSpan: _rootNode.FullSpan,
                        indentationDelta: 0,
                        option: IndentBlockOption.AbsolutePosition));

                    return operations;
                }

                operations.Sort(CommonFormattingHelpers.IndentBlockOperationComparer);
                return operations;
            }

            private List<SuppressOperation> GetInitialSuppressOperations(SyntaxToken startToken, SyntaxToken endToken)
            {
                var noWrapList = this.GetInitialSuppressOperations(startToken, endToken, SuppressOption.NoWrapping);
                var noSpaceList = this.GetInitialSuppressOperations(startToken, endToken, SuppressOption.NoSpacing);

                var list = noWrapList.Combine(noSpaceList);
                if (list == null)
                {
                    return null;
                }

                list.Sort(CommonFormattingHelpers.SuppressOperationComparer);
                return list;
            }

            private List<SuppressOperation> GetInitialSuppressOperations(SyntaxToken startToken, SyntaxToken endToken, SuppressOption mask)
            {
                var startList = this.GetInitialSuppressOperations(startToken, mask);
                var endList = this.GetInitialSuppressOperations(endToken, mask);

                return startList.Combine(endList);
            }

            private List<SuppressOperation> GetInitialSuppressOperations(SyntaxToken token, SuppressOption mask)
            {
                var startNode = token.Parent;
                var startPosition = token.SpanStart;

                // starting from given token, move up to root until the first meaningful
                // operation has found
                var list = new List<SuppressOperation>();

                Predicate<SuppressOperation> predicate = o =>
                {
                    if (o == null)
                    {
                        return true;
                    }

                    if (o.ContainsElasticTrivia(_tokenStream) && !o.Option.IsOn(SuppressOption.IgnoreElastic))
                    {
                        return true;
                    }

                    if (!o.TextSpan.Contains(startPosition))
                    {
                        return true;
                    }

                    if (!o.Option.IsMaskOn(mask))
                    {
                        return true;
                    }

                    return false;
                };

                var currentIndentationNode = startNode;
                while (currentIndentationNode != null)
                {
                    _formattingRules.AddSuppressOperations(list, currentIndentationNode, _lastToken);

                    list.RemoveAll(predicate);
                    if (list.Count > 0)
                    {
                        return list;
                    }

                    currentIndentationNode = currentIndentationNode.Parent;
                }

                return null;
            }
        }
    }
}
