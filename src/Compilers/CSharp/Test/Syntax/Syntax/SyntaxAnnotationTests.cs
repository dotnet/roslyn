// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxAnnotationTests
    {
        #region Boundary Tests

        [Fact]
        public void TestEmpty()
        {
            var code = @"";
            var tree = SyntaxFactory.ParseSyntaxTree(code);

            TestAnnotation(tree);
        }

        [Fact]
        public void TestAddAnnotationToNullSyntaxToken()
        {
            SyntaxAnnotation annotation = new SyntaxAnnotation();
            var oldToken = default(SyntaxToken);
            var newToken = oldToken.WithAdditionalAnnotations(annotation);

            Assert.False(newToken.ContainsAnnotations);
        }

        [Fact]
        public void TestAddAnnotationToNullSyntaxTrivia()
        {
            SyntaxAnnotation annotation = new SyntaxAnnotation();
            var oldTrivia = default(SyntaxTrivia);
            var newTrivia = oldTrivia.WithAdditionalAnnotations(annotation);

            Assert.False(newTrivia.ContainsAnnotations);
        }

        [Fact]
        public void TestCopyAnnotationToNullSyntaxNode()
        {
            var fromNode = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot();
            var toNode = default(SyntaxNode);
            var annotatedNode = fromNode.CopyAnnotationsTo(toNode);
            Assert.Null(annotatedNode);
        }

        [Fact]
        public void TestCopyAnnotationOfZeroLengthToSyntaxNode()
        {
            var fromNode = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot();
            var toNode = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot();
            var annotatedNode = fromNode.CopyAnnotationsTo(toNode);
            Assert.Equal(annotatedNode, toNode); // Reference Equal
        }

        [Fact]
        public void TestCopyAnnotationFromNullSyntaxToken()
        {
            var fromToken = default(SyntaxToken);
            var toToken = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot().DescendantTokens().First();
            var annotatedToken = fromToken.CopyAnnotationsTo(toToken);
            Assert.True(annotatedToken.IsEquivalentTo(toToken));
        }

        [Fact]
        public void TestCopyAnnotationToNullSyntaxToken()
        {
            var fromToken = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot().DescendantTokens().First();
            var toToken = default(SyntaxToken);
            var annotatedToken = fromToken.CopyAnnotationsTo(toToken);
            Assert.True(annotatedToken.IsEquivalentTo(default(SyntaxToken)));
        }

        [Fact]
        public void TestCopyAnnotationOfZeroLengthToSyntaxToken()
        {
            var fromToken = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot().DescendantTokens().First();
            var toToken = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot().DescendantTokens().First();
            var annotatedToken = fromToken.CopyAnnotationsTo(toToken);
            Assert.Equal(annotatedToken, toToken); // Reference Equal
        }

        [Fact]
        public void TestCopyAnnotationFromNullSyntaxTrivia()
        {
            var fromTrivia = default(SyntaxTrivia);
            var tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            var toTrivia = GetAllTrivia(tree.GetCompilationUnitRoot()).FirstOrDefault();
            var annotatedTrivia = fromTrivia.CopyAnnotationsTo(toTrivia);
            Assert.True(toTrivia.IsEquivalentTo(annotatedTrivia));
        }

        [Fact]
        public void TestCopyAnnotationToNullSyntaxTrivia()
        {
            var toTrivia = default(SyntaxTrivia);
            var tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            var fromTrivia = GetAllTrivia(tree.GetCompilationUnitRoot()).FirstOrDefault();
            var annotatedTrivia = fromTrivia.CopyAnnotationsTo(toTrivia);
            Assert.True(default(SyntaxTrivia).IsEquivalentTo(annotatedTrivia));
        }

        [Fact]
        public void TestCopyAnnotationOfZeroLengthToSyntaxTrivia()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            var fromTrivia = GetAllTrivia(tree.GetCompilationUnitRoot()).FirstOrDefault();
            var toTrivia = GetAllTrivia(tree.GetCompilationUnitRoot()).FirstOrDefault();
            var annotatedTrivia = fromTrivia.CopyAnnotationsTo(toTrivia);
            Assert.Equal(annotatedTrivia, toTrivia); // Reference Equal
        }

        #endregion

        #region Negative Tests

        [Fact]
        public void TestMissingAnnotationsOnNodesOrTokens()
        {
            SyntaxAnnotation annotation = new SyntaxAnnotation();
            var tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.ExperimentalParseOptions);

            var matchingNodesOrTokens = tree.GetCompilationUnitRoot().GetAnnotatedNodesAndTokens(annotation);
            Assert.Empty(matchingNodesOrTokens);
        }

        [Fact]
        public void TestMissingAnnotationsOnTrivia()
        {
            SyntaxAnnotation annotation = new SyntaxAnnotation();
            var tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.ExperimentalParseOptions);

            var matchingTrivia = tree.GetCompilationUnitRoot().GetAnnotatedTrivia(annotation);
            Assert.Empty(matchingTrivia);
        }

        #endregion

        #region Other Functional Tests

        [Fact]
        public void TestSimpleMultipleAnnotationsOnNode()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxAnnotation annotation1 = new SyntaxAnnotation();
            SyntaxAnnotation annotation2 = new SyntaxAnnotation();

            // Pick the first node from tree
            var node = GetAllNodesAndTokens(tree.GetCompilationUnitRoot()).First(t => t.IsNode).AsNode();

            // Annotate it
            var annotatedNode = node.WithAdditionalAnnotations(annotation1);
            var newRoot = tree.GetCompilationUnitRoot().ReplaceNode(node, annotatedNode);

            // Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, node);

            // Pick the annotated node from the new tree
            var node2 = newRoot.GetAnnotatedNodesAndTokens(annotation1).Single().AsNode();

            // Annotate it again
            var twiceAnnotatedNode = node2.WithAdditionalAnnotations(annotation2);
            var twiceAnnotatedRoot = newRoot.ReplaceNode(node2, twiceAnnotatedNode);

            // Verify the recent annotation
            TestAnnotation(annotation2, twiceAnnotatedRoot, node2);

            // Verify both annotations exist in the newTree
            TestAnnotation(annotation1, twiceAnnotatedRoot, node);
            TestAnnotation(annotation2, twiceAnnotatedRoot, node);
        }

        [Fact]
        public void TestSimpleMultipleAnnotationsOnToken()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxAnnotation annotation1 = new SyntaxAnnotation();
            SyntaxAnnotation annotation2 = new SyntaxAnnotation();

            // Pick the first node from tree
            var token = GetAllNodesAndTokens(tree.GetCompilationUnitRoot()).First(t => t.IsToken).AsToken();

            // Annotate it
            var annotatedToken = token.WithAdditionalAnnotations(annotation1);
            var newRoot = tree.GetCompilationUnitRoot().ReplaceToken(token, annotatedToken);

            // Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, token);

            // Pick the annotated node from the new tree
            var token2 = newRoot.GetAnnotatedNodesAndTokens(annotation1).Single().AsToken();

            // Annotate it again
            var twiceAnnotatedToken = token2.WithAdditionalAnnotations(annotation2);
            var twiceAnnotatedRoot = newRoot.ReplaceToken(token2, twiceAnnotatedToken);

            // Verify the recent annotation
            TestAnnotation(annotation2, twiceAnnotatedRoot, token2);

            // Verify both annotations exist in the newTree
            TestAnnotation(annotation1, twiceAnnotatedRoot, token);
            TestAnnotation(annotation2, twiceAnnotatedRoot, token);
        }

        [Fact]
        public void TestSimpleMultipleAnnotationsOnTrivia()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxAnnotation annotation1 = new SyntaxAnnotation();
            SyntaxAnnotation annotation2 = new SyntaxAnnotation();

            // Pick the first node from tree
            var trivia = GetAllTrivia(tree.GetCompilationUnitRoot()).First();

            // Annotate it
            var annotatedTrivia = trivia.WithAdditionalAnnotations(annotation1);
            var newRoot = tree.GetCompilationUnitRoot().ReplaceTrivia(trivia, annotatedTrivia);

            // Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, trivia);

            // Pick the annotated node from the new tree
            var trivia2 = newRoot.GetAnnotatedTrivia(annotation1).Single();

            // Annotate it again
            var twiceAnnotatedTrivia = trivia2.WithAdditionalAnnotations(annotation2);
            var twiceAnnotatedRoot = newRoot.ReplaceTrivia(trivia2, twiceAnnotatedTrivia);

            // Verify the recent annotation
            TestAnnotation(annotation2, twiceAnnotatedRoot, trivia2);

            // Verify both annotations exist in the newTree
            TestAnnotation(annotation1, twiceAnnotatedRoot, trivia);
            TestAnnotation(annotation2, twiceAnnotatedRoot, trivia);
        }

        [Fact]
        public void TestMultipleAnnotationsOnAllNodesTokensAndTrivia()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxNode newRoot = tree.GetCompilationUnitRoot();

            var annotations = new List<SyntaxAnnotation>(Enumerable.Range(0, 3).Select(_ => new SyntaxAnnotation()));

            // add annotation one by one to every single node, token, trivia
            foreach (var annotation in annotations)
            {
                var rewriter = new InjectAnnotationRewriter(annotation);
                newRoot = rewriter.Visit(newRoot);
            }

            // Verify that all annotations are present in whichever places they were added
            TestMultipleAnnotationsInTree(tree.GetCompilationUnitRoot(), newRoot, annotations);
        }

        [Fact]
        public void TestAnnotationOnEveryNodeTokenTriviaOfHelloWorld()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);

            TestAnnotation(tree);
            TestTriviaAnnotation(tree);
        }

        [Fact]
        public void TestIfNodeHasAnnotations()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxAnnotation annotation1 = new SyntaxAnnotation();

            // Pick the first node from tree
            var firstNode = GetAllNodesAndTokens(tree.GetCompilationUnitRoot()).First(t => t.IsNode).AsNode();

            var children = firstNode.ChildNodesAndTokens();
            var lastChildOfFirstNode = children.Last(t => t.IsNode).AsNode();
            var annotatedNode = lastChildOfFirstNode.WithAdditionalAnnotations(annotation1);
            var newRoot = tree.GetCompilationUnitRoot().ReplaceNode(lastChildOfFirstNode, annotatedNode);

            // Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, lastChildOfFirstNode);

            // Pick the first node from new tree and see if any of its children is annotated
            var firstNodeInNewTree = GetAllNodesAndTokens(newRoot).First(t => t.IsNode).AsNode();
            Assert.True(firstNodeInNewTree.ContainsAnnotations);

            // Pick the node which was annotated and see if it has the annotation
            var rightNode = firstNodeInNewTree.ChildNodesAndTokens().Last(t => t.IsNode).AsNode();
            Assert.NotNull(rightNode.GetAnnotations().Single());
        }

        [Fact]
        public void TestCSharpAllInOne()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.ExperimentalParseOptions);

            TestAnnotation(tree);
        }

        [Fact]
        public void TestCSharpAllInOneRandom()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.ExperimentalParseOptions);

            TestRandomAnnotations(tree);
        }

        [Fact]
        public void TestCSharpAllInOneManyRandom()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.ExperimentalParseOptions);

            TestManyRandomAnnotations(tree);
        }

        [Fact]
        public void TestCSharpAllInOneTrivia()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.ExperimentalParseOptions);

            TestTriviaAnnotation(tree);
        }

        [Fact]
        public void TestCopyAnnotations1()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.ExperimentalParseOptions);
            var tree2 = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.ExperimentalParseOptions);

            TestCopyAnnotations(tree1, tree2);
        }

        #endregion

        private void TestMultipleAnnotationsInTree(SyntaxNode oldRoot, SyntaxNode newRoot, List<SyntaxAnnotation> annotations)
        {
            foreach (var annotation in annotations)
            {
                // Verify annotations in Nodes or Tokens
                var annotatedNodesOrTokens = newRoot.GetAnnotatedNodesAndTokens(annotation).OrderBy(t => t.SpanStart).ToList();
                var actualNodesOrTokens = GetAllNodesAndTokens(oldRoot).OrderBy(t => t.SpanStart).ToList();

                Assert.Equal(actualNodesOrTokens.Count, annotatedNodesOrTokens.Count);

                for (int i = 0; i < actualNodesOrTokens.Count(); i++)
                {
                    var oldNode = actualNodesOrTokens.ElementAt(i);
                    var annotatedNode = annotatedNodesOrTokens.ElementAt(i);
                    Assert.Equal(oldNode.FullSpan, annotatedNode.FullSpan);
                    Assert.Equal(oldNode.Span, annotatedNode.Span);
                    Assert.True(oldNode.IsEquivalentTo(annotatedNode));
                }

                // Verify annotations in Trivia
                var annotatedTrivia = newRoot.GetAnnotatedTrivia(annotation).OrderBy(t => t.SpanStart);
                var actualTrivia = GetAllTrivia(oldRoot).OrderBy(t => t.SpanStart);

                Assert.Equal(annotatedTrivia.Count(), actualTrivia.Count());

                for (int i = 0; i < actualTrivia.Count(); i++)
                {
                    var oldTrivia = actualTrivia.ElementAt(i);
                    var newTrivia = annotatedTrivia.ElementAt(i);
                    Assert.Equal(oldTrivia.FullSpan, newTrivia.FullSpan);
                    Assert.Equal(oldTrivia.Span, newTrivia.Span);
                    Assert.True(oldTrivia.IsEquivalentTo(newTrivia));
                }
            }
        }

        private void TestCopyAnnotations(SyntaxTree tree1, SyntaxTree tree2)
        {
            // create 10 annotations
            var annotations = new List<SyntaxAnnotation>(Enumerable.Range(0, 10).Select(_ => new SyntaxAnnotation()));

            // add a random annotation to every single node, token, trivia
            var rewriter = new InjectRandomAnnotationsRewriter(annotations);
            var sourceTreeRoot = rewriter.Visit(tree1.GetCompilationUnitRoot());

            var destTreeRoot = CopyAnnotationsTo(sourceTreeRoot, tree2.GetCompilationUnitRoot());

            // now we have two tree with same annotation everywhere
            // verify that
            foreach (var annotation in annotations)
            {
                // verify annotation at nodes and tokens
                var sourceNodeOrTokens = sourceTreeRoot.GetAnnotatedNodesAndTokens(annotation);
                var sourceNodeOrTokenEnumerator = sourceNodeOrTokens.GetEnumerator();

                var destNodeOrTokens = destTreeRoot.GetAnnotatedNodesAndTokens(annotation);
                var destNodeOrTokenEnumerator = destNodeOrTokens.GetEnumerator();

                Assert.Equal(sourceNodeOrTokens.Count(), destNodeOrTokens.Count());

                while (sourceNodeOrTokenEnumerator.MoveNext() && destNodeOrTokenEnumerator.MoveNext())
                {
                    Assert.True(sourceNodeOrTokenEnumerator.Current.IsEquivalentTo(destNodeOrTokenEnumerator.Current));
                }

                // verify annotation at trivia
                var sourceTrivia = sourceTreeRoot.GetAnnotatedTrivia(annotation);
                var destTrivia = destTreeRoot.GetAnnotatedTrivia(annotation);

                var sourceTriviaEnumerator = sourceTrivia.GetEnumerator();
                var destTriviaEnumerator = destTrivia.GetEnumerator();

                Assert.Equal(sourceTrivia.Count(), destTrivia.Count());

                while (sourceTriviaEnumerator.MoveNext() && destTriviaEnumerator.MoveNext())
                {
                    Assert.True(sourceTriviaEnumerator.Current.IsEquivalentTo(destTriviaEnumerator.Current));
                }
            }
        }

        private SyntaxNode CopyAnnotationsTo(SyntaxNode sourceTreeRoot, SyntaxNode destTreeRoot)
        {
            // now I have a tree that has annotation at every node/token/trivia
            // copy all those annotation to tree2 and create map from old one to new one

            var sourceTreeNodeOrTokenEnumerator = GetAllNodesAndTokens(sourceTreeRoot).GetEnumerator();
            var destTreeNodeOrTokenEnumerator = GetAllNodesAndTokens(destTreeRoot).GetEnumerator();

            var nodeOrTokenMap = new Dictionary<SyntaxNodeOrToken, SyntaxNodeOrToken>();
            while (sourceTreeNodeOrTokenEnumerator.MoveNext() && destTreeNodeOrTokenEnumerator.MoveNext())
            {
                if (sourceTreeNodeOrTokenEnumerator.Current.IsNode)
                {
                    var oldNode = destTreeNodeOrTokenEnumerator.Current.AsNode();
                    var newNode = sourceTreeNodeOrTokenEnumerator.Current.AsNode().CopyAnnotationsTo(oldNode);
                    nodeOrTokenMap.Add(oldNode, newNode);
                }
                else if (sourceTreeNodeOrTokenEnumerator.Current.IsToken)
                {
                    var oldToken = destTreeNodeOrTokenEnumerator.Current.AsToken();
                    var newToken = sourceTreeNodeOrTokenEnumerator.Current.AsToken().CopyAnnotationsTo(oldToken);
                    nodeOrTokenMap.Add(oldToken, newToken);
                }
            }

            // copy annotations at trivia
            var sourceTreeTriviaEnumerator = GetAllTrivia(sourceTreeRoot).GetEnumerator();
            var destTreeTriviaEnumerator = GetAllTrivia(destTreeRoot).GetEnumerator();

            var triviaMap = new Dictionary<SyntaxTrivia, SyntaxTrivia>();
            while (sourceTreeTriviaEnumerator.MoveNext() && destTreeTriviaEnumerator.MoveNext())
            {
                var oldTrivia = destTreeTriviaEnumerator.Current;
                var newTrivia = sourceTreeTriviaEnumerator.Current.CopyAnnotationsTo(oldTrivia);
                triviaMap.Add(oldTrivia, newTrivia);
            }

            var copier = new CopyAnnotationRewriter(nodeOrTokenMap, triviaMap);
            return copier.Visit(destTreeRoot);
        }

        private void TestManyRandomAnnotations(SyntaxTree syntaxTree)
        {
            // inject annotations in random places and see whether it is preserved after tree transformation
            var annotations = new List<Tuple<SyntaxAnnotation, SyntaxNodeOrToken>>();

            // we give constant seed so that we get exact same sequence every time.
            var randomGenerator = new Random(0);

            var currentRoot = syntaxTree.GetCompilationUnitRoot();
            var count = GetAllNodesAndTokens(currentRoot).Count;

            // add one in root
            var rootAnnotation = new SyntaxAnnotation();
            annotations.Add(Tuple.Create(rootAnnotation, new SyntaxNodeOrToken(currentRoot)));

            var rootAnnotated = AddAnnotationTo(rootAnnotation, currentRoot);
            currentRoot = Replace(currentRoot, currentRoot, rootAnnotated);

            for (int i = 0; i < 20; i++)
            {
                var annotation = new SyntaxAnnotation();
                var item = GetAllNodesAndTokens(currentRoot)[randomGenerator.Next(count - 1)];

                // save it
                annotations.Add(Tuple.Create(annotation, item));

                var annotated = AddAnnotationTo(annotation, item);
                currentRoot = Replace(currentRoot, item, annotated);

                TestAnnotations(annotations, currentRoot);
            }
        }

        private void TestRandomAnnotations(SyntaxTree syntaxTree)
        {
            // inject annotations in random places and see whether it is preserved after tree transformation
            var firstAnnotation = new SyntaxAnnotation();
            var secondAnnotation = new SyntaxAnnotation();

            var candidatePool = GetAllNodesAndTokens(syntaxTree.GetCompilationUnitRoot());
            var numberOfCandidates = candidatePool.Count;

            // we give constant seed so that we get exact same sequence every time.
            var randomGenerator = new Random(100);

            for (int i = 0; i < 20; i++)
            {
                var firstItem = candidatePool[randomGenerator.Next(numberOfCandidates - 1)];
                var firstAnnotated = AddAnnotationTo(firstAnnotation, firstItem);

                var newRoot = Replace(syntaxTree.GetCompilationUnitRoot(), firstItem, firstAnnotated);

                // check the first annotation
                TestAnnotation(firstAnnotation, newRoot, firstItem);

                var secondItem = GetAllNodesAndTokens(newRoot)[randomGenerator.Next(numberOfCandidates - 1)];
                var secondAnnotated = AddAnnotationTo(secondAnnotation, secondItem);

                // transform the tree again
                newRoot = Replace(newRoot, secondItem, secondAnnotated);

                // make sure both annotation are in the tree
                TestAnnotation(firstAnnotation, newRoot, firstItem);
                TestAnnotation(secondAnnotation, newRoot, secondItem);
            }
        }

        public TRoot Replace<TRoot>(TRoot root, SyntaxNodeOrToken oldNodeOrToken, SyntaxNodeOrToken newNodeOrToken) where TRoot : SyntaxNode
        {
            if (oldNodeOrToken.IsToken)
            {
                return root.ReplaceToken(oldNodeOrToken.AsToken(), newNodeOrToken.AsToken());
            }

            return root.ReplaceNode(oldNodeOrToken.AsNode(), newNodeOrToken.AsNode());
        }

        public SyntaxNodeOrToken AddAnnotationTo(SyntaxAnnotation annotation, SyntaxNodeOrToken nodeOrToken)
        {
            return nodeOrToken.WithAdditionalAnnotations(annotation);
        }

        private void TestAnnotations(List<Tuple<SyntaxAnnotation, SyntaxNodeOrToken>> annotations, SyntaxNode currentRoot)
        {
            // check every annotations
            foreach (var pair in annotations)
            {
                var annotation = pair.Item1;
                var nodeOrToken = pair.Item2;

                TestAnnotation(annotation, currentRoot, nodeOrToken);
            }
        }

        private void TestTriviaAnnotation(SyntaxTree syntaxTree)
        {
            var annotation = new SyntaxAnnotation();

            foreach (var trivia in GetAllTrivia(syntaxTree.GetCompilationUnitRoot()))
            {
                // add one annotation and test its existence
                var newTrivia = trivia.WithAdditionalAnnotations(annotation);
                var newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceTrivia(trivia, newTrivia);

                TestAnnotation(annotation, newRoot, trivia);
            }
        }

        private void TestAnnotation(SyntaxTree syntaxTree)
        {
            var annotation = new SyntaxAnnotation();

            var allNodesAndTokens = GetAllNodesAndTokens(syntaxTree.GetCompilationUnitRoot());
            for (int i = 0; i < allNodesAndTokens.Count; i++)
            {
                var nodeOrToken = allNodesAndTokens[i];
                SyntaxNode newRoot;

                // add one annotation and test its existence
                if (nodeOrToken.IsToken)
                {
                    var newToken = nodeOrToken.AsToken().WithAdditionalAnnotations(annotation);
                    newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceToken(nodeOrToken.AsToken(), newToken);
                }
                else
                {
                    var newNode = nodeOrToken.AsNode().WithAdditionalAnnotations(annotation);
                    newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceNode(nodeOrToken.AsNode(), newNode);
                }

                TestAnnotation(annotation, newRoot, nodeOrToken);
            }
        }

        private void TestAnnotation(SyntaxAnnotation annotation, SyntaxNode root, SyntaxNodeOrToken oldNodeOrToken)
        {
            // Test for existence of exactly one annotation
            if (oldNodeOrToken.IsToken)
            {
                TestAnnotation(annotation, root, oldNodeOrToken.AsToken());
                return;
            }

            TestAnnotation(annotation, root, oldNodeOrToken.AsNode());
        }

        private void TestAnnotation(SyntaxAnnotation annotation, SyntaxNode root, SyntaxNode oldNode)
        {
            var results = root.GetAnnotatedNodesAndTokens(annotation);

            Assert.Equal(1, results.Count());

            var annotatedNode = results.Single().AsNode();

            // try to check whether it is same node as old node.
            Assert.Equal(oldNode.FullSpan, annotatedNode.FullSpan);
            Assert.Equal(oldNode.Span, annotatedNode.Span);
            Assert.True(oldNode.IsEquivalentTo(annotatedNode));
        }

        private void TestAnnotation(SyntaxAnnotation annotation, SyntaxNode root, SyntaxToken oldToken)
        {
            var results = root.GetAnnotatedNodesAndTokens(annotation);

            Assert.Equal(1, results.Count());

            var annotatedToken = results.Single().AsToken();

            // try to check whether it is same token as old token.
            Assert.Equal(oldToken.FullSpan, annotatedToken.FullSpan);
            Assert.Equal(oldToken.Span, annotatedToken.Span);
            Assert.True(oldToken.IsEquivalentTo(annotatedToken));
        }

        private void TestAnnotation(SyntaxAnnotation annotation, SyntaxNode root, SyntaxTrivia oldTrivia)
        {
            var results = root.GetAnnotatedTrivia(annotation);

            Assert.Equal(1, results.Count());

            var annotatedTrivia = results.Single();

            // try to check whether it is same token as old token.
            Assert.Equal(oldTrivia.FullSpan, annotatedTrivia.FullSpan);
            Assert.Equal(oldTrivia.Span, annotatedTrivia.Span);
            Assert.True(oldTrivia.IsEquivalentTo(annotatedTrivia));
        }

        private List<SyntaxTrivia> GetAllTrivia(SyntaxNode root)
        {
            var collector = new Collector();
            collector.Visit(root);

            return collector.Trivia;
        }

        private List<SyntaxNodeOrToken> GetAllNodesAndTokens(SyntaxNode root)
        {
            return root.DescendantNodesAndTokensAndSelf(descendIntoTrivia: true).Select(n => (SyntaxNodeOrToken)n).ToList();
        }

        private class Collector : CSharpSyntaxWalker
        {
            public List<SyntaxNodeOrToken> NodeOrTokens { get; }
            public List<SyntaxTrivia> Trivia { get; }

            public Collector()
                : base(SyntaxWalkerDepth.StructuredTrivia)
            {
                this.NodeOrTokens = new List<SyntaxNodeOrToken>();
                this.Trivia = new List<SyntaxTrivia>();
            }

            public override void Visit(SyntaxNode node)
            {
                if (node != null)
                {
                    this.NodeOrTokens.Add(node);
                }

                base.Visit(node);
            }

            public override void VisitToken(SyntaxToken token)
            {
                if (!token.IsKind(SyntaxKind.None))
                {
                    this.NodeOrTokens.Add(token);
                }

                base.VisitToken(token);
            }

            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                if (!trivia.IsKind(SyntaxKind.None))
                {
                    this.Trivia.Add(trivia);
                }

                base.VisitTrivia(trivia);
            }
        }

        private class InjectAnnotationRewriter : CSharpSyntaxRewriter
        {
            private readonly SyntaxAnnotation _annotation;

            public InjectAnnotationRewriter(SyntaxAnnotation annotation) :
                base(visitIntoStructuredTrivia: true)
            {
                _annotation = annotation;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == null)
                {
                    return node;
                }

                return base.Visit(node).WithAdditionalAnnotations(_annotation);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (token.IsKind(SyntaxKind.None))
                {
                    return token;
                }

                return base.VisitToken(token).WithAdditionalAnnotations(_annotation);
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.IsKind(SyntaxKind.None))
                {
                    return trivia;
                }

                if (trivia.HasStructure)
                {
                    return base.VisitTrivia(trivia);
                }

                return base.VisitTrivia(trivia).WithAdditionalAnnotations(_annotation);
            }
        }

        private class InjectRandomAnnotationsRewriter : CSharpSyntaxRewriter
        {
            private readonly List<SyntaxAnnotation> _annotations;
            private readonly Random _random;

            public InjectRandomAnnotationsRewriter(List<SyntaxAnnotation> annotations) :
                base(visitIntoStructuredTrivia: true)
            {
                _annotations = annotations;
                _random = new Random(10);
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == null)
                {
                    return node;
                }

                var annotation = _annotations[_random.Next(0, _annotations.Count - 1)];
                return base.Visit(node).WithAdditionalAnnotations(annotation);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (token.Kind() == SyntaxKind.None)
                {
                    return token;
                }

                var annotation = _annotations[_random.Next(0, _annotations.Count - 1)];
                return base.VisitToken(token).WithAdditionalAnnotations(annotation);
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.IsKind(SyntaxKind.None))
                {
                    return trivia;
                }

                // annotation will be set by actual structure trivia
                if (trivia.HasStructure)
                {
                    return base.VisitTrivia(trivia);
                }

                var annotation = _annotations[_random.Next(0, _annotations.Count - 1)];
                return base.VisitTrivia(trivia).WithAdditionalAnnotations(annotation);
            }
        }

        private class CopyAnnotationRewriter : CSharpSyntaxRewriter
        {
            private readonly Dictionary<SyntaxNodeOrToken, SyntaxNodeOrToken> _nodeOrTokenMap;
            private readonly Dictionary<SyntaxTrivia, SyntaxTrivia> _triviaMap;

            public CopyAnnotationRewriter(Dictionary<SyntaxNodeOrToken, SyntaxNodeOrToken> nodeOrTokenMap, Dictionary<SyntaxTrivia, SyntaxTrivia> triviaMap) :
                base(visitIntoStructuredTrivia: true)
            {
                _nodeOrTokenMap = nodeOrTokenMap;
                _triviaMap = triviaMap;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == null)
                {
                    return node;
                }

                return _nodeOrTokenMap[node].AsNode().CopyAnnotationsTo(base.Visit(node));
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (token.IsKind(SyntaxKind.None))
                {
                    return token;
                }

                return _nodeOrTokenMap[token].AsToken().CopyAnnotationsTo(base.VisitToken(token));
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.IsKind(SyntaxKind.None))
                {
                    return trivia;
                }

                // annotation will be set by actual structure trivia
                if (trivia.HasStructure)
                {
                    return base.VisitTrivia(trivia);
                }

                return _triviaMap[trivia].CopyAnnotationsTo(base.VisitTrivia(trivia));
            }
        }

        private readonly string _helloWorldCode = @"using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sample Documentation
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        // User Comments
        Console.WriteLine(""Hello World!"");
    }
}";

        private readonly string _allInOneCSharpCode = TestResource.AllInOneCSharpCode;
    }
}
