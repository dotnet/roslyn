// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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

        private void VerifyEquivalent(SyntaxNode node1, SyntaxNode node2, Func<SyntaxKind, bool> ignoreChildNode)
        {
            Assert.True(SyntaxFactory.AreEquivalent(node1, node2, ignoreChildNode));

            // now try as if the second tree were created from scratch.
            var tree3 = SyntaxFactory.ParseSyntaxTree(node2.GetText().ToString());
            Assert.True(SyntaxFactory.AreEquivalent(node1, tree3.GetRoot(), ignoreChildNode));
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
            var tree2 = tree1.WithInsertAt(0, "/* goo */");

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
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { int z = 0; } } }");
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
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { int z = 0; } } }");
            var tree2 = tree1.WithReplaceFirst("z", "z");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingMethod()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { } }");
            var tree2 = tree1.WithInsertBefore("}", "void Goo() { } ");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddingLocal()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { } } }");
            var tree2 = tree1.WithInsertBefore("}", "int i; ");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRemovingLocal()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { int i; } } }");
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
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { const int i = 5; } } }");
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
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { Console.Write(0); } } }");
            var tree2 = tree1.WithReplaceFirst("Write", "WriteLine");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingUsing()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("using System; namespace N { class C { void Goo() { Console.Write(0); } } }");
            var tree2 = tree1.WithReplaceFirst("System", "System.Linq");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingBaseType()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            var tree2 = tree1.WithInsertBefore("{", ": B ");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangingMethodType()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            var tree2 = tree1.WithReplaceFirst("void", "int");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddComment()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            var tree2 = tree1.WithInsertBefore("class", "// Comment\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestCommentOutCode()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            var tree2 = tree1.WithInsertBefore("class", "// ");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddDocComment()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            var tree2 = tree1.WithInsertBefore("class", "/// Comment\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestCommentOutMethodCode()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { Console.Write(0); } }");
            var tree2 = tree1.WithReplaceFirst("Console.Write(0);", "/* Console.Write(0); */");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestCommentOutMethod()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { } }");
            var tree2 = tree1.WithReplaceFirst("void Goo() { }", "/* void Goo() { } */");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundMethodWithActivePPRegion()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { } }");
            var tree2 = tree1.WithReplaceFirst("void Goo() { }", "\r\n#if true\r\n void Goo() { }\r\n#endif\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundMethodWithInactivePPRegion()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { } }");
            var tree2 = tree1.WithReplaceFirst("void Goo() { }", "\r\n#if false\r\n void Goo() { }\r\n#endif\r\n");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundStatementWithActivePPRegion()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { int i; } }");
            var tree2 = tree1.WithReplaceFirst("int i;", "\r\n#if true\r\n int i;\r\n#endif\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestSurroundStatementWithInactivePPRegion()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { int i; } }");
            var tree2 = tree1.WithReplaceFirst("int i;", "\r\n#if false\r\n int i;\r\n#endif\r\n");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestAddLotsOfComments()
        {
            var text = "class C { void Goo() { int i; } }";
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { int i; } }");
            var tree2 = SyntaxFactory.ParseSyntaxTree(text.Replace(" ", " /**/ "));

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestChangeWhitespace()
        {
            var text = "class C { void Goo() { int i; } }";
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void Goo() { int i; } }");
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
            var tree1 = SyntaxFactory.ParseSyntaxTree("namespace N { class C { void Goo() { Console.Write($\"Hello{123:N1}\"); } } }");
            var tree2 = tree1.WithReplaceFirst("N1", "N2");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);

            tree2 = tree1.WithReplaceFirst("Hello", "World");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact, WorkItem(7380, "https://github.com/dotnet/roslyn/issues/7380")]
        public void TestExpressionBodiedMethod()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree("class C { void M() => 1; }");
            var tree2 = SyntaxFactory.ParseSyntaxTree("class C { void M() => 2; }");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Theory, WorkItem(38694, "https://github.com/dotnet/roslyn/issues/38694")]
        [InlineData("#nullable enable", "#nullable disable")]
        [InlineData("#nullable enable", "#nullable restore")]
        [InlineData("#nullable disable", "#nullable restore")]
        [InlineData("#nullable enable", "#nullable enable warnings")]
        [InlineData("#nullable enable", "#nullable enable annotations")]
        [InlineData("#nullable enable annotations", "#nullable enable warnings")]
        [InlineData("", "#nullable disable")]
        [InlineData("", "#nullable enable")]
        [InlineData("", "#nullable restore")]
        [InlineData("#nullable disable", "")]
        [InlineData("#nullable enable", "")]
        [InlineData("#nullable restore", "")]
        public void TestNullableDirectives_DifferentDirectives(string firstDirective, string secondDirective)
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree($@"
{firstDirective}
class C
{{
}}");
            var tree2 = SyntaxFactory.ParseSyntaxTree($@"
{secondDirective}
class C
{{
}}");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1.GetRoot(), tree2.GetRoot(), ignoreChildNode: k => k == SyntaxKind.NullableDirectiveTrivia);

            var tree3 = SyntaxFactory.ParseSyntaxTree($@"
class C
{{
    void M()
    {{
{firstDirective}
    }}
}}");
            var tree4 = SyntaxFactory.ParseSyntaxTree($@"
class C
{{
    void M()
    {{
{secondDirective}
    }}
}}");

            VerifyNotEquivalent(tree3, tree4, topLevel: true);
            VerifyNotEquivalent(tree3, tree4, topLevel: false);
            VerifyEquivalent(tree3.GetRoot(), tree4.GetRoot(), ignoreChildNode: k => k == SyntaxKind.NullableDirectiveTrivia);
        }

        [Theory, WorkItem(38694, "https://github.com/dotnet/roslyn/issues/38694")]
        [InlineData("#nullable enable")]
        [InlineData("#nullable disable")]
        [InlineData("#nullable restore")]
        [InlineData("#nullable enable warnings")]
        public void TestNullableDirectives_TopLevelIdentical(string directive)
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree($@"
class C
{{
    void M()
    {{
{directive}
        Console.WriteLine(1234);
    }}
}}");
            var tree2 = SyntaxFactory.ParseSyntaxTree($@"
class C
{{
    void M()
    {{
{directive}
    }}
}}");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact, WorkItem(38694, "https://github.com/dotnet/roslyn/issues/38694")]
        public void TestNullableDirectives_InvalidDirective()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
#nullable invalid
    }
}");
            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact, WorkItem(38694, "https://github.com/dotnet/roslyn/issues/38694")]
        public void TestNullableDirectives_DifferentNumberOfDirectives()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
#nullable enable
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
#nullable enable
#nullable disable
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRawStringLiteral1()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc"""""";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc"""""";
    }
}");

            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRawStringLiteral2()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc"""""";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abcd"""""";
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestRawStringLiteral3()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc"""""";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc"";
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestRawStringLiteral4()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc"""""";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""""abc"""""""";
    }
}");

            VerifyEquivalent(tree1, tree2, topLevel: true);
            VerifyNotEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestStringLiteral_01()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc"";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc"";
    }
}");

            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestStringLiteral_02()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc"";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = @""abc"";
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestStringLiteral_03()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc"";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abcd"";
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestStringLiteral_04()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc"";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = @""abcd"";
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8StringLiteral_01()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc""u8;
    }
}");

            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestUTF8StringLiteral_02()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = @""abc""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8StringLiteral_03()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abcd""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8StringLiteral_04()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc""U8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8StringLiteral_05()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc"";
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8StringLiteral_06()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc"";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = ""abc""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8SingleLineRawStringLiteral_01()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc""""""u8;
    }
}");

            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestUTF8SingleLineRawStringLiteral_02()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""""abc""""""""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8SingleLineRawStringLiteral_03()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abcd""""""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8SingleLineRawStringLiteral_04()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc""""""U8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8SingleLineRawStringLiteral_05()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc"""""";
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8SingleLineRawStringLiteral_06()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc"""""";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc""""""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8MultiLineRawStringLiteral_01()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
""""""u8;
    }
}");

            VerifyEquivalent(tree1, tree2, topLevel: false);
        }

        [Fact]
        public void TestUTF8MultiLineRawStringLiteral_02()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""""
abc
""""""""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8MultiLineRawStringLiteral_03()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abcd
""""""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8MultiLineRawStringLiteral_04()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
""""""U8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8MultiLineRawStringLiteral_05()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
"""""";
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8MultiLineRawStringLiteral_06()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
"""""";
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
""""""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }

        [Fact]
        public void TestUTF8MultiLineRawStringLiteral_07()
        {
            var tree1 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""
abc
""""""u8;
    }
}");

            var tree2 = SyntaxFactory.ParseSyntaxTree(@"
class C
{
    void M()
    {
        var v = """"""abc""""""u8;
    }
}");

            VerifyNotEquivalent(tree1, tree2, topLevel: false);
            VerifyEquivalent(tree1, tree2, topLevel: true);
        }
    }
}
