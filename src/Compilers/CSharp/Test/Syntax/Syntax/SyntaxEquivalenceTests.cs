// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxEquivalenceTests
    {
        private void VerifyEquivalent(SyntaxTree tree1, SyntaxTree tree2, bool topLevel)
        {
            Assert.True(SyntaxFactory.AreEquivalent(tree1, tree2, topLevel));

            // now try as if the second tree were created from scratch.
            SyntaxTree tree3 = SyntaxFactory.ParseSyntaxTree(tree2.GetText().ToString());
            Assert.True(SyntaxFactory.AreEquivalent(tree1, tree3, topLevel));
        }

        private void VerifyNotEquivalent(SyntaxTree tree1, SyntaxTree tree2, bool topLevel)
        {
            Assert.False(SyntaxFactory.AreEquivalent(tree1, tree2, topLevel));

            // now try as if the second tree were created from scratch.
            SyntaxTree tree3 = SyntaxFactory.ParseSyntaxTree(tree2.GetText().ToString());
            Assert.False(SyntaxFactory.AreEquivalent(tree1, tree3, topLevel));
        }

        [Fact]
        public void TestEmptyTrees()
        {
            var text = "";
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree(text);
            SyntaxTree tree2 = SyntaxFactory.ParseSyntaxTree(text);

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingComment()
        {
            var text = "";
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree(text);
            SyntaxTree tree2 = tree1.WithInsertAt(0, "/* goo */");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingActivePPDirective()
        {
            var text = "";
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree(text);
            SyntaxTree tree2 = tree1.WithInsertAt(0, "#if true \r\n\r\n#endif");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingInactivePPDirective()
        {
            var text = "";
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree(text);
            SyntaxTree tree2 = tree1.WithInsertAt(0, "#if false \r\n\r\n#endif");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingEmpty()
        {
            var text = "";
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree(text);
            SyntaxTree tree2 = tree1.WithInsertAt(0, "namespace N { }");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingClass()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { }");
            SyntaxTree tree2 = tree1.WithInsertBefore("}", "class C { }");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRenameOuter()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("N", "N1");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRenameInner()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { int z = 0; } } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("z", "y");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRenameOuterToSamename()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("N", "N");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRenameInnerToSameName()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { int z = 0; } } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("z", "z");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingMethod()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { } }");
            SyntaxTree tree2 = tree1.WithInsertBefore("}", "void Goo() { } ");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingLocal()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { } } }");
            SyntaxTree tree2 = tree1.WithInsertBefore("}", "int i; ");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRemovingLocal()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { int i; } } }");
            SyntaxTree tree2 = tree1.WithRemoveFirst("int i;");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRemovingField1()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { int i = 5; int j = 6; } }");
            SyntaxTree tree2 = tree1.WithRemoveFirst("int i = 5;");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRemovingField2()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { int i = 5; int j = 6; } }");
            SyntaxTree tree2 = tree1.WithRemoveFirst("int j = 6;");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingField()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { int i = 5; } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingField2()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { int i = 5, j = 7; } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("7", "8");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingConstField()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { const int i = 5; } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingConstField2()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { const int i = 5, j = 7; } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);

            tree2 = tree1.WithReplaceFirst("7", "8");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingConstLocal()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { const int i = 5; } } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingEnumMember()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("enum E { i = 5 }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingAttribute()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { [Obsolete(true)]class C { const int i = 5; } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("true", "false");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingMethodCall()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { Console.Write(0); } } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("Write", "WriteLine");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingUsing()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("using System; namespace N { class C { void Goo() { Console.Write(0); } } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("System", "System.Linq");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingBaseType()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            SyntaxTree tree2 = tree1.WithInsertBefore("{", ": B ");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingMethodType()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("void", "int");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddComment()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            SyntaxTree tree2 = tree1.WithInsertBefore("class", "// Comment\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestCommentOutCode()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            SyntaxTree tree2 = tree1.WithInsertBefore("class", "// ");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddDocComment()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            SyntaxTree tree2 = tree1.WithInsertBefore("class", "/// Comment\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestCommentOutMethodCode()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("Console.Write(0);", "/* Console.Write(0); */");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestCommentOutMethod()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("void Goo() { }", "/* void Goo() { } */");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundMethodWithActivePPRegion()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("void Goo() { }", "\r\n#if true\r\n void Goo() { }\r\n#endif\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundMethodWithInactivePPRegion()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("void Goo() { }", "\r\n#if false\r\n void Goo() { }\r\n#endif\r\n");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundStatementWithActivePPRegion()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { int i; } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("int i;", "\r\n#if true\r\n int i;\r\n#endif\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundStatementWithInactivePPRegion()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { int i; } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("int i;", "\r\n#if false\r\n int i;\r\n#endif\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddLotsOfComments()
        {
            var text = "class C { void Goo() { int i; } }";
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { int i; } }");
            SyntaxTree tree2 = SyntaxFactory.ParseSyntaxTree(text.Replace(" ", " /**/ "));

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangeWhitespace()
        {
            var text = "class C { void Goo() { int i; } }";
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { int i; } }");
            SyntaxTree tree2 = SyntaxFactory.ParseSyntaxTree(text.Replace(" ", "  "));

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSkippedTest()
        {
            var text = "abc using";
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree(text);
            SyntaxTree tree2 = SyntaxFactory.ParseSyntaxTree(text.Replace("abc", "hello"));

            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUpdateInterpolatedString()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { Console.Write($\"Hello{123:N1}\"); } } }");
            SyntaxTree tree2 = tree1.WithReplaceFirst("N1", "N2");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);

            tree2 = tree1.WithReplaceFirst("Hello", "World");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact, WorkItem(7380, "https://github.com/dotnet/roslyn/issues/7380")]
        public void TestExpressionBodiedMethod()
        {
            SyntaxTree tree1 = SyntaxFactory.ParseSyntaxTree("class C { void M() => 1; }");
            SyntaxTree tree2 = SyntaxFactory.ParseSyntaxTree("class C { void M() => 2; }");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }
    }
}
