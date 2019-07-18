// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Test.Utilities;
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
            var source1 = @"
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

            var source2 = @"
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

            SyntaxUtilities.FindLeafNodeAndPartner(leftRoot, leftPosition, rightRoot, out var leftNode, out var rightNodeOpt);
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

            SyntaxUtilities.FindLeafNodeAndPartner(leftRoot, leftPosition, rightRoot, out var leftNode, out var rightNodeOpt);
            Assert.Equal("3", leftNode.ToString());
            Assert.Null(rightNodeOpt);
        }

        [Fact]
        public void IsIteratorDeclaration()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    IEnumerable<int> X = new[] { 1, 2, 3 };

    IEnumerable<int> M1() { yield return 1; }
    
    void M2() 
    {
        IEnumerable<int> f() { yield return 1; }
    }
}
");

            var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(m => m.Identifier.ValueText == "X");
            var m1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M1");
            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M2");
            var f = m2.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single(m => m.Identifier.ValueText == "f");

            Assert.False(SyntaxUtilities.IsIteratorDeclaration(x));
            Assert.True(SyntaxUtilities.IsIteratorDeclaration(m1));
            Assert.False(SyntaxUtilities.IsIteratorDeclaration(m2));
            Assert.True(SyntaxUtilities.IsIteratorDeclaration(f));

            Assert.Equal(0, SyntaxUtilities.GetYieldStatements(x.Initializer).Length);
            Assert.Equal(1, SyntaxUtilities.GetYieldStatements(m1.Body).Length);
            Assert.Equal(0, SyntaxUtilities.GetYieldStatements(m2.Body).Length);
            Assert.Equal(1, SyntaxUtilities.GetYieldStatements(f.Body).Length);
        }

        [Fact]
        public void IsAsyncDeclaration()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    async Task<int> M1() => await Task.FromResult(1);
    async Task<int> M2() { return await Task.FromResult(1); }

    void M3()
    {
        async Task<int> f1() => await Task.FromResult(1);
        async Task<int> f2() { return await Task.FromResult(1); }

        var l1 = new Func<Task<int>>(async () => await Task.FromResult(1));
        var l2 = new Func<Task<int>>(async () => { return await Task.FromResult(1); });

        var l3 = new Func<Task<int>>(async delegate () { return await Task.FromResult(1); });
    }
}
");

            var m1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M1");
            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M2");
            var m3 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M3");

            var f1 = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single(m => m.Identifier.ValueText == "f1");
            var f2 = tree.GetRoot().DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single(m => m.Identifier.ValueText == "f2");

            var l1 = m3.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(m => m.Identifier.ValueText == "l1").Initializer.
                DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

            var l2 = m3.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(m => m.Identifier.ValueText == "l2").Initializer.
                DescendantNodes().OfType<LambdaExpressionSyntax>().Single();

            var l3 = m3.DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(m => m.Identifier.ValueText == "l3").Initializer.
                DescendantNodes().OfType<AnonymousFunctionExpressionSyntax>().Single();

            Assert.True(SyntaxUtilities.IsAsyncDeclaration(m1.ExpressionBody));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(m2));
            Assert.False(SyntaxUtilities.IsAsyncDeclaration(m3));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(f1.ExpressionBody));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(f2));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(l1));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(l2));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(l3));

            Assert.Equal(1, SyntaxUtilities.GetAwaitExpressions(m1.ExpressionBody).Length);
            Assert.Equal(1, SyntaxUtilities.GetAwaitExpressions(m2.Body).Length);
            Assert.Equal(0, SyntaxUtilities.GetAwaitExpressions(m3.Body).Length);
            Assert.Equal(1, SyntaxUtilities.GetAwaitExpressions(f1.ExpressionBody).Length);
            Assert.Equal(1, SyntaxUtilities.GetAwaitExpressions(f2.Body).Length);
            Assert.Equal(1, SyntaxUtilities.GetAwaitExpressions(l1.Body).Length);
            Assert.Equal(1, SyntaxUtilities.GetAwaitExpressions(l2.Body).Length);
            Assert.Equal(1, SyntaxUtilities.GetAwaitExpressions(l3.Body).Length);
        }
    }
}
