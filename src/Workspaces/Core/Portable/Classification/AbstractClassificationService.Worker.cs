// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Classification
{
    internal partial class AbstractClassificationService
    {
        private struct Worker
        {
            private readonly SemanticModel semanticModel;
            private readonly SyntaxTree syntaxTree;
            private readonly TextSpan textSpan;
            private readonly List<ClassifiedSpan> list;
            private readonly CancellationToken cancellationToken;
            private readonly Func<SyntaxNode, List<ISyntaxClassifier>> getNodeClassifiers;
            private readonly Func<SyntaxToken, List<ISyntaxClassifier>> getTokenClassifiers;
            private readonly HashSet<ClassifiedSpan> set;
            private readonly Stack<SyntaxNodeOrToken> pendingNodes;

            private Worker(
                Workspace workspace,
                SemanticModel semanticModel,
                TextSpan textSpan,
                List<ClassifiedSpan> list,
                Func<SyntaxNode, List<ISyntaxClassifier>> getNodeClassifiers,
                Func<SyntaxToken, List<ISyntaxClassifier>> getTokenClassifiers,
                CancellationToken cancellationToken)
            {
                this.getNodeClassifiers = getNodeClassifiers;
                this.getTokenClassifiers = getTokenClassifiers;
                this.semanticModel = semanticModel;
                this.syntaxTree = semanticModel.SyntaxTree;
                this.textSpan = textSpan;
                this.list = list;
                this.cancellationToken = cancellationToken;

                // get one from pool
                this.set = SharedPools.Default<HashSet<ClassifiedSpan>>().AllocateAndClear();
                this.pendingNodes = SharedPools.Default<Stack<SyntaxNodeOrToken>>().AllocateAndClear();
            }

            internal static void Classify(
                Workspace workspace,
                SemanticModel semanticModel,
                TextSpan textSpan,
                List<ClassifiedSpan> list,
                Func<SyntaxNode, List<ISyntaxClassifier>> getNodeClassifiers,
                Func<SyntaxToken, List<ISyntaxClassifier>> getTokenClassifiers,
                CancellationToken cancellationToken)
            {
                var worker = new Worker(workspace, semanticModel, textSpan, list, getNodeClassifiers, getTokenClassifiers, cancellationToken);

                try
                {
                    worker.pendingNodes.Push(worker.syntaxTree.GetRoot(cancellationToken));
                    worker.ProcessNodes();
                }
                finally
                {
                    // release collections to the pool
                    SharedPools.Default<HashSet<ClassifiedSpan>>().ClearAndFree(worker.set);
                    SharedPools.Default<Stack<SyntaxNodeOrToken>>().ClearAndFree(worker.pendingNodes);
                }
            }

            private void AddClassification(TextSpan textSpan, string type)
            {
                var tuple = new ClassifiedSpan(type, textSpan);
                if (!this.set.Contains(tuple))
                {
                    this.list.Add(tuple);
                    this.set.Add(tuple);
                }
            }

            private void ProcessNodes()
            {
                while (pendingNodes.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var nodeOrToken = pendingNodes.Pop();

                    if (nodeOrToken.Span.IntersectsWith(textSpan))
                    {
                        ClassifyNodeOrToken(nodeOrToken);

                        foreach (var child in nodeOrToken.ChildNodesAndTokens())
                        {
                            pendingNodes.Push(child);
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
                foreach (var classifier in getNodeClassifiers(syntax))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var classifications = classifier.ClassifyNode(syntax, this.semanticModel, cancellationToken);
                    AddClassifications(classifications);
                }
            }

            private void AddClassifications(IEnumerable<ClassifiedSpan> classifications)
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

                foreach (var classifier in getTokenClassifiers(syntax))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var classifications = classifier.ClassifyToken(syntax, this.semanticModel, cancellationToken);
                    AddClassifications(classifications);
                }

                ClassifyStructuredTrivia(syntax.TrailingTrivia);
            }

            private void ClassifyStructuredTrivia(SyntaxTriviaList triviaList)
            {
                foreach (var trivia in triviaList)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (trivia.HasStructure)
                    {
                        pendingNodes.Push(trivia.GetStructure());
                    }
                }
            }
        }
    }
}