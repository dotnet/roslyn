// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxTests
    {
        private static void AssertIncompleteSubmission(string code)
        {
            Assert.False(SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(code, options: TestOptions.Script)));
        }

        private static void AssertCompleteSubmission(string code, bool isComplete = true)
        {
            Assert.True(SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree(code, options: TestOptions.Script)));
        }

        [Fact]
        public void TextIsCompleteSubmission()
        {
            Assert.Throws<ArgumentNullException>(() => SyntaxFactory.IsCompleteSubmission(null));
            Assert.Throws<ArgumentException>(() =>
                SyntaxFactory.IsCompleteSubmission(SyntaxFactory.ParseSyntaxTree("", options: TestOptions.Regular)));

            AssertCompleteSubmission("");
            AssertCompleteSubmission("//hello");
            AssertCompleteSubmission("@");
            AssertCompleteSubmission("$");
            AssertCompleteSubmission("#");

            AssertIncompleteSubmission("#if F");
            AssertIncompleteSubmission("#region R");
            AssertCompleteSubmission("#r");
            AssertCompleteSubmission("#r \"");
            AssertCompleteSubmission("#define");
            AssertCompleteSubmission("#line \"");
            AssertCompleteSubmission("#pragma");

            AssertIncompleteSubmission("using X; /*");

            AssertIncompleteSubmission(@"
void goo() 
{
#if F
}
");

            AssertIncompleteSubmission(@"
void goo() 
{
#region R
}
");

            AssertCompleteSubmission("1");
            AssertCompleteSubmission("1;");

            AssertIncompleteSubmission("\"");
            AssertIncompleteSubmission("'");

            AssertIncompleteSubmission("@\"xxx");
            AssertIncompleteSubmission("/* ");

            AssertIncompleteSubmission("1.");
            AssertIncompleteSubmission("1+");
            AssertIncompleteSubmission("f(");
            AssertIncompleteSubmission("f,");
            AssertIncompleteSubmission("f(a");
            AssertIncompleteSubmission("f(a,");
            AssertIncompleteSubmission("f(a:");
            AssertIncompleteSubmission("new");
            AssertIncompleteSubmission("new T(");
            AssertIncompleteSubmission("new T {");
            AssertIncompleteSubmission("new T");
            AssertIncompleteSubmission("1 + new T");

            // invalid escape sequence in a string
            AssertCompleteSubmission("\"\\q\"");

            AssertIncompleteSubmission("void goo(");
            AssertIncompleteSubmission("void goo()");
            AssertIncompleteSubmission("void goo() {");
            AssertCompleteSubmission("void goo() {}");
            AssertCompleteSubmission("void goo() { int a = 1 }");

            AssertIncompleteSubmission("int goo {");
            AssertCompleteSubmission("int goo { }");
            AssertCompleteSubmission("int goo { get }");

            AssertIncompleteSubmission("enum goo {");
            AssertCompleteSubmission("enum goo {}");
            AssertCompleteSubmission("enum goo { a = }");
            AssertIncompleteSubmission("class goo {");
            AssertCompleteSubmission("class goo {}");
            AssertCompleteSubmission("class goo { void }");
            AssertIncompleteSubmission("struct goo {");
            AssertCompleteSubmission("struct goo {}");
            AssertCompleteSubmission("[A struct goo {}");
            AssertIncompleteSubmission("interface goo {");
            AssertCompleteSubmission("interface goo {}");
            AssertCompleteSubmission("interface goo : {}");

            AssertCompleteSubmission("partial");
            AssertIncompleteSubmission("partial class");

            AssertIncompleteSubmission("int x = 1");
            AssertCompleteSubmission("int x = 1;");

            AssertIncompleteSubmission("delegate T F()");
            AssertIncompleteSubmission("delegate T F<");
            AssertCompleteSubmission("delegate T F();");

            AssertIncompleteSubmission("using");
            AssertIncompleteSubmission("using X");
            AssertCompleteSubmission("using X;");

            AssertIncompleteSubmission("extern");
            AssertIncompleteSubmission("extern alias");
            AssertIncompleteSubmission("extern alias X");
            AssertCompleteSubmission("extern alias X;");

            AssertIncompleteSubmission("[");
            AssertIncompleteSubmission("[A");
            AssertCompleteSubmission("[assembly: A]");

            AssertIncompleteSubmission("try");
            AssertIncompleteSubmission("try {");
            AssertIncompleteSubmission("try { }");
            AssertIncompleteSubmission("try { } finally");
            AssertIncompleteSubmission("try { } finally {");
            AssertIncompleteSubmission("try { } catch");
            AssertIncompleteSubmission("try { } catch {");
            AssertIncompleteSubmission("try { } catch (");
            AssertIncompleteSubmission("try { } catch (Exception");
            AssertIncompleteSubmission("try { } catch (Exception e");
            AssertIncompleteSubmission("try { } catch (Exception e)");
            AssertIncompleteSubmission("try { } catch (Exception e) {");

            AssertCompleteSubmission("from x in await GetStuffAsync() where x > 2 select x * x");
        }

        [Fact]
        public void TestBug530094()
        {
            var t = SyntaxFactory.AccessorDeclaration(SyntaxKind.UnknownAccessorDeclaration);
        }

        [Fact]
        public void TestBug991510()
        {
            var section = SyntaxFactory.SwitchSection();
            var span = section.Span;
            Assert.Equal(default(TextSpan), span);
        }

        [Theory]
        [InlineData("x", "x")]
        [InlineData("x.y", "y")]
        [InlineData("x?.y", "y")]
        [InlineData("this.y", "y")]
        [InlineData("M()", null)]
        [InlineData("new C()", null)]
        [InlineData("x.M()", null)]
        [InlineData("-x", null)]
        [InlineData("this", null)]
        [InlineData("default(x)", null)]
        [InlineData("typeof(x)", null)]
        public void TestTryGetInferredMemberName(string source, string expected)
        {
            var expr = SyntaxFactory.ParseExpression(source, options: TestOptions.Regular);
            var actual = SyntaxFacts.TryGetInferredMemberName(expr);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("Item0", false)]
        [InlineData("Item01", false)]
        [InlineData("Item1", true)]
        [InlineData("Item2", true)]
        [InlineData("Item10", true)]
        [InlineData("Rest", true)]
        [InlineData("ToString", true)]
        [InlineData("GetHashCode", true)]
        [InlineData("item1", false)]
        [InlineData("item10", false)]
        [InlineData("Alice", false)]
        public void TestIsReservedTupleElementName(string elementName, bool isReserved)
        {
            Assert.Equal(isReserved, SyntaxFacts.IsReservedTupleElementName(elementName));
        }

        [Theory]
        [InlineData(SyntaxKind.StringLiteralToken)]
        [InlineData(SyntaxKind.SingleLineRawStringLiteralToken)]
        [InlineData(SyntaxKind.MultiLineRawStringLiteralToken)]
        [InlineData(SyntaxKind.CharacterLiteralToken)]
        [InlineData(SyntaxKind.NumericLiteralToken)]
        [InlineData(SyntaxKind.XmlTextLiteralToken)]
        [InlineData(SyntaxKind.XmlTextLiteralNewLineToken)]
        [InlineData(SyntaxKind.XmlEntityLiteralToken)]
        public void TestIsLiteral(SyntaxKind kind)
        {
            Assert.True(SyntaxFacts.IsLiteral(kind));
        }

        [Theory]
        [InlineData(SyntaxKind.StringLiteralToken)]
        [InlineData(SyntaxKind.SingleLineRawStringLiteralToken)]
        [InlineData(SyntaxKind.MultiLineRawStringLiteralToken)]
        [InlineData(SyntaxKind.CharacterLiteralToken)]
        [InlineData(SyntaxKind.NumericLiteralToken)]
        [InlineData(SyntaxKind.XmlTextLiteralToken)]
        [InlineData(SyntaxKind.XmlTextLiteralNewLineToken)]
        [InlineData(SyntaxKind.XmlEntityLiteralToken)]
        public void TestIsAnyToken(SyntaxKind kind)
        {
            Assert.True(SyntaxFacts.IsAnyToken(kind));
        }

        [Theory]
        [InlineData(SyntaxKind.StringLiteralToken, SyntaxKind.StringLiteralExpression)]
        [InlineData(SyntaxKind.SingleLineRawStringLiteralToken, SyntaxKind.StringLiteralExpression)]
        [InlineData(SyntaxKind.MultiLineRawStringLiteralToken, SyntaxKind.StringLiteralExpression)]
        [InlineData(SyntaxKind.CharacterLiteralToken, SyntaxKind.CharacterLiteralExpression)]
        [InlineData(SyntaxKind.NumericLiteralToken, SyntaxKind.NumericLiteralExpression)]
        [InlineData(SyntaxKind.NullKeyword, SyntaxKind.NullLiteralExpression)]
        [InlineData(SyntaxKind.TrueKeyword, SyntaxKind.TrueLiteralExpression)]
        [InlineData(SyntaxKind.FalseKeyword, SyntaxKind.FalseLiteralExpression)]
        [InlineData(SyntaxKind.ArgListKeyword, SyntaxKind.ArgListExpression)]
        public void TestGetLiteralExpression(SyntaxKind tokenKind, SyntaxKind expressionKind)
        {
            Assert.Equal(expressionKind, SyntaxFacts.GetLiteralExpression(tokenKind));
        }
    }
}
