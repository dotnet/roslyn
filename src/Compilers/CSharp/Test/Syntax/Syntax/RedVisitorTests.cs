// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class RedVisitorTests
    {
        #region CSharpSyntaxVisitor

        [Fact]
        public void VisitDoesNotThrowOnNullNode()
        {
            var visitor = new DefaultVisitor();
            visitor.Visit(null);
        }

        #endregion

        #region CSharpSyntaxVisitor<TResult>

        [Fact]
        public void VisitDoesNotThrowOnNullNode_TResult()
        {
            var visitor = new DefaultVisitor<object>(new object());
            visitor.Visit(null);
        }

        #endregion

        #region CSharpSyntaxVisitor<TArgument, TResult>

        [Fact]
        public void VisitThrowsOnNullNode_TArgument_TResult()
        {
            var visitor = new DefaultVisitor<object?, object>(new object());
            Assert.ThrowsAny<NullReferenceException>(() => visitor.Visit(null!, null));
        }

        #endregion

        #region CSharpSyntaxWalker

        [Fact]
        public void WalkWithDepthNode()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker(SyntaxWalkerDepth.Node);

            walker.Visit(syntax);

            Assert.NotEmpty(walker.VisitedNodes);

            foreach (var visited in walker.VisitedNodes)
            {
                Assert.False(visited is SyntaxNode node && node.IsPartOfStructuredTrivia(), "Should not visit structured trivia.");
                Assert.False(visited is SyntaxToken, "Should not visit tokens.");
                Assert.False(visited is SyntaxTrivia, "Should not visit trivia.");
            }
        }


        [Fact]
        public void WalkWithDepthToken()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker(SyntaxWalkerDepth.Token);

            walker.Visit(syntax);

            Assert.NotEmpty(walker.VisitedNodes);

            foreach (var visited in walker.VisitedNodes)
            {
                Assert.False(visited is SyntaxNode node && node.IsPartOfStructuredTrivia(), "Should not visit structured trivia.");
                Assert.False(visited is SyntaxTrivia, "Should not visit trivia.");
            }
        }


        [Fact]
        public void WalkWithDepthTrivia()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker(SyntaxWalkerDepth.Trivia);

            walker.Visit(syntax);

            Assert.NotEmpty(walker.VisitedNodes);

            foreach (var visited in walker.VisitedNodes)
            {
                Assert.False(visited is SyntaxNode node && node.IsPartOfStructuredTrivia(), "Should not visit structured trivia.");
            }
        }

        [Fact]
        public void WalkWithNegativeDepthOnlyVisitsRoot()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker((SyntaxWalkerDepth)int.MinValue);

            walker.Visit(syntax);

            var visited = Assert.Single(walker.VisitedNodes);
            Assert.Same(syntax, visited);
        }

        [Fact]
        public void WalkWithMaximumDepthIsSameAsStructuredTriviaDepth()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker1 = new RecordingWalker(SyntaxWalkerDepth.StructuredTrivia);
            var walker2 = new RecordingWalker((SyntaxWalkerDepth)int.MaxValue);

            walker1.Visit(syntax);
            walker2.Visit(syntax);

            Assert.NotEmpty(walker1.VisitedNodes);

            Assert.Equal(walker1.VisitedNodes, walker2.VisitedNodes);
        }

        [Fact]
        public void WalkingWithHigherDepthYieldsMoreResults()
        {
            var syntax = GenerateWalkingTestSyntax();

            var lastResultSet = new List<object?>();

            foreach (var depth in Enum.GetValues(typeof(SyntaxWalkerDepth)).OfType<SyntaxWalkerDepth>().OrderBy(i => (int)i))
            {
                var walker = new RecordingWalker(SyntaxWalkerDepth.StructuredTrivia);
                walker.Visit(syntax);

                var result = walker.VisitedNodes;

                foreach (var element in lastResultSet)
                {
                    Assert.Contains(element, result);
                }

                lastResultSet = result;
            }
        }

        [Fact]
        public void WalkingOrderIsCorrect()
        {
            //We test this by reconstructing the original source from tokens.

            //NOTE: We do this on the troken level instead of the trivia level because the implementation
            //      of the walkers visits tokens before visiting their leading and trailing trivias.

            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker(SyntaxWalkerDepth.Token);

            walker.Visit(syntax);

            var stringBuilder = new StringBuilder();

            foreach (var visited in walker.VisitedNodes)
            {
                if (visited is SyntaxToken token)
                {
                    stringBuilder.Append(token.ToFullString());
                }
            }

            var originalText = syntax.ToString();
            var rebuiltText = stringBuilder.ToString();

            Assert.Equal(originalText, rebuiltText);
        }

        #endregion

        #region CSharpSyntaxWalker<TArgument>


        [Fact]
        public void WalkWithDepthNode_TArgument()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker<int>(SyntaxWalkerDepth.Node, 3);

            walker.Visit(syntax, 3);

            Assert.NotEmpty(walker.VisitedNodes);

            foreach (var visited in walker.VisitedNodes)
            {
                Assert.False(visited is SyntaxNode node && node.IsPartOfStructuredTrivia(), "Should not visit structured trivia.");
                Assert.False(visited is SyntaxToken, "Should not visit tokens.");
                Assert.False(visited is SyntaxTrivia, "Should not visit trivia.");
            }
        }


        [Fact]
        public void WalkWithDepthToken_TArgument()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker<int>(SyntaxWalkerDepth.Token, 3);

            walker.Visit(syntax, 3);

            Assert.NotEmpty(walker.VisitedNodes);

            foreach (var visited in walker.VisitedNodes)
            {
                Assert.False(visited is SyntaxNode node && node.IsPartOfStructuredTrivia(), "Should not visit structured trivia.");
                Assert.False(visited is SyntaxTrivia, "Should not visit trivia.");
            }
        }


        [Fact]
        public void WalkWithDepthTrivia_TArgument()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker<int>(SyntaxWalkerDepth.Trivia, 3);

            walker.Visit(syntax, 3);

            Assert.NotEmpty(walker.VisitedNodes);

            foreach (var visited in walker.VisitedNodes)
            {
                Assert.False(visited is SyntaxNode node && node.IsPartOfStructuredTrivia(), "Should not visit structured trivia.");
            }
        }

        [Fact]
        public void WalkWithNegativeDepthOnlyVisitsRoot_TArgument()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker<int>((SyntaxWalkerDepth)int.MinValue, 3);

            walker.Visit(syntax, 3);

            var visited = Assert.Single(walker.VisitedNodes);
            Assert.Same(syntax, visited);
        }

        [Fact]
        public void WalkWithMaximumDepthIsSameAsStructuredTriviaDepth_TArgument()
        {
            var syntax = GenerateWalkingTestSyntax();
            var walker1 = new RecordingWalker<int>(SyntaxWalkerDepth.StructuredTrivia, 3);
            var walker2 = new RecordingWalker<int>((SyntaxWalkerDepth)int.MaxValue, 3);

            walker1.Visit(syntax, 3);
            walker2.Visit(syntax, 3);

            Assert.NotEmpty(walker1.VisitedNodes);

            Assert.Equal(walker1.VisitedNodes, walker2.VisitedNodes);
        }

        [Fact]
        public void WalkingWithHigherDepthYieldsMoreResults_TArgument()
        {
            var syntax = GenerateWalkingTestSyntax();

            var lastResultSet = new List<object?>();

            foreach (var depth in Enum.GetValues(typeof(SyntaxWalkerDepth)).OfType<SyntaxWalkerDepth>().OrderBy(i => (int)i))
            {
                var walker = new RecordingWalker<int>(SyntaxWalkerDepth.StructuredTrivia, 3);
                walker.Visit(syntax, 3);

                var result = walker.VisitedNodes;

                foreach (var element in lastResultSet)
                {
                    Assert.Contains(element, result);
                }

                lastResultSet = result;
            }
        }

        [Fact]
        public void WalkingOrderIsCorrect_TArgument()
        {
            //We test this by reconstructing the original source from tokens.

            //NOTE: We do this on the troken level instead of the trivia level because the implementation
            //      of the walkers visits tokens before visiting their leading and trailing trivias.

            var syntax = GenerateWalkingTestSyntax();
            var walker = new RecordingWalker<int>(SyntaxWalkerDepth.Token, 3);

            walker.Visit(syntax, 3);

            var stringBuilder = new StringBuilder();

            foreach (var visited in walker.VisitedNodes)
            {
                if (visited is SyntaxToken token)
                {
                    stringBuilder.Append(token.ToFullString());
                }
            }

            var originalText = syntax.ToString();
            var rebuiltText = stringBuilder.ToString();

            Assert.Equal(originalText, rebuiltText);
        }

        #endregion

        #region Misc

        internal class DefaultVisitor : CSharpSyntaxVisitor
        {
            public bool DefaultVisitWasCalled { get; private set; }

            public override void DefaultVisit(SyntaxNode node)
            {
                DefaultVisitWasCalled = true;
            }
        }

        internal class DefaultVisitor<TResult> : CSharpSyntaxVisitor<TResult>
        {
            public bool DefaultVisitWasCalled { get; private set; }

            private readonly TResult _returnValue;

            public DefaultVisitor(TResult returnValue)
            {
                _returnValue = returnValue;
            }

            public override TResult DefaultVisit(SyntaxNode node)
            {
                DefaultVisitWasCalled = true;
                return _returnValue;
            }
        }

        internal class DefaultVisitor<TArgument, TResult> : CSharpSyntaxVisitor<TArgument, TResult>
        {
            public bool DefaultVisitWasCalled { get; private set; }

            private readonly TResult _returnValue;

            public DefaultVisitor(TResult returnValue)
            {
                _returnValue = returnValue;
            }

            public override TResult DefaultVisit(SyntaxNode node, TArgument argument)
            {
                DefaultVisitWasCalled = true;
                return _returnValue;
            }
        }

        /// <summary>
        /// Returns a non-trivial syntax tree that should lead a walker to walk different paths depending on its <see cref="SyntaxWalkerDepth"/>.
        /// </summary>
        /// <returns></returns>
        private static SyntaxNode GenerateWalkingTestSyntax()
        {
            return SyntaxFactory.ParseCompilationUnit(
                $$"""
                namespace A;
                        
                /// <summary>
                /// This is a doc-comment on class <see cref="B"/>.
                /// </summary>
                class B
                {
                    //regular comment

                    #if true
                
                    /// <summary>
                    /// This is a doc-comment on method <see cref="C"/>.
                    /// </summary>
                    int C(int a)
                    {
                        return a + 1;
                    }

                    #endif

                    #if false
                
                    int D(int a)
                    {
                        return a + 1;
                    }

                    #endif
                
                    int E(int a)
                    {
                        return F();

                        int F() => a + 1;
                    }
                }
                """);
        }


        private class RecordingWalker : CSharpSyntaxWalker
        {
            public List<object?> VisitedNodes { get; } = new();

            public RecordingWalker(SyntaxWalkerDepth depth) : base(depth)
            {

            }

            public override void Visit(SyntaxNode? node)
            {
                VisitedNodes.Add(node);

                base.Visit(node);
            }

            public override void VisitToken(SyntaxToken token)
            {
                VisitedNodes.Add(token);

                base.VisitToken(token);
            }

            public override void VisitTrivia(SyntaxTrivia trivia)
            {
                VisitedNodes.Add(trivia);

                base.VisitTrivia(trivia);
            }
        }

        private class RecordingWalker<TArgument> : CSharpSyntaxWalker<TArgument>
        {
            public List<object?> VisitedNodes { get; } = new();
            private readonly TArgument _expectedArgument;

            public RecordingWalker(SyntaxWalkerDepth depth, TArgument expectedArgument) : base(depth)
            {
                _expectedArgument = expectedArgument;
            }

            public override object? Visit(SyntaxNode? node, TArgument argument)
            {
                VisitedNodes.Add(node);
                Assert.Equal(_expectedArgument, argument);

                return base.Visit(node, argument);
            }

            public override object? VisitToken(SyntaxToken token, TArgument argument)
            {
                VisitedNodes.Add(token);
                Assert.Equal(_expectedArgument, argument);

                return base.VisitToken(token, argument);
            }

            public override object? VisitTrivia(SyntaxTrivia trivia, TArgument argument)
            {
                VisitedNodes.Add(trivia);
                Assert.Equal(_expectedArgument, argument);

                return base.VisitTrivia(trivia, argument);
            }
        }

        #endregion
    }
}
