// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    internal partial class AbstractSyntaxClassificationService
    {
        private struct Worker
        {
            private readonly SemanticModel _semanticModel;
            private readonly SyntaxTree _syntaxTree;
            private readonly TextSpan _textSpan;
            private readonly ArrayBuilder<ClassifiedSpanSlim> _list;
            private readonly CancellationToken _cancellationToken;
            private readonly Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> _getNodeClassifiers;
            private readonly Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> _getTokenClassifiers;
            private readonly HashSet<ClassifiedSpanSlim> _set;
            private readonly Stack<SyntaxNodeOrToken> _pendingNodes;

            private Worker(
                Workspace workspace,
                SemanticModel semanticModel,
                TextSpan textSpan,
                ArrayBuilder<ClassifiedSpanSlim> list,
                Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
                Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
                CancellationToken cancellationToken)
            {
                _getNodeClassifiers = getNodeClassifiers;
                _getTokenClassifiers = getTokenClassifiers;
                _semanticModel = semanticModel;
                _syntaxTree = semanticModel.SyntaxTree;
                _textSpan = textSpan;
                _list = list;
                _cancellationToken = cancellationToken;

                // get one from pool
                _set = SharedPools.Default<HashSet<ClassifiedSpanSlim>>().AllocateAndClear();
                _pendingNodes = SharedPools.Default<Stack<SyntaxNodeOrToken>>().AllocateAndClear();
            }

            internal static void Classify(
                Workspace workspace,
                SemanticModel semanticModel,
                TextSpan textSpan,
                ArrayBuilder<ClassifiedSpanSlim> list,
                Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
                Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
                CancellationToken cancellationToken)
            {
                var worker = new Worker(workspace, semanticModel, textSpan, list, getNodeClassifiers, getTokenClassifiers, cancellationToken);

                try
                {
                    worker._pendingNodes.Push(worker._syntaxTree.GetRoot(cancellationToken));
                    worker.ProcessNodes();
                }
                finally
                {
                    // release collections to the pool
                    SharedPools.Default<HashSet<ClassifiedSpanSlim>>().ClearAndFree(worker._set);
                    SharedPools.Default<Stack<SyntaxNodeOrToken>>().ClearAndFree(worker._pendingNodes);
                }
            }

            private void AddClassification(TextSpan textSpan, ClassificationTypeKind? type)
            {
                if (type != null)
                {
                    AddClassification(textSpan, type.Value);
                }
            }

            private void AddClassification(TextSpan textSpan, ClassificationTypeKind type)
            {
                if (textSpan.Length > 0 && textSpan.OverlapsWith(_textSpan))
                {
                    var tuple = new ClassifiedSpanSlim(type, textSpan);
                    if (!_set.Contains(tuple))
                    {
                        _list.Add(tuple);
                        _set.Add(tuple);
                    }
                }
            }

            private void ProcessNodes()
            {
                while (_pendingNodes.Count > 0)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    var nodeOrToken = _pendingNodes.Pop();

                    if (nodeOrToken.FullSpan.IntersectsWith(_textSpan))
                    {
                        ClassifyNodeOrToken(nodeOrToken);

                        foreach (var child in nodeOrToken.ChildNodesAndTokens())
                        {
                            _pendingNodes.Push(child);
                        }
                    }
                }
            }

            private void ClassifyNodeOrToken(SyntaxNodeOrToken nodeOrToken)
            {
                var node = nodeOrToken.AsNode();
                if (node != null)
                {
                    ClassifyNode(node);
                }
                else
                {
                    ClassifyToken(nodeOrToken.AsToken());
                }
            }

            private void ClassifyNode(SyntaxNode syntax)
            {
                foreach (var classifier in _getNodeClassifiers(syntax))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var result = ArrayBuilder<ClassifiedSpanSlim>.GetInstance();
                    classifier.AddClassifications(syntax, _semanticModel, result, _cancellationToken);
                    AddClassifications(result);
                    result.Free();
                }
            }

            private void AddClassifications(ArrayBuilder<ClassifiedSpanSlim> classifications)
            {
                if (classifications != null)
                {
                    foreach (var classification in classifications)
                    {
                        AddClassification(classification);
                    }
                }
            }

            private void AddClassification(ClassifiedSpanSlim classifiedSpan)
            {
                AddClassification(classifiedSpan.TextSpan, classifiedSpan.Kind);
            }

            private void ClassifyToken(SyntaxToken syntax)
            {
                ClassifyStructuredTrivia(syntax.LeadingTrivia);

                foreach (var classifier in _getTokenClassifiers(syntax))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var result = ArrayBuilder<ClassifiedSpanSlim>.GetInstance();
                    classifier.AddClassifications(syntax, _semanticModel, result, _cancellationToken);
                    AddClassifications(result);
                    result.Free();
                }

                ClassifyStructuredTrivia(syntax.TrailingTrivia);
            }

            private void ClassifyStructuredTrivia(SyntaxTriviaList triviaList)
            {
                foreach (var trivia in triviaList)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    if (trivia.HasStructure)
                    {
                        _pendingNodes.Push(trivia.GetStructure());
                    }
                }
            }
        }
    }
}
