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
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(code);

            TestAnnotation(tree);
        }

        [Fact]
        public void TestAddAnnotationToNullSyntaxToken()
        {
            SyntaxAnnotation annotation = new SyntaxAnnotation();
            var oldToken = default(SyntaxToken);
            SyntaxToken newToken = oldToken.WithAdditionalAnnotations(annotation);

            Assert.False(newToken.ContainsAnnotations);
        }

        [Fact]
        public void TestAddAnnotationToNullSyntaxTrivia()
        {
            SyntaxAnnotation annotation = new SyntaxAnnotation();
            var oldTrivia = default(SyntaxTrivia);
            SyntaxTrivia newTrivia = oldTrivia.WithAdditionalAnnotations(annotation);

            Assert.False(newTrivia.ContainsAnnotations);
        }

        [Fact]
        public void TestCopyAnnotationToNullSyntaxNode()
        {
            CompilationUnitSyntax fromNode = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot();
            var toNode = default(SyntaxNode);
            SyntaxNode annotatedNode = fromNode.CopyAnnotationsTo(toNode);
            Assert.Null(annotatedNode);
        }

        [Fact]
        public void TestCopyAnnotationOfZeroLengthToSyntaxNode()
        {
            CompilationUnitSyntax fromNode = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot();
            CompilationUnitSyntax toNode = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot();
            CompilationUnitSyntax annotatedNode = fromNode.CopyAnnotationsTo(toNode);
            Assert.Equal(annotatedNode, toNode); // Reference Equal
        }

        [Fact]
        public void TestCopyAnnotationFromNullSyntaxToken()
        {
            var fromToken = default(SyntaxToken);
            SyntaxToken toToken = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot().DescendantTokens().First();
            SyntaxToken annotatedToken = fromToken.CopyAnnotationsTo(toToken);
            Assert.True(annotatedToken.IsEquivalentTo(toToken));
        }

        [Fact]
        public void TestCopyAnnotationToNullSyntaxToken()
        {
            SyntaxToken fromToken = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot().DescendantTokens().First();
            var toToken = default(SyntaxToken);
            SyntaxToken annotatedToken = fromToken.CopyAnnotationsTo(toToken);
            Assert.True(annotatedToken.IsEquivalentTo(default(SyntaxToken)));
        }

        [Fact]
        public void TestCopyAnnotationOfZeroLengthToSyntaxToken()
        {
            SyntaxToken fromToken = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot().DescendantTokens().First();
            SyntaxToken toToken = SyntaxFactory.ParseSyntaxTree(_helloWorldCode).GetCompilationUnitRoot().DescendantTokens().First();
            SyntaxToken annotatedToken = fromToken.CopyAnnotationsTo(toToken);
            Assert.Equal(annotatedToken, toToken); // Reference Equal
        }

        [Fact]
        public void TestCopyAnnotationFromNullSyntaxTrivia()
        {
            var fromTrivia = default(SyntaxTrivia);
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxTrivia toTrivia = GetAllTrivia(tree.GetCompilationUnitRoot()).FirstOrDefault();
            SyntaxTrivia annotatedTrivia = fromTrivia.CopyAnnotationsTo(toTrivia);
            Assert.True(toTrivia.IsEquivalentTo(annotatedTrivia));
        }

        [Fact]
        public void TestCopyAnnotationToNullSyntaxTrivia()
        {
            var toTrivia = default(SyntaxTrivia);
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxTrivia fromTrivia = GetAllTrivia(tree.GetCompilationUnitRoot()).FirstOrDefault();
            SyntaxTrivia annotatedTrivia = fromTrivia.CopyAnnotationsTo(toTrivia);
            Assert.True(default(SyntaxTrivia).IsEquivalentTo(annotatedTrivia));
        }

        [Fact]
        public void TestCopyAnnotationOfZeroLengthToSyntaxTrivia()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxTrivia fromTrivia = GetAllTrivia(tree.GetCompilationUnitRoot()).FirstOrDefault();
            SyntaxTrivia toTrivia = GetAllTrivia(tree.GetCompilationUnitRoot()).FirstOrDefault();
            SyntaxTrivia annotatedTrivia = fromTrivia.CopyAnnotationsTo(toTrivia);
            Assert.Equal(annotatedTrivia, toTrivia); // Reference Equal
        }

        #endregion

        #region Negative Tests

        [Fact]
        public void TestMissingAnnotationsOnNodesOrTokens()
        {
            SyntaxAnnotation annotation = new SyntaxAnnotation();
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.Regular);

            IEnumerable<SyntaxNodeOrToken> matchingNodesOrTokens = tree.GetCompilationUnitRoot().GetAnnotatedNodesAndTokens(annotation);
            Assert.Empty(matchingNodesOrTokens);
        }

        [Fact]
        public void TestMissingAnnotationsOnTrivia()
        {
            SyntaxAnnotation annotation = new SyntaxAnnotation();
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.Regular);

            IEnumerable<SyntaxTrivia> matchingTrivia = tree.GetCompilationUnitRoot().GetAnnotatedTrivia(annotation);
            Assert.Empty(matchingTrivia);
        }

        #endregion

        #region Other Functional Tests

        [Fact]
        public void TestSimpleMultipleAnnotationsOnNode()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxAnnotation annotation1 = new SyntaxAnnotation();
            SyntaxAnnotation annotation2 = new SyntaxAnnotation();

            // Pick the first node from tree
            SyntaxNode node = GetAllNodesAndTokens(tree.GetCompilationUnitRoot()).First(t => t.IsNode).AsNode();

            // Annotate it
            SyntaxNode annotatedNode = node.WithAdditionalAnnotations(annotation1);
            CompilationUnitSyntax newRoot = tree.GetCompilationUnitRoot().ReplaceNode(node, annotatedNode);

            // Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, node);

            // Pick the annotated node from the new tree
            SyntaxNode node2 = newRoot.GetAnnotatedNodesAndTokens(annotation1).Single().AsNode();

            // Annotate it again
            SyntaxNode twiceAnnotatedNode = node2.WithAdditionalAnnotations(annotation2);
            CompilationUnitSyntax twiceAnnotatedRoot = newRoot.ReplaceNode(node2, twiceAnnotatedNode);

            // Verify the recent annotation
            TestAnnotation(annotation2, twiceAnnotatedRoot, node2);

            // Verify both annotations exist in the newTree
            TestAnnotation(annotation1, twiceAnnotatedRoot, node);
            TestAnnotation(annotation2, twiceAnnotatedRoot, node);
        }

        [Fact]
        public void TestSimpleMultipleAnnotationsOnToken()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxAnnotation annotation1 = new SyntaxAnnotation();
            SyntaxAnnotation annotation2 = new SyntaxAnnotation();

            // Pick the first node from tree
            SyntaxToken token = GetAllNodesAndTokens(tree.GetCompilationUnitRoot()).First(t => t.IsToken).AsToken();

            // Annotate it
            SyntaxToken annotatedToken = token.WithAdditionalAnnotations(annotation1);
            CompilationUnitSyntax newRoot = tree.GetCompilationUnitRoot().ReplaceToken(token, annotatedToken);

            // Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, token);

            // Pick the annotated node from the new tree
            SyntaxToken token2 = newRoot.GetAnnotatedNodesAndTokens(annotation1).Single().AsToken();

            // Annotate it again
            SyntaxToken twiceAnnotatedToken = token2.WithAdditionalAnnotations(annotation2);
            CompilationUnitSyntax twiceAnnotatedRoot = newRoot.ReplaceToken(token2, twiceAnnotatedToken);

            // Verify the recent annotation
            TestAnnotation(annotation2, twiceAnnotatedRoot, token2);

            // Verify both annotations exist in the newTree
            TestAnnotation(annotation1, twiceAnnotatedRoot, token);
            TestAnnotation(annotation2, twiceAnnotatedRoot, token);
        }

        [Fact]
        public void TestSimpleMultipleAnnotationsOnTrivia()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxAnnotation annotation1 = new SyntaxAnnotation();
            SyntaxAnnotation annotation2 = new SyntaxAnnotation();

            // Pick the first node from tree
            SyntaxTrivia trivia = GetAllTrivia(tree.GetCompilationUnitRoot()).First();

            // Annotate it
            SyntaxTrivia annotatedTrivia = trivia.WithAdditionalAnnotations(annotation1);
            CompilationUnitSyntax newRoot = tree.GetCompilationUnitRoot().ReplaceTrivia(trivia, annotatedTrivia);

            // Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, trivia);

            // Pick the annotated node from the new tree
            SyntaxTrivia trivia2 = newRoot.GetAnnotatedTrivia(annotation1).Single();

            // Annotate it again
            SyntaxTrivia twiceAnnotatedTrivia = trivia2.WithAdditionalAnnotations(annotation2);
            CompilationUnitSyntax twiceAnnotatedRoot = newRoot.ReplaceTrivia(trivia2, twiceAnnotatedTrivia);

            // Verify the recent annotation
            TestAnnotation(annotation2, twiceAnnotatedRoot, trivia2);

            // Verify both annotations exist in the newTree
            TestAnnotation(annotation1, twiceAnnotatedRoot, trivia);
            TestAnnotation(annotation2, twiceAnnotatedRoot, trivia);
        }

        [Fact]
        public void TestMultipleAnnotationsOnAllNodesTokensAndTrivia()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxNode newRoot = tree.GetCompilationUnitRoot();

            var annotations = new List<SyntaxAnnotation>(Enumerable.Range(0, 3).Select(_ => new SyntaxAnnotation()));

            // add annotation one by one to every single node, token, trivia
            foreach (SyntaxAnnotation annotation in annotations)
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
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);

            TestAnnotation(tree);
            TestTriviaAnnotation(tree);
        }

        [Fact]
        public void TestIfNodeHasAnnotations()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_helloWorldCode);
            SyntaxAnnotation annotation1 = new SyntaxAnnotation();

            // Pick the first node from tree
            SyntaxNode firstNode = GetAllNodesAndTokens(tree.GetCompilationUnitRoot()).First(t => t.IsNode).AsNode();

            ChildSyntaxList children = firstNode.ChildNodesAndTokens();
            SyntaxNode lastChildOfFirstNode = children.Last(t => t.IsNode).AsNode();
            SyntaxNode annotatedNode = lastChildOfFirstNode.WithAdditionalAnnotations(annotation1);
            CompilationUnitSyntax newRoot = tree.GetCompilationUnitRoot().ReplaceNode(lastChildOfFirstNode, annotatedNode);

            // Verify if annotation Exists
            TestAnnotation(annotation1, newRoot, lastChildOfFirstNode);

            // Pick the first node from new tree and see if any of its children is annotated
            SyntaxNode firstNodeInNewTree = GetAllNodesAndTokens(newRoot).First(t => t.IsNode).AsNode();
            Assert.True(firstNodeInNewTree.ContainsAnnotations);

            // Pick the node which was annotated and see if it has the annotation
            SyntaxNode rightNode = firstNodeInNewTree.ChildNodesAndTokens().Last(t => t.IsNode).AsNode();
            Assert.NotNull(rightNode.GetAnnotations().Single());
        }

        [Fact]
        public void TestCSharpAllInOne()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.Regular);

            TestAnnotation(tree);
        }

        [Fact]
        public void TestCSharpAllInOneRandom()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.Regular);

            TestRandomAnnotations(tree);
        }

        [Fact]
        public void TestCSharpAllInOneManyRandom()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.Regular);

            TestManyRandomAnnotations(tree);
        }

        [Fact]
        public void TestCSharpAllInOneTrivia()
        {
            SyntaxTree tree = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.Regular);

            TestTriviaAnnotation(tree);
        }

        [Fact]
        public void TestCopyAnnotations1()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.Regular);
            SyntaxTree tree2 = SyntaxFactory.ParseSyntaxTree(_allInOneCSharpCode, options: Test.Utilities.TestOptions.Regular);

            TestCopyAnnotations(tree1, tree2);
        }

        #endregion

        private void TestMultipleAnnotationsInTree(SyntaxNode oldRoot, SyntaxNode newRoot, List<SyntaxAnnotation> annotations)
        {
            foreach (SyntaxAnnotation annotation in annotations)
            {
                // Verify annotations in Nodes or Tokens
                var annotatedNodesOrTokens = newRoot.GetAnnotatedNodesAndTokens(annotation).OrderBy(t => t.SpanStart).ToList();
                var actualNodesOrTokens = GetAllNodesAndTokens(oldRoot).OrderBy(t => t.SpanStart).ToList();

                Assert.Equal(actualNodesOrTokens.Count, annotatedNodesOrTokens.Count);

                for (int i = 0; i < actualNodesOrTokens.Count(); i++)
                {
                    SyntaxNodeOrToken oldNode = actualNodesOrTokens.ElementAt(i);
                    SyntaxNodeOrToken annotatedNode = annotatedNodesOrTokens.ElementAt(i);
                    Assert.Equal(oldNode.FullSpan, annotatedNode.FullSpan);
                    Assert.Equal(oldNode.Span, annotatedNode.Span);
                    Assert.True(oldNode.IsEquivalentTo(annotatedNode));
                }

                // Verify annotations in Trivia
                IOrderedEnumerable<SyntaxTrivia> annotatedTrivia = newRoot.GetAnnotatedTrivia(annotation).OrderBy(t => t.SpanStart);
                IOrderedEnumerable<SyntaxTrivia> actualTrivia = GetAllTrivia(oldRoot).OrderBy(t => t.SpanStart);

                Assert.Equal(annotatedTrivia.Count(), actualTrivia.Count());

                for (int i = 0; i < actualTrivia.Count(); i++)
                {
                    SyntaxTrivia oldTrivia = actualTrivia.ElementAt(i);
                    SyntaxTrivia newTrivia = annotatedTrivia.ElementAt(i);
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
            SyntaxNode sourceTreeRoot = rewriter.Visit(tree1.GetCompilationUnitRoot());

            SyntaxNode destTreeRoot = CopyAnnotationsTo(sourceTreeRoot, tree2.GetCompilationUnitRoot());

            // now we have two tree with same annotation everywhere
            // verify that
            foreach (SyntaxAnnotation annotation in annotations)
            {
                // verify annotation at nodes and tokens
                IEnumerable<SyntaxNodeOrToken> sourceNodeOrTokens = sourceTreeRoot.GetAnnotatedNodesAndTokens(annotation);
                IEnumerator<SyntaxNodeOrToken> sourceNodeOrTokenEnumerator = sourceNodeOrTokens.GetEnumerator();

                IEnumerable<SyntaxNodeOrToken> destNodeOrTokens = destTreeRoot.GetAnnotatedNodesAndTokens(annotation);
                IEnumerator<SyntaxNodeOrToken> destNodeOrTokenEnumerator = destNodeOrTokens.GetEnumerator();

                Assert.Equal(sourceNodeOrTokens.Count(), destNodeOrTokens.Count());

                while (sourceNodeOrTokenEnumerator.MoveNext() && destNodeOrTokenEnumerator.MoveNext())
                {
                    Assert.True(sourceNodeOrTokenEnumerator.Current.IsEquivalentTo(destNodeOrTokenEnumerator.Current));
                }

                // verify annotation at trivia
                IEnumerable<SyntaxTrivia> sourceTrivia = sourceTreeRoot.GetAnnotatedTrivia(annotation);
                IEnumerable<SyntaxTrivia> destTrivia = destTreeRoot.GetAnnotatedTrivia(annotation);

                IEnumerator<SyntaxTrivia> sourceTriviaEnumerator = sourceTrivia.GetEnumerator();
                IEnumerator<SyntaxTrivia> destTriviaEnumerator = destTrivia.GetEnumerator();

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

            List<SyntaxNodeOrToken>.Enumerator sourceTreeNodeOrTokenEnumerator = GetAllNodesAndTokens(sourceTreeRoot).GetEnumerator();
            List<SyntaxNodeOrToken>.Enumerator destTreeNodeOrTokenEnumerator = GetAllNodesAndTokens(destTreeRoot).GetEnumerator();

            var nodeOrTokenMap = new Dictionary<SyntaxNodeOrToken, SyntaxNodeOrToken>();
            while (sourceTreeNodeOrTokenEnumerator.MoveNext() && destTreeNodeOrTokenEnumerator.MoveNext())
            {
                if (sourceTreeNodeOrTokenEnumerator.Current.IsNode)
                {
                    SyntaxNode oldNode = destTreeNodeOrTokenEnumerator.Current.AsNode();
                    SyntaxNode newNode = sourceTreeNodeOrTokenEnumerator.Current.AsNode().CopyAnnotationsTo(oldNode);
                    nodeOrTokenMap.Add(oldNode, newNode);
                }
                else if (sourceTreeNodeOrTokenEnumerator.Current.IsToken)
                {
                    SyntaxToken oldToken = destTreeNodeOrTokenEnumerator.Current.AsToken();
                    SyntaxToken newToken = sourceTreeNodeOrTokenEnumerator.Current.AsToken().CopyAnnotationsTo(oldToken);
                    nodeOrTokenMap.Add(oldToken, newToken);
                }
            }

            // copy annotations at trivia
            List<SyntaxTrivia>.Enumerator sourceTreeTriviaEnumerator = GetAllTrivia(sourceTreeRoot).GetEnumerator();
            List<SyntaxTrivia>.Enumerator destTreeTriviaEnumerator = GetAllTrivia(destTreeRoot).GetEnumerator();

            var triviaMap = new Dictionary<SyntaxTrivia, SyntaxTrivia>();
            while (sourceTreeTriviaEnumerator.MoveNext() && destTreeTriviaEnumerator.MoveNext())
            {
                SyntaxTrivia oldTrivia = destTreeTriviaEnumerator.Current;
                SyntaxTrivia newTrivia = sourceTreeTriviaEnumerator.Current.CopyAnnotationsTo(oldTrivia);
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

            CompilationUnitSyntax currentRoot = syntaxTree.GetCompilationUnitRoot();
            var count = GetAllNodesAndTokens(currentRoot).Count;

            // add one in root
            var rootAnnotation = new SyntaxAnnotation();
            annotations.Add(Tuple.Create(rootAnnotation, new SyntaxNodeOrToken(currentRoot)));

            SyntaxNodeOrToken rootAnnotated = AddAnnotationTo(rootAnnotation, currentRoot);
            currentRoot = Replace(currentRoot, currentRoot, rootAnnotated);

            for (int i = 0; i < 20; i++)
            {
                var annotation = new SyntaxAnnotation();
                SyntaxNodeOrToken item = GetAllNodesAndTokens(currentRoot)[randomGenerator.Next(count - 1)];

                // save it
                annotations.Add(Tuple.Create(annotation, item));

                SyntaxNodeOrToken annotated = AddAnnotationTo(annotation, item);
                currentRoot = Replace(currentRoot, item, annotated);

                TestAnnotations(annotations, currentRoot);
            }
        }

        private void TestRandomAnnotations(SyntaxTree syntaxTree)
        {
            // inject annotations in random places and see whether it is preserved after tree transformation
            var firstAnnotation = new SyntaxAnnotation();
            var secondAnnotation = new SyntaxAnnotation();

            List<SyntaxNodeOrToken> candidatePool = GetAllNodesAndTokens(syntaxTree.GetCompilationUnitRoot());
            var numberOfCandidates = candidatePool.Count;

            // we give constant seed so that we get exact same sequence every time.
            var randomGenerator = new Random(100);

            for (int i = 0; i < 20; i++)
            {
                SyntaxNodeOrToken firstItem = candidatePool[randomGenerator.Next(numberOfCandidates - 1)];
                SyntaxNodeOrToken firstAnnotated = AddAnnotationTo(firstAnnotation, firstItem);

                CompilationUnitSyntax newRoot = Replace(syntaxTree.GetCompilationUnitRoot(), firstItem, firstAnnotated);

                // check the first annotation
                TestAnnotation(firstAnnotation, newRoot, firstItem);

                SyntaxNodeOrToken secondItem = GetAllNodesAndTokens(newRoot)[randomGenerator.Next(numberOfCandidates - 1)];
                SyntaxNodeOrToken secondAnnotated = AddAnnotationTo(secondAnnotation, secondItem);

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
            foreach (Tuple<SyntaxAnnotation, SyntaxNodeOrToken> pair in annotations)
            {
                SyntaxAnnotation annotation = pair.Item1;
                SyntaxNodeOrToken nodeOrToken = pair.Item2;

                TestAnnotation(annotation, currentRoot, nodeOrToken);
            }
        }

        private void TestTriviaAnnotation(SyntaxTree syntaxTree)
        {
            var annotation = new SyntaxAnnotation();

            foreach (SyntaxTrivia trivia in GetAllTrivia(syntaxTree.GetCompilationUnitRoot()))
            {
                // add one annotation and test its existence
                SyntaxTrivia newTrivia = trivia.WithAdditionalAnnotations(annotation);
                CompilationUnitSyntax newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceTrivia(trivia, newTrivia);

                TestAnnotation(annotation, newRoot, trivia);
            }
        }

        private void TestAnnotation(SyntaxTree syntaxTree)
        {
            var annotation = new SyntaxAnnotation();

            List<SyntaxNodeOrToken> allNodesAndTokens = GetAllNodesAndTokens(syntaxTree.GetCompilationUnitRoot());
            for (int i = 0; i < allNodesAndTokens.Count; i++)
            {
                SyntaxNodeOrToken nodeOrToken = allNodesAndTokens[i];
                SyntaxNode newRoot;

                // add one annotation and test its existence
                if (nodeOrToken.IsToken)
                {
                    SyntaxToken newToken = nodeOrToken.AsToken().WithAdditionalAnnotations(annotation);
                    newRoot = syntaxTree.GetCompilationUnitRoot().ReplaceToken(nodeOrToken.AsToken(), newToken);
                }
                else
                {
                    SyntaxNode newNode = nodeOrToken.AsNode().WithAdditionalAnnotations(annotation);
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
            IEnumerable<SyntaxNodeOrToken> results = root.GetAnnotatedNodesAndTokens(annotation);

            Assert.Equal(1, results.Count());

            SyntaxNode annotatedNode = results.Single().AsNode();

            // try to check whether it is same node as old node.
            Assert.Equal(oldNode.FullSpan, annotatedNode.FullSpan);
            Assert.Equal(oldNode.Span, annotatedNode.Span);
            Assert.True(oldNode.IsEquivalentTo(annotatedNode));
        }

        private void TestAnnotation(SyntaxAnnotation annotation, SyntaxNode root, SyntaxToken oldToken)
        {
            IEnumerable<SyntaxNodeOrToken> results = root.GetAnnotatedNodesAndTokens(annotation);

            Assert.Equal(1, results.Count());

            SyntaxToken annotatedToken = results.Single().AsToken();

            // try to check whether it is same token as old token.
            Assert.Equal(oldToken.FullSpan, annotatedToken.FullSpan);
            Assert.Equal(oldToken.Span, annotatedToken.Span);
            Assert.True(oldToken.IsEquivalentTo(annotatedToken));
        }

        private void TestAnnotation(SyntaxAnnotation annotation, SyntaxNode root, SyntaxTrivia oldTrivia)
        {
            IEnumerable<SyntaxTrivia> results = root.GetAnnotatedTrivia(annotation);

            Assert.Equal(1, results.Count());

            SyntaxTrivia annotatedTrivia = results.Single();

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

                SyntaxAnnotation annotation = _annotations[_random.Next(0, _annotations.Count - 1)];
                return base.Visit(node).WithAdditionalAnnotations(annotation);
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (token.Kind() == SyntaxKind.None)
                {
                    return token;
                }

                SyntaxAnnotation annotation = _annotations[_random.Next(0, _annotations.Count - 1)];
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

                SyntaxAnnotation annotation = _annotations[_random.Next(0, _annotations.Count - 1)];
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
