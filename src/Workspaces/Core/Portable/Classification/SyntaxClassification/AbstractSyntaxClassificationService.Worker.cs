// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
            private readonly Workspace _workspace;
            private readonly SemanticModel _semanticModel;
            private readonly SyntaxTree _syntaxTree;
            private readonly TextSpan _textSpan;
            private readonly ArrayBuilder<ClassifiedSpan> _list;
            private readonly CancellationToken _cancellationToken;
            private readonly Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> _getNodeClassifiers;
            private readonly Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> _getTokenClassifiers;
            private readonly HashSet<ClassifiedSpan> _set;
            private readonly Stack<SyntaxNodeOrToken> _pendingNodes;

            private Worker(
                Workspace workspace,
                SemanticModel semanticModel,
                TextSpan textSpan,
                ArrayBuilder<ClassifiedSpan> list,
                Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
                Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
                CancellationToken cancellationToken)
            {
                _workspace = workspace;
                _getNodeClassifiers = getNodeClassifiers;
                _getTokenClassifiers = getTokenClassifiers;
                _semanticModel = semanticModel;
                _syntaxTree = semanticModel.SyntaxTree;
                _textSpan = textSpan;
                _list = list;
                _cancellationToken = cancellationToken;

                // get one from pool
                _set = SharedPools.Default<HashSet<ClassifiedSpan>>().AllocateAndClear();
                _pendingNodes = SharedPools.Default<Stack<SyntaxNodeOrToken>>().AllocateAndClear();
            }

            internal static void Classify(
                Workspace workspace,
                SemanticModel semanticModel,
                TextSpan textSpan,
                ArrayBuilder<ClassifiedSpan> list,
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
                    SharedPools.Default<HashSet<ClassifiedSpan>>().ClearAndFree(worker._set);
                    SharedPools.Default<Stack<SyntaxNodeOrToken>>().ClearAndFree(worker._pendingNodes);
                }
            }

            private void AddClassification(TextSpan textSpan, string type)
            {
                if (textSpan.Length > 0 && textSpan.OverlapsWith(_textSpan))
                {
                    var tuple = new ClassifiedSpan(type, textSpan);
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

                    var result = ArrayBuilder<ClassifiedSpan>.GetInstance();
                    classifier.AddClassifications(_workspace, syntax, _semanticModel, result, _cancellationToken);
                    AddClassifications(result);
                    result.Free();
                }
            }

            private void AddClassifications(ArrayBuilder<ClassifiedSpan> classifications)
            {
                if (classifications != null)
                {
                    foreach (var classification in classifications)
                    {
                        AddClassification(classification);
                    }
                }
            }

            private void AddClassification(ClassifiedSpan classification)
            {
                if (classification.ClassificationType != null)
                {
                    AddClassification(classification.TextSpan, classification.ClassificationType);
                }
            }

            private void ClassifyToken(SyntaxToken syntax)
            {
                ClassifyStructuredTrivia(syntax.LeadingTrivia);

                foreach (var classifier in _getTokenClassifiers(syntax))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var result = ArrayBuilder<ClassifiedSpan>.GetInstance();
                    classifier.AddClassifications(_workspace, syntax, _semanticModel, result, _cancellationToken);
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
