// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EditAndContinue;
using Roslyn.Test.Utilities;
using Xunit;
using SyntaxUtilities = Microsoft.CodeAnalysis.CSharp.EditAndContinue.SyntaxUtilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EditAndContinue
{
    public class SyntaxUtilitiesTests
    {
        private static void VerifySyntaxMap(string oldSource, string newSource)
        {
            var oldRoot = SyntaxFactory.ParseSyntaxTree(oldSource).GetRoot();
            var newRoot = SyntaxFactory.ParseSyntaxTree(newSource).GetRoot();

            foreach (var oldNode in oldRoot.DescendantNodes().Where(n => n.FullSpan.Length > 0))
            {
                var newNode = AbstractEditAndContinueAnalyzer.FindPartner(newRoot, oldRoot, oldNode);
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

            AbstractEditAndContinueAnalyzer.FindLeafNodeAndPartner(leftRoot, leftPosition, rightRoot, out var leftNode, out var rightNodeOpt);
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

            AbstractEditAndContinueAnalyzer.FindLeafNodeAndPartner(leftRoot, leftPosition, rightRoot, out var leftNode, out var rightNodeOpt);
            Assert.Equal("3", leftNode.ToString());
            Assert.Null(rightNodeOpt);
        }

        [Fact]
        public void IsAsyncDeclaration()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    async Task<int> M0() => 1;
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

            var m0 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M0");
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

            Assert.True(SyntaxUtilities.IsAsyncDeclaration(m0.ExpressionBody));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(m1.ExpressionBody));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(m2));
            Assert.False(SyntaxUtilities.IsAsyncDeclaration(m3));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(f1.ExpressionBody));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(f2));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(l1));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(l2));
            Assert.True(SyntaxUtilities.IsAsyncDeclaration(l3));

            Assert.Equal(0, SyntaxUtilities.GetSuspensionPoints(m0.ExpressionBody).Count());
            Assert.Equal(1, SyntaxUtilities.GetSuspensionPoints(m1.ExpressionBody).Count());
            Assert.Equal(1, SyntaxUtilities.GetSuspensionPoints(m2.Body).Count());
            Assert.Equal(0, SyntaxUtilities.GetSuspensionPoints(m3.Body).Count());
            Assert.Equal(1, SyntaxUtilities.GetSuspensionPoints(f1.ExpressionBody).Count());
            Assert.Equal(1, SyntaxUtilities.GetSuspensionPoints(f2.Body).Count());
            Assert.Equal(1, SyntaxUtilities.GetSuspensionPoints(l1.Body).Count());
            Assert.Equal(1, SyntaxUtilities.GetSuspensionPoints(l2.Body).Count());
            Assert.Equal(1, SyntaxUtilities.GetSuspensionPoints(l3.Body).Count());
        }

        [Fact]
        public void GetSuspensionPoints()
        {
            var tree = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    IEnumerable<int> X = new[] { 1, 2, 3 };

    IEnumerable<int> M1() { yield return 1; }
    
    void M2() 
    {
        IAsyncEnumerable<int> f() 
        {
            yield return 1;

            yield break;

            await Task.FromResult(1);

            await foreach (var x in F()) { }

            await foreach (var (x, y) in F()) { }

            await using T x1 = F1(), x2 = F2(), x3 = F3();
        }
    }
}
");

            var x = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(m => m.Identifier.ValueText == "X");
            var m1 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M1");
            var m2 = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(m => m.Identifier.ValueText == "M2");
            var f = m2.DescendantNodes().OfType<LocalFunctionStatementSyntax>().Single(m => m.Identifier.ValueText == "f");

            AssertEx.Empty(SyntaxUtilities.GetSuspensionPoints(x.Initializer));
            AssertEx.Equal(new[] { "yield return 1;" }, SyntaxUtilities.GetSuspensionPoints(m1.Body).Select(n => n.ToString()));
            AssertEx.Empty(SyntaxUtilities.GetSuspensionPoints(m2.Body));

            AssertEx.Equal(new[]
            {
                "yield return 1;",
                "await Task.FromResult(1)",
                "await foreach (var x in F()) { }",
                "await foreach (var (x, y) in F()) { }",
                "x1 = F1()",
                "x2 = F2()",
                "x3 = F3()",
            }, SyntaxUtilities.GetSuspensionPoints(f.Body).Select(n => n.ToString()));
        }
    }
}
