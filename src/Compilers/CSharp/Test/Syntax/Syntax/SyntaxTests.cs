// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
void foo() 
{
#if F
}
");

            AssertIncompleteSubmission(@"
void foo() 
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

            AssertIncompleteSubmission("void foo(");
            AssertIncompleteSubmission("void foo()");
            AssertIncompleteSubmission("void foo() {");
            AssertCompleteSubmission("void foo() {}");
            AssertCompleteSubmission("void foo() { int a = 1 }");

            AssertIncompleteSubmission("int foo {");
            AssertCompleteSubmission("int foo { }");
            AssertCompleteSubmission("int foo { get }");

            AssertIncompleteSubmission("enum foo {");
            AssertCompleteSubmission("enum foo {}");
            AssertCompleteSubmission("enum foo { a = }");
            AssertIncompleteSubmission("class foo {");
            AssertCompleteSubmission("class foo {}");
            AssertCompleteSubmission("class foo { void }");
            AssertIncompleteSubmission("struct foo {");
            AssertCompleteSubmission("struct foo {}");
            AssertCompleteSubmission("[A struct foo {}");
            AssertIncompleteSubmission("interface foo {");
            AssertCompleteSubmission("interface foo {}");
            AssertCompleteSubmission("interface foo : {}");

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
    }
}
