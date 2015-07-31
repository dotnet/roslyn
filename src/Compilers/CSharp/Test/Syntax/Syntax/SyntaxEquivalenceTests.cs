// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxEquivalenceTests
    {
        private void VerifyEquivalent(SyntaxTree tree1, SyntaxTree tree2, bool topLevel)
        {
            Assert.True(SyntaxFactory.AreEquivalent(tree1, tree2, topLevel));

            // now try as if the second tree were created from scratch.
            var tree3 = SyntaxFactory.ParseSyntaxTree(tree2.GetText().ToString());
            Assert.True(SyntaxFactory.AreEquivalent(tree1, tree3, topLevel));
        }

        private void VerifyNotEquivalent(SyntaxTree tree1, SyntaxTree tree2, bool topLevel)
        {
            Assert.False(SyntaxFactory.AreEquivalent(tree1, tree2, topLevel));

            // now try as if the second tree were created from scratch.
            var tree3 = SyntaxFactory.ParseSyntaxTree(tree2.GetText().ToString());
            Assert.False(SyntaxFactory.AreEquivalent(tree1, tree3, topLevel));
        }

        [Fact]
        public void TestEmptyTrees()
        {
            var text = "";
            var tree1 = SyntaxFactory.ParseSyntaxTree(text);
            var tree2 = SyntaxFactory.ParseSyntaxTree(text);

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingComment()
        {
            var text = "";
            var tree1 = SyntaxFactory.ParseSyntaxTree(text);
            var tree2 = tree1.WithInsertAt(0, "/* foo */");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingActivePPDirective()
        {
            var text = "";
            var tree1 = SyntaxFactory.ParseSyntaxTree(text);
            var tree2 = tree1.WithInsertAt(0, "#if true \r\n\r\n#endif");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingInactivePPDirective()
        {
            var text = "";
            var tree1 = SyntaxFactory.ParseSyntaxTree(text);
            var tree2 = tree1.WithInsertAt(0, "#if false \r\n\r\n#endif");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingEmpty()
        {
            var text = "";
            var tree1 = SyntaxFactory.ParseSyntaxTree(text);
            var tree2 = tree1.WithInsertAt(0, "namespace N { }");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingClass()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { }");
            var tree2 = tree1.WithInsertBefore("}", "class C { }");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRenameOuter()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { }");
            var tree2 = tree1.WithReplaceFirst("N", "N1");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRenameInner()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Foo() { int z = 0; } } }");
            var tree2 = tree1.WithReplaceFirst("z", "y");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRenameOuterToSamename()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { }");
            var tree2 = tree1.WithReplaceFirst("N", "N");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRenameInnerToSameName()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Foo() { int z = 0; } } }");
            var tree2 = tree1.WithReplaceFirst("z", "z");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingMethod()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { } }");
            var tree2 = tree1.WithInsertBefore("}", "void Foo() { } ");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingLocal()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Foo() { } } }");
            var tree2 = tree1.WithInsertBefore("}", "int i; ");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRemovingLocal()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Foo() { int i; } } }");
            var tree2 = tree1.WithRemoveFirst("int i;");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRemovingField1()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { int i = 5; int j = 6; } }");
            var tree2 = tree1.WithRemoveFirst("int i = 5;");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRemovingField2()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { int i = 5; int j = 6; } }");
            var tree2 = tree1.WithRemoveFirst("int j = 6;");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingField()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { int i = 5; } }");
            var tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingField2()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { int i = 5, j = 7; } }");
            var tree2 = tree1.WithReplaceFirst("7", "8");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingConstField()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { const int i = 5; } }");
            var tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingConstField2()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { const int i = 5, j = 7; } }");
            var tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);

            tree2 = tree1.WithReplaceFirst("7", "8");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingConstLocal()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Foo() { const int i = 5; } } }");
            var tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingEnumMember()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("enum E { i = 5 }");
            var tree2 = tree1.WithReplaceFirst("5", "6");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingAttribute()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { [Obsolete(true)]class C { const int i = 5; } }");
            var tree2 = tree1.WithReplaceFirst("true", "false");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingMethodCall()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Foo() { Console.Write(0); } } }");
            var tree2 = tree1.WithReplaceFirst("Write", "WriteLine");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingUsing()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("using System; namespace N { class C { void Foo() { Console.Write(0); } } }");
            var tree2 = tree1.WithReplaceFirst("System", "System.Linq");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingBaseType()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { Console.Write(0); } }");
            var tree2 = tree1.WithInsertBefore("{", ": B ");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingMethodType()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { Console.Write(0); } }");
            var tree2 = tree1.WithReplaceFirst("void", "int");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddComment()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { Console.Write(0); } }");
            var tree2 = tree1.WithInsertBefore("class", "// Comment\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestCommentOutCode()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { Console.Write(0); } }");
            var tree2 = tree1.WithInsertBefore("class", "// ");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddDocComment()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { Console.Write(0); } }");
            var tree2 = tree1.WithInsertBefore("class", "/// Comment\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestCommentOutMethodCode()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { Console.Write(0); } }");
            var tree2 = tree1.WithReplaceFirst("Console.Write(0);", "/* Console.Write(0); */");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestCommentOutMethod()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { } }");
            var tree2 = tree1.WithReplaceFirst("void Foo() { }", "/* void Foo() { } */");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundMethodWithActivePPRegion()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { } }");
            var tree2 = tree1.WithReplaceFirst("void Foo() { }", "\r\n#if true\r\n void Foo() { }\r\n#endif\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundMethodWithInactivePPRegion()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { } }");
            var tree2 = tree1.WithReplaceFirst("void Foo() { }", "\r\n#if false\r\n void Foo() { }\r\n#endif\r\n");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundStatementWithActivePPRegion()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { int i; } }");
            var tree2 = tree1.WithReplaceFirst("int i;", "\r\n#if true\r\n int i;\r\n#endif\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundStatementWithInactivePPRegion()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { int i; } }");
            var tree2 = tree1.WithReplaceFirst("int i;", "\r\n#if false\r\n int i;\r\n#endif\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddLotsOfComments()
        {
            var text = "class C { void Foo() { int i; } }";
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { int i; } }");
            var tree2 = SyntaxFactory.ParseSyntaxTree(text.Replace(" ", " /**/ "));

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangeWhitespace()
        {
            var text = "class C { void Foo() { int i; } }";
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Foo() { int i; } }");
            var tree2 = SyntaxFactory.ParseSyntaxTree(text.Replace(" ", "  "));

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSkippedTest()
        {
            var text = "abc using";
            var tree1 = SyntaxFactory.ParseSyntaxTree(text);
            var tree2 = SyntaxFactory.ParseSyntaxTree(text.Replace("abc", "hello"));

            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUpdateInterpolatedString()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Foo() { Console.Write($\"Hello{123:N1}\"); } } }");
            var tree2 = tree1.WithReplaceFirst("N1", "N2");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);

            tree2 = tree1.WithReplaceFirst("Hello", "World");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }
    }
}
