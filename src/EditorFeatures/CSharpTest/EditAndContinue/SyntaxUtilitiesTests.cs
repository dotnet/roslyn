// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using SyntaxUtilities = Microsoft.CodeAnalysis.CSharp.EditAndContinue.SyntaxUtilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue
{
    public class SyntaxUtilitiesTests
    {
        private void VerifySyntaxMap(string oldSource, string newSource)
        {
            var oldRoot = SyntaxFactory.ParseSyntaxTree(oldSource).GetRoot();
            var newRoot = SyntaxFactory.ParseSyntaxTree(newSource).GetRoot();

            foreach (var oldNode in oldRoot.DescendantNodes().Where(n => n.FullSpan.Length > 0))
            {
                var newNode = SyntaxUtilities.FindPartner(oldRoot, newRoot, oldNode);
                Assert.True(SyntaxFactory.AreEquivalent(oldNode, newNode), $"Node '{oldNode}' not equivalent to '{newNode}'.");
            }
        }

        [Fact]
        public void FindPartner1()
        {
            string source1 = @"
using System;

class C
{
    static void Main(string[] args)
    {

        // sdasd
        var b = true;
        do
        {
            Console.WriteLine(""hi"");
        } while (b == true);
    }
}
";

            string source2 = @"
using System;

class C
{
    static void Main(string[] args)
    {
        var b = true;
        do
        {
            Console.WriteLine(""hi"");
        } while (b == true);
    }
}
";
            VerifySyntaxMap(source1, source2);
        }

        [Fact]
        public void FindLeafNodeAndPartner1()
        {
            var leftRoot = SyntaxFactory.ParseSyntaxTree(@"
using System;

class C
{
    public void M()
    {
        if (0 == 1)
        {
            Console.WriteLine(0);
        }
    }
}
").GetRoot();
            var leftPosition = leftRoot.DescendantNodes().OfType<LiteralExpressionSyntax>().ElementAt(2).SpanStart; // 0 within Console.WriteLine(0)
            var rightRoot = SyntaxFactory.ParseSyntaxTree(@"
using System;

class C
{
    public void M()
    {
        if (0 == 1)
        {
            if (2 == 3)
            {
                Console.WriteLine(0);
            }
        }
    }
}
").GetRoot();

            SyntaxUtilities.FindLeafNodeAndPartner(leftRoot, leftPosition, rightRoot, out SyntaxNode leftNode, out SyntaxNode rightNodeOpt);
            Assert.Equal("0", leftNode.ToString());
            Assert.Null(rightNodeOpt);
        }

        [Fact]
        public void FindLeafNodeAndPartner2()
        {
            // Check that the method does not fail even if the index of the child (4) 
            // is greater than the count of children on the corresponding (from the upper side) node (3).
            var leftRoot = SyntaxFactory.ParseSyntaxTree(@"
using System;

class C
{
    public void M()
    {
        if (0 == 1)
        {
            Console.WriteLine(0);
            Console.WriteLine(1);
            Console.WriteLine(2);
            Console.WriteLine(3);
        }
    }
}
").GetRoot();

            var leftPosition = leftRoot.DescendantNodes().OfType<LiteralExpressionSyntax>().ElementAt(5).SpanStart; // 3 within Console.WriteLine(3)
            var rightRoot = SyntaxFactory.ParseSyntaxTree(@"
using System;

class C
{
    public void M()
    {
        if (0 == 1)
        {
            if (2 == 3)
            {
                Console.WriteLine(0);
                Console.WriteLine(1);
                Console.WriteLine(2);
                Console.WriteLine(3);
            }
        }
    }
}
").GetRoot();

            SyntaxUtilities.FindLeafNodeAndPartner(leftRoot, leftPosition, rightRoot, out SyntaxNode leftNode, out SyntaxNode rightNodeOpt);
            Assert.Equal("3", leftNode.ToString());
            Assert.Null(rightNodeOpt);
        }
    }
}
