// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxFormatterTests
    {
        [Fact]
        public void TestFormatExpression1()
        {
            TestFormatExpression("!a", "!a");
            TestFormatExpression("-a", "-a");
            TestFormatExpression("+a", "+a");
            TestFormatExpression("~a", "~a");

            TestFormatExpression("a", "a");
            TestFormatExpression("a+b", "a + b");
            TestFormatExpression("a-b", "a - b");
            TestFormatExpression("a*b", "a * b");
            TestFormatExpression("a/b", "a / b");
            TestFormatExpression("a%b", "a % b");
            TestFormatExpression("a^b", "a ^ b");
            TestFormatExpression("a|b", "a | b");
            TestFormatExpression("a&b", "a & b");
            TestFormatExpression("a||b", "a || b");
            TestFormatExpression("a&&b", "a && b");
            TestFormatExpression("a<b", "a < b");
            TestFormatExpression("a<=b", "a <= b");
            TestFormatExpression("a>b", "a > b");
            TestFormatExpression("a>=b", "a >= b");
            TestFormatExpression("a==b", "a == b");
            TestFormatExpression("a!=b", "a != b");
            TestFormatExpression("a<<b", "a << b");
            TestFormatExpression("a>>b", "a >> b");
            TestFormatExpression("a??b", "a ?? b");

            TestFormatExpression("a<b>.c", "a<b>.c");
            TestFormatExpression("(a+b)", "(a + b)");
            TestFormatExpression("((a)+(b))", "((a) + (b))");
            TestFormatExpression("(a)b", "(a)b");
            TestFormatExpression("(a)(b)", "(a)(b)");

            TestFormatExpression("m()", "m()");
            TestFormatExpression("m(a)", "m(a)");
            TestFormatExpression("m(a,b)", "m(a, b)");
            TestFormatExpression("m(a,b,c)", "m(a, b, c)");
            TestFormatExpression("m(a,b(c,d))", "m(a, b(c, d))");

            TestFormatExpression("a?b:c", "a ? b : c");
            TestFormatExpression("from a in b where c select d", "from a in b\r\nwhere c\r\nselect d");

            TestFormatExpression("a().b().c()", "a().b().c()");
            TestFormatExpression("a->b->c", "a->b->c");
            TestFormatExpression("global :: a", "global::a");

            TestFormatExpression("(IList<int>)args", "(IList<int>)args");
            TestFormatExpression("(IList<IList<int>>)args", "(IList<IList<int>>)args");
        }

        private void TestFormatExpression(string text, string expected)
        {
            var node = SyntaxFactory.ParseExpression(text);
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestFormatStatement1()
        {
            // expressions
            TestFormatStatement("a;", "a;");

            // blocks
            TestFormatStatement("{a;}", "{\r\n  a;\r\n}");
            TestFormatStatement("{a;b;}", "{\r\n  a;\r\n  b;\r\n}");
            TestFormatStatement("\t{a;}", "{\r\n  a;\r\n}");
            TestFormatStatement("\t{a;b;}", "{\r\n  a;\r\n  b;\r\n}");

            // if
            TestFormatStatement("if(a)b;", "if (a)\r\n  b;");
            TestFormatStatement("if(a){b;}", "if (a)\r\n{\r\n  b;\r\n}");
            TestFormatStatement("if(a){b;c;}", "if (a)\r\n{\r\n  b;\r\n  c;\r\n}");
            TestFormatStatement("if(a)b;else c;", "if (a)\r\n  b;\r\nelse\r\n  c;");
            TestFormatStatement("if(a)b;else if(c)d;", "if (a)\r\n  b;\r\nelse if (c)\r\n  d;");

            // while
            TestFormatStatement("while(a)b;", "while (a)\r\n  b;");
            TestFormatStatement("while(a){b;}", "while (a)\r\n{\r\n  b;\r\n}");

            // do 
            TestFormatStatement("do{a;}while(b);", "do\r\n{\r\n  a;\r\n}\r\nwhile (b);");

            // for
            TestFormatStatement("for(a;b;c)d;", "for (a; b; c)\r\n  d;");
            TestFormatStatement("for(;;)a;", "for (;;)\r\n  a;");

            // foreach
            TestFormatStatement("foreach(a in b)c;", "foreach (a in b)\r\n  c;");

            // try
            TestFormatStatement("try{a;}catch(b){c;}", "try\r\n{\r\n  a;\r\n}\r\ncatch (b)\r\n{\r\n  c;\r\n}");
            TestFormatStatement("try{a;}finally{b;}", "try\r\n{\r\n  a;\r\n}\r\nfinally\r\n{\r\n  b;\r\n}");

            // other
            TestFormatStatement("lock(a)b;", "lock (a)\r\n  b;");
            TestFormatStatement("fixed(a)b;", "fixed (a)\r\n  b;");
            TestFormatStatement("using(a)b;", "using (a)\r\n  b;");
            TestFormatStatement("checked{a;}", "checked\r\n{\r\n  a;\r\n}");
            TestFormatStatement("unchecked{a;}", "unchecked\r\n{\r\n  a;\r\n}");
            TestFormatStatement("unsafe{a;}", "unsafe\r\n{\r\n  a;\r\n}");

            // declaration statements
            TestFormatStatement("a b;", "a b;");
            TestFormatStatement("a?b;", "a? b;");
            TestFormatStatement("a b,c;", "a b, c;");
            TestFormatStatement("a b=c;", "a b = c;");
            TestFormatStatement("a b=c,d=e;", "a b = c, d = e;");

            // empty statements
            TestFormatStatement(";", ";");
            TestFormatStatement("{;;}", "{\r\n  ;\r\n  ;\r\n}");

            // labelled statemetns
            TestFormatStatement("foo:;", "foo:\r\n  ;");
            TestFormatStatement("foo:a;", "foo:\r\n  a;");

            // return/goto
            TestFormatStatement("return;", "return;");
            TestFormatStatement("return(a);", "return (a);");
            TestFormatStatement("continue;", "continue;");
            TestFormatStatement("break;", "break;");
            TestFormatStatement("yield return;", "yield return;");
            TestFormatStatement("yield return(a);", "yield return (a);");
            TestFormatStatement("yield break;", "yield break;");
            TestFormatStatement("goto a;", "goto a;");
            TestFormatStatement("throw;", "throw;");
            TestFormatStatement("throw a;", "throw a;");
            TestFormatStatement("return this.Bar()", "return this.Bar()");

            // switch
            TestFormatStatement("switch(a){case b:c;}", "switch (a)\r\n{\r\n  case b:\r\n    c;\r\n}");
            TestFormatStatement("switch(a){case b:c;case d:e;}", "switch (a)\r\n{\r\n  case b:\r\n    c;\r\n  case d:\r\n    e;\r\n}");
            TestFormatStatement("switch(a){case b:c;default:d;}", "switch (a)\r\n{\r\n  case b:\r\n    c;\r\n  default:\r\n    d;\r\n}");
            TestFormatStatement("switch(a){case b:{}default:{}}", "switch (a)\r\n{\r\n  case b:\r\n  {\r\n  }\r\n\r\n  default:\r\n  {\r\n  }\r\n}");
            TestFormatStatement("switch(a){case b:c();d();default:e();f();}", "switch (a)\r\n{\r\n  case b:\r\n    c();\r\n    d();\r\n  default:\r\n    e();\r\n    f();\r\n}");
            TestFormatStatement("switch(a){case b:{c();}}", "switch (a)\r\n{\r\n  case b:\r\n  {\r\n    c();\r\n  }\r\n}");

            // curlies
            TestFormatStatement("{if(foo){}if(bar){}}", "{\r\n  if (foo)\r\n  {\r\n  }\r\n\r\n  if (bar)\r\n  {\r\n  }\r\n}");

            // Queries
            TestFormatStatement("int i=from v in vals select v;", "int i =\r\n  from v in vals\r\n  select v;");
            TestFormatStatement("Foo(from v in vals select v);", "Foo(\r\n  from v in vals\r\n  select v);");
            TestFormatStatement("int i=from v in vals select from x in xxx where x > 10 select x;", "int i =\r\n  from v in vals\r\n  select\r\n    from x in xxx\r\n    where x > 10\r\n    select x;");
            TestFormatStatement("int i=from v in vals group v by x into g where g > 10 select g;", "int i =\r\n  from v in vals\r\n  group v by x into g\r\n    where g > 10\r\n    select g;");

            // Generics
            TestFormatStatement("Func<string, int> f = blah;", "Func<string, int> f = blah;");
        }

        private void TestFormatStatement(string text, string expected)
        {
            var node = SyntaxFactory.ParseStatement(text);
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestFormatDeclaration1()
        {
            // usings
            TestFormatDeclaration("using a;", "using a;");
            TestFormatDeclaration("using a=b;", "using a = b;");
            TestFormatDeclaration("using a.b;", "using a.b;");
            TestFormatDeclaration("using A; using B; class C {}", "using A;\r\nusing B;\r\n\r\nclass C\r\n{\r\n}");

            // namespace
            TestFormatDeclaration("namespace a{}", "namespace a\r\n{\r\n}");
            TestFormatDeclaration("namespace a{using b;}", "namespace a\r\n{\r\n  using b;\r\n}");
            TestFormatDeclaration("namespace a{namespace b{}}", "namespace a\r\n{\r\n  namespace b\r\n  {\r\n  }\r\n}");
            TestFormatDeclaration("namespace a{}namespace b{}", "namespace a\r\n{\r\n}\r\n\r\nnamespace b\r\n{\r\n}");

            // type
            TestFormatDeclaration("class a{}", "class a\r\n{\r\n}");
            TestFormatDeclaration("class a{class b{}}", "class a\r\n{\r\n  class b\r\n  {\r\n  }\r\n}");
            TestFormatDeclaration("class a<b>where a:c{}", "class a<b>\r\n  where a : c\r\n{\r\n}");
            TestFormatDeclaration("class a<b,c>where a:c{}", "class a<b, c>\r\n  where a : c\r\n{\r\n}");
            TestFormatDeclaration("class a:b{}", "class a : b\r\n{\r\n}");

            // methods
            TestFormatDeclaration("class a{void b(){}}", "class a\r\n{\r\n  void b()\r\n  {\r\n  }\r\n}");
            TestFormatDeclaration("class a{void b(){}void c(){}}", "class a\r\n{\r\n  void b()\r\n  {\r\n  }\r\n\r\n  void c()\r\n  {\r\n  }\r\n}");
            TestFormatDeclaration("class a{a(){}}", "class a\r\n{\r\n  a()\r\n  {\r\n  }\r\n}");
            TestFormatDeclaration("class a{~a(){}}", "class a\r\n{\r\n  ~a()\r\n  {\r\n  }\r\n}");

            // properties
            TestFormatDeclaration("class a{b c{get;}}", "class a\r\n{\r\n  b c\r\n  {\r\n    get;\r\n  }\r\n}");

            // indexers
            TestFormatDeclaration("class a{b this[c d]{get;}}", "class a\r\n{\r\n  b this[c d]\r\n  {\r\n    get;\r\n  }\r\n}");

            // fields
            TestFormatDeclaration("class a{b c;}", "class a\r\n{\r\n  b c;\r\n}");
            TestFormatDeclaration("class a{b c=d;}", "class a\r\n{\r\n  b c = d;\r\n}");
            TestFormatDeclaration("class a{b c=d,e=f;}", "class a\r\n{\r\n  b c = d, e = f;\r\n}");

            // delegate
            TestFormatDeclaration("delegate a b();", "delegate a b();");
            TestFormatDeclaration("delegate a b(c);", "delegate a b(c);");
            TestFormatDeclaration("delegate a b(c,d);", "delegate a b(c, d);");

            // enums
            TestFormatDeclaration("enum a{}", "enum a\r\n{\r\n}");
            TestFormatDeclaration("enum a{b}", "enum a\r\n{\r\n  b\r\n}");
            TestFormatDeclaration("enum a{b,c}", "enum a\r\n{\r\n  b,\r\n  c\r\n}");
            TestFormatDeclaration("enum a{b=c}", "enum a\r\n{\r\n  b = c\r\n}");

            // attributes
            TestFormatDeclaration("[a]class b{}", "[a]\r\nclass b\r\n{\r\n}");
            TestFormatDeclaration("\t[a]class b{}", "[a]\r\nclass b\r\n{\r\n}");
            TestFormatDeclaration("[a,b]class c{}", "[a, b]\r\nclass c\r\n{\r\n}");
            TestFormatDeclaration("[a(b)]class c{}", "[a(b)]\r\nclass c\r\n{\r\n}");
            TestFormatDeclaration("[a(b,c)]class d{}", "[a(b, c)]\r\nclass d\r\n{\r\n}");
            TestFormatDeclaration("[a][b]class c{}", "[a]\r\n[b]\r\nclass c\r\n{\r\n}");
            TestFormatDeclaration("[a:b]class c{}", "[a: b]\r\nclass c\r\n{\r\n}");
        }

        [WorkItem(541684, "DevDiv")]
        [Fact]
        public void TestFormatRegion1()
        {
            // NOTE: the space after the region name is retained, since the text after the space
            // following "#region" is a single, unstructured trivia element.
            TestFormatDeclaration(
                "\r\nclass Class \r\n{ \r\n#region Methods \r\nvoid Method() \r\n{ \r\n} \r\n#endregion \r\n}",
                "class Class\r\n{\r\n#region Methods \r\n  void Method()\r\n  {\r\n  }\r\n#endregion\r\n}");
            TestFormatDeclaration(
                "\r\n#region\r\n#endregion",
                "#region\r\n#endregion\r\n");
            TestFormatDeclaration(
                "\r\n#region  \r\n#endregion",
                "#region\r\n#endregion\r\n");
            TestFormatDeclaration(
                "\r\n#region name //comment\r\n#endregion",
                "#region name //comment\r\n#endregion\r\n");
            TestFormatDeclaration(
                "\r\n#region /*comment*/\r\n#endregion",
                "#region /*comment*/\r\n#endregion\r\n");
        }

        [WorkItem(528584, "DevDiv")]
        [Fact]
        public void TestFormatRegion2()
        {
            TestFormatDeclaration(
                "\r\n#region //comment\r\n#endregion",
                // NOTE: the extra newline should be removed, but it's not worth the
                // effort (see DevDiv #8564)
                "#region //comment\r\n\r\n#endregion\r\n");
            TestFormatDeclaration(
                "\r\n#region //comment\r\n\r\n#endregion",
                // NOTE: the extra newline should be removed, but it's not worth the
                // effort (see DevDiv #8564).
                "#region //comment\r\n\r\n#endregion\r\n");
        }

        private void TestFormatDeclaration(string text, string expected)
        {
            var node = SyntaxFactory.ParseCompilationUnit(text);
            Assert.Equal(text, node.ToFullString());
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestFormatComments()
        {
            TestFormatToken("a//b", "a //b\r\n");
            TestFormatToken("a/*b*/", "a /*b*/");
            TestFormatToken("//a\r\nb", "//a\r\nb");
            TestFormatExpression("a/*b*/+c", "a /*b*/ + c");
            TestFormatExpression("/*a*/b", "/*a*/\r\nb");
            TestFormatExpression("/*a\r\n*/b", "/*a\r\n*/\r\nb");
            TestFormatStatement("{/*a*/b}", "{ /*a*/\r\n  b\r\n}");
            TestFormatStatement("{\r\na//b\r\n}", "{\r\n  a //b\r\n}");
            TestFormatStatement("{\r\n//a\r\n}", "{\r\n//a\r\n}");
            TestFormatStatement("{\r\n//a\r\nb}", "{\r\n  //a\r\n  b\r\n}");
            TestFormatStatement("{\r\n/*a*/b}", "{\r\n  /*a*/\r\n  b\r\n}");
            TestFormatStatement("{\r\n/// <foo/>\r\na}", "{\r\n  /// <foo/>\r\n  a\r\n}");
            TestFormatStatement("{\r\n///<foo/>\r\na}", "{\r\n  ///<foo/>\r\n  a\r\n}");
            TestFormatStatement("{\r\n/// <foo>\r\n/// </foo>\r\na}", "{\r\n  /// <foo>\r\n  /// </foo>\r\n  a\r\n}");
            TestFormatToken("/// <foo>\r\n/// </foo>\r\na", "/// <foo>\r\n/// </foo>\r\na");
            TestFormatStatement("{\r\n/*** <foo/> ***/\r\na}", "{\r\n  /*** <foo/> ***/\r\n  a\r\n}");
            TestFormatStatement("{\r\n/*** <foo/>\r\n ***/\r\na}", "{\r\n  /*** <foo/>\r\n ***/\r\n  a\r\n}");
        }

        private void TestFormatToken(string text, string expected)
        {
            var token = SyntaxFactory.ParseToken(text);
            var actual = token.NormalizeWhitespace().ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestFormatPreprocessorDirectives()
        {
            TestFormat(SyntaxFactory.DefineDirectiveTrivia(SyntaxFactory.Identifier("a"), false), "#define a\r\n");
            TestFormatTrivia("  #  define a", "#define a\r\n");
            TestFormatTrivia("#if(a||b)", "#if (a || b)\r\n");
            TestFormatTrivia("#if(a&&b)", "#if (a && b)\r\n");
            TestFormatTrivia("  #if a\r\n  #endif", "#if a\r\n#endif\r\n");

            TestFormat(
                SyntaxFactory.TriviaList(
                    SyntaxFactory.Trivia(
                        SyntaxFactory.IfDirectiveTrivia(SyntaxFactory.IdentifierName("a"), false, false, false)),
                    SyntaxFactory.Trivia(
                        SyntaxFactory.EndIfDirectiveTrivia(false))),
                "#if a\r\n#endif\r\n");

            // red factories for structured trivia needs to return SyntaxTrivia, not structured node types?
        }

        [Fact]
        [WorkItem(531607, "DevDiv")]
        public void TestFormatLineDirectiveTrivia()
        {
            TestFormat(
                SyntaxFactory.TriviaList(
                    SyntaxFactory.Trivia(
                        SyntaxFactory.LineDirectiveTrivia(
                            SyntaxFactory.Literal(1),
                            true)
                        .WithEndOfDirectiveToken(
                            SyntaxFactory.Token(
                                SyntaxFactory.TriviaList(
                                    SyntaxFactory.Trivia(
                                        SyntaxFactory.SkippedTokensTrivia()
                                        .WithTokens(
                                            SyntaxFactory.TokenList(
                                                SyntaxFactory.Literal(@"""a\b"""))))),
                                SyntaxKind.EndOfDirectiveToken,
                                default(SyntaxTriviaList))))),
                @"#line 1 ""\""a\\b\""""
");
            // Note: without all the escaping, it looks like this '#line 1 @"""a\b"""' (i.e. the string literal has a value of '"a\b"').
            // Note: the literal was formatted as a C# string literal, not as a directive string literal.
        }

        [WorkItem(538115, "DevDiv")]
        [Fact]
        public void TestFormatWithinDirectives()
        {
            TestFormatDeclaration(
"class C\r\n{\r\n#if true\r\nvoid Foo(A x) { }\r\n#else\r\n#endif\r\n}\r\n",
"class C\r\n{\r\n#if true\r\n  void Foo(A x)\r\n  {\r\n  }\r\n#else\r\n#endif\r\n}");
        }

        [WorkItem(542887, "DevDiv")]
        [Fact]
        public void TestFormattingForBlockSyntax()
        {
            var code =
@"class c1
{
void foo()
{
{
int i = 1;
}
}
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            TestFormat(tree.GetCompilationUnitRoot(),
@"class c1
{
  void foo()
  {
    {
      int i = 1;
    }
  }
}");
        }

        [WorkItem(1079042, "DevDiv")]
        [Fact]
        public void TestFormatDocumentationComments()
        {
            var code =
@"class c1
{
    ///<summary>
    /// A documenation comment
    ///</summary>
    void foo()
    {
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            TestFormat(tree.GetCompilationUnitRoot(),
@"class c1
{
  ///<summary>
  /// A documenation comment
  ///</summary>
  void foo()
  {
  }
}");
        }

        [Fact]
        public void TestFormatDocumentationComments2()
        {
            var code =
@"class c1
{
    ///  <summary>
    ///  A documenation comment
    ///  </summary>
    void foo()
    {
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            TestFormat(tree.GetCompilationUnitRoot(),
@"class c1
{
  ///  <summary>
  ///  A documenation comment
  ///  </summary>
  void foo()
  {
  }
}");
        }

        private void TestFormatTrivia(string text, string expected)
        {
            var list = SyntaxFactory.ParseLeadingTrivia(text);
            var actual = SyntaxFormatter.Format(list, "    ").ToFullString();
            Assert.Equal(expected, actual);
        }

        private void TestFormat(CSharpSyntaxNode node, string expected)
        {
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected, actual);
        }

        private void TestFormat(SyntaxTriviaList trivia, string expected)
        {
            var actual = SyntaxFormatter.Format(trivia, "    ").ToFullString();
            Assert.Equal(expected, actual);
        }
    }
}
