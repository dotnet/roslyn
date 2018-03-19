// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using InternalSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class TrackNodeTests
    {
        [Fact]
        public void TestGetCurrentNodeAfterTrackNodesReturnsCurrentNode()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax a = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            ExpressionSyntax trackedExpr = expr.TrackNodes(a);
            IdentifierNameSyntax currentA = trackedExpr.GetCurrentNode(a);
            Assert.NotNull(currentA);
            Assert.Equal("a", currentA.ToString());
        }

        [Fact]
        public void TestGetCurrentNodesAfterTrackNodesReturnsSingletonSequence()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax a = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            ExpressionSyntax trackedExpr = expr.TrackNodes(a);
            IEnumerable<IdentifierNameSyntax> currentAs = trackedExpr.GetCurrentNodes(a);
            Assert.NotNull(currentAs);
            Assert.Equal(1, currentAs.Count());
            Assert.Equal("a", currentAs.ElementAt(0).ToString());
        }

        [Fact]
        public void TestGetCurrentNodeWithoutTrackNodesReturnsNull()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax a = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            IdentifierNameSyntax currentA = expr.GetCurrentNode(a);
            Assert.Null(currentA);
        }

        [Fact]
        public void TestGetCurrentNodesWithoutTrackNodesReturnsEmptySequence()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax a = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            IEnumerable<IdentifierNameSyntax> currentAs = expr.GetCurrentNodes(a);
            Assert.NotNull(currentAs);
            Assert.Equal(0, currentAs.Count());
        }

        [Fact]
        public void TestGetCurrentNodeAfterEditReturnsCurrentNode()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax originalA = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            ExpressionSyntax trackedExpr = expr.TrackNodes(originalA);
            IdentifierNameSyntax currentA = trackedExpr.GetCurrentNode(originalA);
            IdentifierNameSyntax newA = currentA.WithLeadingTrivia(SyntaxFactory.Comment("/* ayup */"));
            ExpressionSyntax replacedExpr = trackedExpr.ReplaceNode(currentA, newA);
            IdentifierNameSyntax latestA = replacedExpr.GetCurrentNode(originalA);
            Assert.NotNull(latestA);
            Assert.NotSame(latestA, newA); // not the same reference
            Assert.Equal(newA.ToFullString(), latestA.ToFullString());
        }

        [Fact]
        public void TestGetCurrentNodeAfterEditReturnsSingletonSequence()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax originalA = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            ExpressionSyntax trackedExpr = expr.TrackNodes(originalA);
            IdentifierNameSyntax currentA = trackedExpr.GetCurrentNode(originalA);
            IdentifierNameSyntax newA = currentA.WithLeadingTrivia(SyntaxFactory.Comment("/* ayup */"));
            ExpressionSyntax replacedExpr = trackedExpr.ReplaceNode(currentA, newA);
            IEnumerable<IdentifierNameSyntax> latestAs = replacedExpr.GetCurrentNodes(originalA);
            Assert.NotNull(latestAs);
            Assert.Equal(1, latestAs.Count());
            Assert.Equal(newA.ToFullString(), latestAs.ElementAt(0).ToFullString());
        }

        [WorkItem(1070667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1070667")]
        [Fact]
        public void TestGetCurrentNodeAfterRemovalReturnsNull()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax originalA = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            ExpressionSyntax trackedExpr = expr.TrackNodes(originalA);
            IdentifierNameSyntax currentA = trackedExpr.GetCurrentNode(originalA);
            ExpressionSyntax replacedExpr = trackedExpr.ReplaceNode(currentA, SyntaxFactory.IdentifierName("c"));
            IdentifierNameSyntax latestA = replacedExpr.GetCurrentNode(originalA);
            Assert.Null(latestA);
        }

        [Fact]
        public void TestGetCurrentNodesAfterRemovalEmptySequence()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax originalA = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            ExpressionSyntax trackedExpr = expr.TrackNodes(originalA);
            IdentifierNameSyntax currentA = trackedExpr.GetCurrentNode(originalA);
            ExpressionSyntax replacedExpr = trackedExpr.ReplaceNode(currentA, SyntaxFactory.IdentifierName("c"));
            IEnumerable<IdentifierNameSyntax> latestAs = replacedExpr.GetCurrentNodes(originalA);
            Assert.NotNull(latestAs);
            Assert.Equal(0, latestAs.Count());
        }

        [Fact]
        public void TestGetCurrentNodeAfterAddingMultipleThrows()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax originalA = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            ExpressionSyntax trackedExpr = expr.TrackNodes(originalA);
            IdentifierNameSyntax currentA = trackedExpr.GetCurrentNode(originalA);
            // replace all identifiers with same 'a'
            ExpressionSyntax replacedExpr = trackedExpr.ReplaceNodes(trackedExpr.DescendantNodes().OfType<IdentifierNameSyntax>(), (original, changed) => currentA);
            Assert.Throws<InvalidOperationException>(() => replacedExpr.GetCurrentNode(originalA));
        }

        [Fact]
        public void TestGetCurrentNodeAfterAddingMultipleReturnsMultiple()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            IdentifierNameSyntax originalA = expr.DescendantNodes().OfType<IdentifierNameSyntax>().First(n => n.Identifier.Text == "a");
            ExpressionSyntax trackedExpr = expr.TrackNodes(originalA);
            IdentifierNameSyntax currentA = trackedExpr.GetCurrentNode(originalA);
            // replace all identifiers with same 'a'
            ExpressionSyntax replacedExpr = trackedExpr.ReplaceNodes(trackedExpr.DescendantNodes().OfType<IdentifierNameSyntax>(), (original, changed) => currentA);
            var nodes = replacedExpr.GetCurrentNodes(originalA).ToList();
            Assert.Equal(2, nodes.Count);
            Assert.Equal("a", nodes[0].ToString());
            Assert.Equal("a", nodes[1].ToString());
        }

        [Fact]
        public void TestTrackNodesWithMultipleTracksAllNodes()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b + c");
            var ids = expr.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();
            ExpressionSyntax trackedExpr = expr.TrackNodes(ids);

            Assert.Equal(3, ids.Count);

            foreach (IdentifierNameSyntax id in ids)
            {
                IdentifierNameSyntax currentId = trackedExpr.GetCurrentNode(id);
                Assert.NotNull(currentId);
                Assert.NotSame(id, currentId);
                Assert.Equal(id.ToString(), currentId.ToString());
            }
        }

        [Fact]
        public void TestTrackNodesWithNoNodesTracksNothing()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b + c");
            var ids = expr.DescendantNodes().OfType<IdentifierNameSyntax>().ToList();

            ExpressionSyntax trackedExpr = expr.TrackNodes();

            Assert.Equal(3, ids.Count);

            foreach (IdentifierNameSyntax id in ids)
            {
                IdentifierNameSyntax currentId = trackedExpr.GetCurrentNode(id);
                Assert.Null(currentId);
            }
        }

        [Fact]
        public void TestTrackNodeThatIsNotInTheSubtreeThrows()
        {
            ExpressionSyntax expr = SyntaxFactory.ParseExpression("a + b");
            Assert.Throws<ArgumentException>(() => expr.TrackNodes(SyntaxFactory.IdentifierName("c")));
        }
    }
}
