// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxDiffingTests
    {
        [Fact]
        public void TestDiffEmptyVersusClass()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("");
            var newTree = SyntaxFactory.ParseSyntaxTree("class C { }");

            // it should be all new
            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(1, spans.Count);
            Assert.Equal(newTree.GetCompilationUnitRoot().FullSpan, spans[0]);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(0, 0), changes[0].Span);
            Assert.Equal("class C { }", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassWithNameChanged()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { }");
            var newTree = SyntaxFactory.ParseSyntaxTree("class B { }");

            // since most tokens are automatically interned we should see only the name tokens change
            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(1, spans.Count);
            var decl = (TypeDeclarationSyntax)(newTree.GetCompilationUnitRoot()).Members[0];
            Assert.Equal(decl.Identifier.Span, spans[0]);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(6, 1), changes[0].Span);
            Assert.Equal("B", changes[0].NewText);
        }

        [Fact]
        public void TestDiffTwoClassesWithBothNamesChanged()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { } class B { }");
            var newTree = SyntaxFactory.ParseSyntaxTree("class C { } class D { }");

            // since most tokens are automatically interned we should see only the name tokens change
            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(2, spans.Count);
            var decl1 = (TypeDeclarationSyntax)(newTree.GetCompilationUnitRoot()).Members[0];
            Assert.Equal(decl1.Identifier.Span, spans[0]);
            var decl2 = (TypeDeclarationSyntax)(newTree.GetCompilationUnitRoot()).Members[1];
            Assert.Equal(decl2.Identifier.Span, spans[1]);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(2, changes.Count);
            Assert.Equal(new TextSpan(6, 1), changes[0].Span);
            Assert.Equal("C", changes[0].NewText);
            Assert.Equal(new TextSpan(18, 1), changes[1].Span);
            Assert.Equal("D", changes[1].NewText);
        }

        [Fact]
        public void TestDiffClassWithNewClassStarted()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { }");
            var newTree = oldTree.WithInsertAt(0, "class ");

            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(1, spans.Count);
            Assert.Equal(new TextSpan(0, 6), spans[0]);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(0, 0), changes[0].Span);
            Assert.Equal("class ", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassWithNewClassStarted2()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { }");
            var newTree = oldTree.WithInsertAt(0, "class A ");

            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(1, spans.Count);
            Assert.Equal(new TextSpan(0, 8), spans[0]);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(0, 0), changes[0].Span);
            Assert.Equal("class A ", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassWithNewClassStarted3()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { }");
            var newTree = oldTree.WithInsertAt(0, "class A { }");

            // new tree appears to have two duplicate (similar) copies of the same declarations (indistinguishable)
            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(1, spans.Count);
            Assert.Equal(new TextSpan(11, 11), spans[0]); // its going to pick one of the two spans.

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(11, 0), changes[0].Span);
            Assert.Equal("class A { }", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassWithNewClassStarted4()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { }");
            var newTree = oldTree.WithInsertAt(0, "class A { } ");

            // new tree appears to have two almost duplicate (similar) copies of the same declarations, except the
            // second (original) one is a closer match
            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(1, spans.Count);
            Assert.Equal(new TextSpan(10, 12), spans[0]);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(10, 0), changes[0].Span);
            Assert.Equal("} class A { ", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassWithNewNamespaceEnclosing()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { }");
            var newTree = oldTree.WithInsertAt(0, "namespace N { ");

            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(1, spans.Count);
            Assert.Equal(new TextSpan(0, 14), spans[0]);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(0, 0), changes[0].Span);
            Assert.Equal("namespace N { ", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassWithNewMemberInserted()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { }");
            var newTree = oldTree.WithInsertAt(10, "int X; ");

            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(1, spans.Count);
            Assert.Equal(new TextSpan(10, 7), spans[0]); // int X;

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(10, 0), changes[0].Span);
            Assert.Equal("int X; ", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassWithMemberRemoved()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { int X; }");
            var newTree = oldTree.WithRemoveAt(10, 7);

            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(0, spans.Count);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(10, 7), changes[0].Span);
            Assert.Equal("", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassWithMemberRemovedDeep()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("namespace N { class A { int X; } }");
            var newTree = oldTree.WithRemoveAt(24, 7);

            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(0, spans.Count);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(24, 7), changes[0].Span);
            Assert.Equal("", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassWithMemberNameRemoved()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { int X; }");
            var newTree = oldTree.WithRemoveAt(14, 1);

            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(0, spans.Count);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(14, 1), changes[0].Span);
            Assert.Equal("", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassChangedToStruct()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("namespace N { class A { int X; } }");
            var newTree = oldTree.WithReplaceFirst("class", "struct");

            var spans = newTree.GetChangedSpans(oldTree);
            Assert.NotNull(spans);
            Assert.Equal(1, spans.Count);
            Assert.Equal(new TextSpan(14, 6), spans[0]); // 'struct'

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal(new TextSpan(14, 5), changes[0].Span);
            Assert.Equal("struct", changes[0].NewText);
        }

        [Fact]
        public void TestDiffClassChangedToStruct2()
        {
            var oldTree = SyntaxFactory.ParseSyntaxTree("class A { }");
            // the trailing space is necessary, so that ReduceChanges doesn't help
            var newTree = SyntaxFactory.ParseSyntaxTree("struct A { } ");

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(2, changes.Count);
            Assert.Equal(new TextSpan(0, 5), changes[0].Span);
            Assert.Equal("struct", changes[0].NewText);
            Assert.Equal(new TextSpan(11, 0), changes[1].Span);
            Assert.Equal(" ", changes[1].NewText);
        }

        [Fact]
        public void TestDiffMinusChangedToPlus()
        {
            var options = new CSharpParseOptions(kind: SourceCodeKind.Script);
            // note: PostIncrementExpression and PostDecrementExpression are both PostfixUnaryExpressionSyntax
            var oldTree = SyntaxFactory.ParseSyntaxTree("a.b++", options);
            var newTree = SyntaxFactory.ParseSyntaxTree("c.b--", options);

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(2, changes.Count);
            Assert.Equal(new TextSpan(0, 1), changes[0].Span);
            Assert.Equal("c", changes[0].NewText);
            Assert.Equal(new TextSpan(3, 2), changes[1].Span);
            Assert.Equal("--", changes[1].NewText);
        }

        [Fact, WorkItem(463, "https://github.com/dotnet/roslyn/issues/463")]
        public void TestQualifyWithThis()
        {
            var original = @"
class C
{
    int Sign;
    void F()
    {
        string x = @""namespace Namespace
    {
        class Type
        {
            void Goo()
            {
                int x = 1 "" + Sign + @"" "" + Sign + @""3;
            }
        }
    }
"";
    }
}";
            var oldTree = SyntaxFactory.ParseSyntaxTree(original);
            var root = oldTree.GetRoot();

            var indexText = "Sign +";

            // Expected behavior: Qualifying identifier 'Sign' with 'this.' and doing a diff between trees 
            // should return a single text change with 'this.' as added text.

            // Works as expected for last index
            var index = original.LastIndexOf(indexText, StringComparison.Ordinal);
            TestQualifyWithThisCore(root, index);

            // Doesn't work as expected for first index.
            // It returns 2 changes with add followed by delete, 
            // causing the 2 isolated edits of adding "this." to seem conflicting edits, even though they are not.
            // See https://github.com/dotnet/roslyn/issues/320 for details.
            index = original.IndexOf(indexText, StringComparison.Ordinal);
            TestQualifyWithThisCore(root, index);
        }

        private void TestQualifyWithThisCore(SyntaxNode root, int index)
        {
            var oldTree = root.SyntaxTree;

            var span = new TextSpan(index, 4);
            var node = root.FindNode(span, getInnermostNodeForTie: true) as SimpleNameSyntax;
            Assert.NotNull(node);
            Assert.Equal("Sign", node.Identifier.ValueText);

            var leadingTrivia = node.GetLeadingTrivia();
            var newNode = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.ThisExpression(),
                node.WithoutLeadingTrivia())
                .WithLeadingTrivia(leadingTrivia);

            var newRoot = root.ReplaceNode(node, newNode);
            var newTree = newRoot.SyntaxTree;

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal("this.", changes[0].NewText);
        }

        [Fact, WorkItem(463, "https://github.com/dotnet/roslyn/issues/463")]
        public void TestReplaceWithBuiltInType()
        {
            var original = @"
using System;
using System.Collections.Generic;

public class TestClass
{
    public void TestMethod()
    {
        var dictionary = new Dictionary<Object, Object>();
        dictionary[new Object()] = new Object();
    }
}";
            var oldTree = SyntaxFactory.ParseSyntaxTree(original);
            var root = oldTree.GetRoot();

            var indexText = "Object";

            // Expected behavior: Replacing identifier 'Object' with 'object' and doing a diff between trees 
            // should return a single text change for character replace.

            // Works as expected for first index
            var index = original.IndexOf(indexText, StringComparison.Ordinal);
            TestReplaceWithBuiltInTypeCore(root, index);

            // Works as expected for last index
            index = original.LastIndexOf(indexText, StringComparison.Ordinal);
            TestReplaceWithBuiltInTypeCore(root, index);

            // Doesn't work as expected for the third index.
            // It returns 2 changes with add followed by delete, 
            // causing the 2 isolated edits to seem conflicting edits, even though they are not.
            // See https://github.com/dotnet/roslyn/issues/320 for details.
            indexText = "Object()";
            index = original.IndexOf(indexText, StringComparison.Ordinal);
            TestReplaceWithBuiltInTypeCore(root, index);
        }

        private void TestReplaceWithBuiltInTypeCore(SyntaxNode root, int index)
        {
            var oldTree = root.SyntaxTree;

            var span = new TextSpan(index, 6);
            var node = root.FindNode(span, getInnermostNodeForTie: true) as SimpleNameSyntax;
            Assert.NotNull(node);
            Assert.Equal("Object", node.Identifier.ValueText);

            var leadingTrivia = node.GetLeadingTrivia();
            var newNode = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))
                .WithLeadingTrivia(leadingTrivia);

            var newRoot = root.ReplaceNode(node, newNode);
            var newTree = newRoot.SyntaxTree;

            var changes = newTree.GetChanges(oldTree);
            Assert.NotNull(changes);
            Assert.Equal(1, changes.Count);
            Assert.Equal("o", changes[0].NewText);
        }
    }
}
