// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxNormalizerTests
    {
        [Fact]
        public void TestNormalizeExpression1()
        {
            TestNormalizeExpression("!a", "!a");
            TestNormalizeExpression("-a", "-a");
            TestNormalizeExpression("+a", "+a");
            TestNormalizeExpression("~a", "~a");

            TestNormalizeExpression("a", "a");
            TestNormalizeExpression("a+b", "a + b");
            TestNormalizeExpression("a-b", "a - b");
            TestNormalizeExpression("a*b", "a * b");
            TestNormalizeExpression("a/b", "a / b");
            TestNormalizeExpression("a%b", "a % b");
            TestNormalizeExpression("a^b", "a ^ b");
            TestNormalizeExpression("a|b", "a | b");
            TestNormalizeExpression("a&b", "a & b");
            TestNormalizeExpression("a||b", "a || b");
            TestNormalizeExpression("a&&b", "a && b");
            TestNormalizeExpression("a<b", "a < b");
            TestNormalizeExpression("a<=b", "a <= b");
            TestNormalizeExpression("a>b", "a > b");
            TestNormalizeExpression("a>=b", "a >= b");
            TestNormalizeExpression("a==b", "a == b");
            TestNormalizeExpression("a!=b", "a != b");
            TestNormalizeExpression("a<<b", "a << b");
            TestNormalizeExpression("a>>b", "a >> b");
            TestNormalizeExpression("a??b", "a ?? b");

            TestNormalizeExpression("a<b>.c", "a<b>.c");
            TestNormalizeExpression("(a+b)", "(a + b)");
            TestNormalizeExpression("((a)+(b))", "((a) + (b))");
            TestNormalizeExpression("(a)b", "(a)b");
            TestNormalizeExpression("(a)(b)", "(a)(b)");

            TestNormalizeExpression("m()", "m()");
            TestNormalizeExpression("m(a)", "m(a)");
            TestNormalizeExpression("m(a,b)", "m(a, b)");
            TestNormalizeExpression("m(a,b,c)", "m(a, b, c)");
            TestNormalizeExpression("m(a,b(c,d))", "m(a, b(c, d))");

            TestNormalizeExpression("a?b:c", "a ? b : c");
            TestNormalizeExpression("from a in b where c select d", "from a in b\r\nwhere c\r\nselect d");

            TestNormalizeExpression("a().b().c()", "a().b().c()");
            TestNormalizeExpression("a->b->c", "a->b->c");
            TestNormalizeExpression("global :: a", "global::a");

            TestNormalizeExpression("(IList<int>)args", "(IList<int>)args");
            TestNormalizeExpression("(IList<IList<int>>)args", "(IList<IList<int>>)args");
        }

        private void TestNormalizeExpression(string text, string expected)
        {
            var node = SyntaxFactory.ParseExpression(text);
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestNormalizeStatement1()
        {
            // expressions
            TestNormalizeStatement("a;", "a;");

            // blocks
            TestNormalizeStatement("{a;}", "{\r\n  a;\r\n}");
            TestNormalizeStatement("{a;b;}", "{\r\n  a;\r\n  b;\r\n}");
            TestNormalizeStatement("\t{a;}", "{\r\n  a;\r\n}");
            TestNormalizeStatement("\t{a;b;}", "{\r\n  a;\r\n  b;\r\n}");

            // if
            TestNormalizeStatement("if(a)b;", "if (a)\r\n  b;");
            TestNormalizeStatement("if(a){b;}", "if (a)\r\n{\r\n  b;\r\n}");
            TestNormalizeStatement("if(a){b;c;}", "if (a)\r\n{\r\n  b;\r\n  c;\r\n}");
            TestNormalizeStatement("if(a)b;else c;", "if (a)\r\n  b;\r\nelse\r\n  c;");
            TestNormalizeStatement("if(a)b;else if(c)d;", "if (a)\r\n  b;\r\nelse if (c)\r\n  d;");

            // while
            TestNormalizeStatement("while(a)b;", "while (a)\r\n  b;");
            TestNormalizeStatement("while(a){b;}", "while (a)\r\n{\r\n  b;\r\n}");

            // do 
            TestNormalizeStatement("do{a;}while(b);", "do\r\n{\r\n  a;\r\n}\r\nwhile (b);");

            // for
            TestNormalizeStatement("for(a;b;c)d;", "for (a; b; c)\r\n  d;");
            TestNormalizeStatement("for(;;)a;", "for (;;)\r\n  a;");

            // foreach
            TestNormalizeStatement("foreach(a in b)c;", "foreach (a in b)\r\n  c;");

            // try
            TestNormalizeStatement("try{a;}catch(b){c;}", "try\r\n{\r\n  a;\r\n}\r\ncatch (b)\r\n{\r\n  c;\r\n}");
            TestNormalizeStatement("try{a;}finally{b;}", "try\r\n{\r\n  a;\r\n}\r\nfinally\r\n{\r\n  b;\r\n}");

            // other
            TestNormalizeStatement("lock(a)b;", "lock (a)\r\n  b;");
            TestNormalizeStatement("fixed(a)b;", "fixed (a)\r\n  b;");
            TestNormalizeStatement("using(a)b;", "using (a)\r\n  b;");
            TestNormalizeStatement("checked{a;}", "checked\r\n{\r\n  a;\r\n}");
            TestNormalizeStatement("unchecked{a;}", "unchecked\r\n{\r\n  a;\r\n}");
            TestNormalizeStatement("unsafe{a;}", "unsafe\r\n{\r\n  a;\r\n}");

            // declaration statements
            TestNormalizeStatement("a b;", "a b;");
            TestNormalizeStatement("a?b;", "a? b;");
            TestNormalizeStatement("a b,c;", "a b, c;");
            TestNormalizeStatement("a b=c;", "a b = c;");
            TestNormalizeStatement("a b=c,d=e;", "a b = c, d = e;");

            // empty statements
            TestNormalizeStatement(";", ";");
            TestNormalizeStatement("{;;}", "{\r\n  ;\r\n  ;\r\n}");

            // labelled statements
            TestNormalizeStatement("foo:;", "foo:\r\n  ;");
            TestNormalizeStatement("foo:a;", "foo:\r\n  a;");

            // return/goto
            TestNormalizeStatement("return;", "return;");
            TestNormalizeStatement("return(a);", "return (a);");
            TestNormalizeStatement("continue;", "continue;");
            TestNormalizeStatement("break;", "break;");
            TestNormalizeStatement("yield return;", "yield return;");
            TestNormalizeStatement("yield return(a);", "yield return (a);");
            TestNormalizeStatement("yield break;", "yield break;");
            TestNormalizeStatement("goto a;", "goto a;");
            TestNormalizeStatement("throw;", "throw;");
            TestNormalizeStatement("throw a;", "throw a;");
            TestNormalizeStatement("return this.Bar()", "return this.Bar()");

            // switch
            TestNormalizeStatement("switch(a){case b:c;}", "switch (a)\r\n{\r\n  case b:\r\n    c;\r\n}");
            TestNormalizeStatement("switch(a){case b:c;case d:e;}", "switch (a)\r\n{\r\n  case b:\r\n    c;\r\n  case d:\r\n    e;\r\n}");
            TestNormalizeStatement("switch(a){case b:c;default:d;}", "switch (a)\r\n{\r\n  case b:\r\n    c;\r\n  default:\r\n    d;\r\n}");
            TestNormalizeStatement("switch(a){case b:{}default:{}}", "switch (a)\r\n{\r\n  case b:\r\n  {\r\n  }\r\n\r\n  default:\r\n  {\r\n  }\r\n}");
            TestNormalizeStatement("switch(a){case b:c();d();default:e();f();}", "switch (a)\r\n{\r\n  case b:\r\n    c();\r\n    d();\r\n  default:\r\n    e();\r\n    f();\r\n}");
            TestNormalizeStatement("switch(a){case b:{c();}}", "switch (a)\r\n{\r\n  case b:\r\n  {\r\n    c();\r\n  }\r\n}");

            // curlies
            TestNormalizeStatement("{if(foo){}if(bar){}}", "{\r\n  if (foo)\r\n  {\r\n  }\r\n\r\n  if (bar)\r\n  {\r\n  }\r\n}");

            // Queries
            TestNormalizeStatement("int i=from v in vals select v;", "int i =\r\n  from v in vals\r\n  select v;");
            TestNormalizeStatement("Foo(from v in vals select v);", "Foo(\r\n  from v in vals\r\n  select v);");
            TestNormalizeStatement("int i=from v in vals select from x in xxx where x > 10 select x;", "int i =\r\n  from v in vals\r\n  select\r\n    from x in xxx\r\n    where x > 10\r\n    select x;");
            TestNormalizeStatement("int i=from v in vals group v by x into g where g > 10 select g;", "int i =\r\n  from v in vals\r\n  group v by x into g\r\n    where g > 10\r\n    select g;");

            // Generics
            TestNormalizeStatement("Func<string, int> f = blah;", "Func<string, int> f = blah;");
        }

        private void TestNormalizeStatement(string text, string expected)
        {
            var node = SyntaxFactory.ParseStatement(text);
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestNormalizeDeclaration1()
        {
            // usings
            TestNormalizeDeclaration("using a;", "using a;");
            TestNormalizeDeclaration("using a=b;", "using a = b;");
            TestNormalizeDeclaration("using a.b;", "using a.b;");
            TestNormalizeDeclaration("using A; using B; class C {}", "using A;\r\nusing B;\r\n\r\nclass C\r\n{\r\n}");

            // namespace
            TestNormalizeDeclaration("namespace a{}", "namespace a\r\n{\r\n}");
            TestNormalizeDeclaration("namespace a{using b;}", "namespace a\r\n{\r\n  using b;\r\n}");
            TestNormalizeDeclaration("namespace a{namespace b{}}", "namespace a\r\n{\r\n  namespace b\r\n  {\r\n  }\r\n}");
            TestNormalizeDeclaration("namespace a{}namespace b{}", "namespace a\r\n{\r\n}\r\n\r\nnamespace b\r\n{\r\n}");

            // type
            TestNormalizeDeclaration("class a{}", "class a\r\n{\r\n}");
            TestNormalizeDeclaration("class a{class b{}}", "class a\r\n{\r\n  class b\r\n  {\r\n  }\r\n}");
            TestNormalizeDeclaration("class a<b>where a:c{}", "class a<b>\r\n  where a : c\r\n{\r\n}");
            TestNormalizeDeclaration("class a<b,c>where a:c{}", "class a<b, c>\r\n  where a : c\r\n{\r\n}");
            TestNormalizeDeclaration("class a:b{}", "class a : b\r\n{\r\n}");

            // methods
            TestNormalizeDeclaration("class a{void b(){}}", "class a\r\n{\r\n  void b()\r\n  {\r\n  }\r\n}");
            TestNormalizeDeclaration("class a{void b(){}void c(){}}", "class a\r\n{\r\n  void b()\r\n  {\r\n  }\r\n\r\n  void c()\r\n  {\r\n  }\r\n}");
            TestNormalizeDeclaration("class a{a(){}}", "class a\r\n{\r\n  a()\r\n  {\r\n  }\r\n}");
            TestNormalizeDeclaration("class a{~a(){}}", "class a\r\n{\r\n  ~a()\r\n  {\r\n  }\r\n}");

            // properties
            TestNormalizeDeclaration("class a{b c{get;}}", "class a\r\n{\r\n  b c\r\n  {\r\n    get;\r\n  }\r\n}");

            // indexers
            TestNormalizeDeclaration("class a{b this[c d]{get;}}", "class a\r\n{\r\n  b this[c d]\r\n  {\r\n    get;\r\n  }\r\n}");

            // fields
            TestNormalizeDeclaration("class a{b c;}", "class a\r\n{\r\n  b c;\r\n}");
            TestNormalizeDeclaration("class a{b c=d;}", "class a\r\n{\r\n  b c = d;\r\n}");
            TestNormalizeDeclaration("class a{b c=d,e=f;}", "class a\r\n{\r\n  b c = d, e = f;\r\n}");

            // delegate
            TestNormalizeDeclaration("delegate a b();", "delegate a b();");
            TestNormalizeDeclaration("delegate a b(c);", "delegate a b(c);");
            TestNormalizeDeclaration("delegate a b(c,d);", "delegate a b(c, d);");

            // enums
            TestNormalizeDeclaration("enum a{}", "enum a\r\n{\r\n}");
            TestNormalizeDeclaration("enum a{b}", "enum a\r\n{\r\n  b\r\n}");
            TestNormalizeDeclaration("enum a{b,c}", "enum a\r\n{\r\n  b,\r\n  c\r\n}");
            TestNormalizeDeclaration("enum a{b=c}", "enum a\r\n{\r\n  b = c\r\n}");

            // attributes
            TestNormalizeDeclaration("[a]class b{}", "[a]\r\nclass b\r\n{\r\n}");
            TestNormalizeDeclaration("\t[a]class b{}", "[a]\r\nclass b\r\n{\r\n}");
            TestNormalizeDeclaration("[a,b]class c{}", "[a, b]\r\nclass c\r\n{\r\n}");
            TestNormalizeDeclaration("[a(b)]class c{}", "[a(b)]\r\nclass c\r\n{\r\n}");
            TestNormalizeDeclaration("[a(b,c)]class d{}", "[a(b, c)]\r\nclass d\r\n{\r\n}");
            TestNormalizeDeclaration("[a][b]class c{}", "[a]\r\n[b]\r\nclass c\r\n{\r\n}");
            TestNormalizeDeclaration("[a:b]class c{}", "[a: b]\r\nclass c\r\n{\r\n}");

            // parameter attributes
            TestNormalizeDeclaration("class c{void M([a]int x,[b] [c,d]int y){}}", "class c\r\n{\r\n  void M([a] int x, [b][c, d] int y)\r\n  {\r\n  }\r\n}");
        }

        [WorkItem(541684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541684")]
        [Fact]
        public void TestNormalizeRegion1()
        {
            // NOTE: the space after the region name is retained, since the text after the space
            // following "#region" is a single, unstructured trivia element.
            TestNormalizeDeclaration(
                "\r\nclass Class \r\n{ \r\n#region Methods \r\nvoid Method() \r\n{ \r\n} \r\n#endregion \r\n}",
                "class Class\r\n{\r\n#region Methods \r\n  void Method()\r\n  {\r\n  }\r\n#endregion\r\n}");
            TestNormalizeDeclaration(
                "\r\n#region\r\n#endregion",
                "#region\r\n#endregion\r\n");
            TestNormalizeDeclaration(
                "\r\n#region  \r\n#endregion",
                "#region\r\n#endregion\r\n");
            TestNormalizeDeclaration(
                "\r\n#region name //comment\r\n#endregion",
                "#region name //comment\r\n#endregion\r\n");
            TestNormalizeDeclaration(
                "\r\n#region /*comment*/\r\n#endregion",
                "#region /*comment*/\r\n#endregion\r\n");
        }

        [WorkItem(2076, "github")]
        [Fact]
        public void TestNormalizeInterpolatedString()
        {
            TestNormalizeExpression(@"$""Message is {a}""", @"$""Message is {a}""");
        }

        [WorkItem(528584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528584")]
        [Fact]
        public void TestNormalizeRegion2()
        {
            TestNormalizeDeclaration(
                "\r\n#region //comment\r\n#endregion",
                // NOTE: the extra newline should be removed, but it's not worth the
                // effort (see DevDiv #8564)
                "#region //comment\r\n\r\n#endregion\r\n");
            TestNormalizeDeclaration(
                "\r\n#region //comment\r\n\r\n#endregion",
                // NOTE: the extra newline should be removed, but it's not worth the
                // effort (see DevDiv #8564).
                "#region //comment\r\n\r\n#endregion\r\n");
        }

        private void TestNormalizeDeclaration(string text, string expected)
        {
            var node = SyntaxFactory.ParseCompilationUnit(text);
            Assert.Equal(text, node.ToFullString());
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestNormalizeComments()
        {
            TestNormalizeToken("a//b", "a //b\r\n");
            TestNormalizeToken("a/*b*/", "a /*b*/");
            TestNormalizeToken("//a\r\nb", "//a\r\nb");
            TestNormalizeExpression("a/*b*/+c", "a /*b*/ + c");
            TestNormalizeExpression("/*a*/b", "/*a*/\r\nb");
            TestNormalizeExpression("/*a\r\n*/b", "/*a\r\n*/\r\nb");
            TestNormalizeStatement("{/*a*/b}", "{ /*a*/\r\n  b\r\n}");
            TestNormalizeStatement("{\r\na//b\r\n}", "{\r\n  a //b\r\n}");
            TestNormalizeStatement("{\r\n//a\r\n}", "{\r\n//a\r\n}");
            TestNormalizeStatement("{\r\n//a\r\nb}", "{\r\n  //a\r\n  b\r\n}");
            TestNormalizeStatement("{\r\n/*a*/b}", "{\r\n  /*a*/\r\n  b\r\n}");
            TestNormalizeStatement("{\r\n/// <foo/>\r\na}", "{\r\n  /// <foo/>\r\n  a\r\n}");
            TestNormalizeStatement("{\r\n///<foo/>\r\na}", "{\r\n  ///<foo/>\r\n  a\r\n}");
            TestNormalizeStatement("{\r\n/// <foo>\r\n/// </foo>\r\na}", "{\r\n  /// <foo>\r\n  /// </foo>\r\n  a\r\n}");
            TestNormalizeToken("/// <foo>\r\n/// </foo>\r\na", "/// <foo>\r\n/// </foo>\r\na");
            TestNormalizeStatement("{\r\n/*** <foo/> ***/\r\na}", "{\r\n  /*** <foo/> ***/\r\n  a\r\n}");
            TestNormalizeStatement("{\r\n/*** <foo/>\r\n ***/\r\na}", "{\r\n  /*** <foo/>\r\n ***/\r\n  a\r\n}");
        }

        private void TestNormalizeToken(string text, string expected)
        {
            var token = SyntaxFactory.ParseToken(text);
            var actual = token.NormalizeWhitespace().ToFullString();
            Assert.Equal(expected, actual);
        }

        [ClrOnlyFact]
        [WorkItem(1066, "github")]
        public void TestNormalizePreprocessorDirectives()
        {
            // directive as node
            TestNormalize(SyntaxFactory.DefineDirectiveTrivia(SyntaxFactory.Identifier("a"), false), "#define a\r\n");

            // directive as trivia
            TestNormalizeTrivia("  #  define a", "#define a\r\n");
            TestNormalizeTrivia("#if(a||b)", "#if (a || b)\r\n");
            TestNormalizeTrivia("#if(a&&b)", "#if (a && b)\r\n");
            TestNormalizeTrivia("  #if a\r\n  #endif", "#if a\r\n#endif\r\n");

            TestNormalize(
                SyntaxFactory.TriviaList(
                    SyntaxFactory.Trivia(
                        SyntaxFactory.IfDirectiveTrivia(SyntaxFactory.IdentifierName("a"), false, false, false)),
                    SyntaxFactory.Trivia(
                        SyntaxFactory.EndIfDirectiveTrivia(false))),
                "#if a\r\n#endif\r\n");

            TestNormalizeTrivia("#endregion foo", "#endregion foo\r\n");

            TestNormalizeDeclaration(
@"#pragma warning disable 123

namespace foo {
}

#pragma warning restore 123",
@"#pragma warning disable 123
namespace foo
{
}
#pragma warning restore 123
");
        }

        [ClrOnlyFact]
        [WorkItem(531607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531607")]
        public void TestNormalizeLineDirectiveTrivia()
        {
            TestNormalize(
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

        [WorkItem(538115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538115")]
        [Fact]
        public void TestNormalizeWithinDirectives()
        {
            TestNormalizeDeclaration(
"class C\r\n{\r\n#if true\r\nvoid Foo(A x) { }\r\n#else\r\n#endif\r\n}\r\n",
"class C\r\n{\r\n#if true\r\n  void Foo(A x)\r\n  {\r\n  }\r\n#else\r\n#endif\r\n}");
        }

        [WorkItem(542887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542887")]
        [ClrOnlyFact]
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
            TestNormalize(tree.GetCompilationUnitRoot(),
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

        [WorkItem(1079042, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079042")]
        [ClrOnlyFact]
        public void TestNormalizeDocumentationComments()
        {
            var code =
@"class c1
{
    ///<summary>
    /// A documentation comment
    ///</summary>
    void foo()
    {
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            TestNormalize(tree.GetCompilationUnitRoot(),
@"class c1
{
  ///<summary>
  /// A documentation comment
  ///</summary>
  void foo()
  {
  }
}");
        }

        [ClrOnlyFact]
        public void TestNormalizeDocumentationComments2()
        {
            var code =
@"class c1
{
    ///  <summary>
    ///  A documentation comment
    ///  </summary>
    void foo()
    {
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            TestNormalize(tree.GetCompilationUnitRoot(),
@"class c1
{
  ///  <summary>
  ///  A documentation comment
  ///  </summary>
  void foo()
  {
  }
}");
        }

        [Fact]
        public void TestNormalizeEOL()
        {
            var code = "class c{}";
            var expected = "class c\n{\n}";
            var actual = SyntaxFactory.ParseCompilationUnit(code).NormalizeWhitespace(indentation: "  ", eol: "\n").ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void TestNormalizeTabs()
        {
            var code = "class c{void m(){}}";
            var expected = "class c\r\n{\r\n\tvoid m()\r\n\t{\r\n\t}\r\n}";
            var actual = SyntaxFactory.ParseCompilationUnit(code).NormalizeWhitespace(indentation: "\t").ToFullString();
            Assert.Equal(expected, actual);
        }

        private void TestNormalize(CSharpSyntaxNode node, string expected)
        {
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected, actual);
        }

        private void TestNormalizeTrivia(string text, string expected)
        {
            var list = SyntaxFactory.ParseLeadingTrivia(text);
            TestNormalize(list, expected);
        }

        private void TestNormalize(SyntaxTriviaList trivia, string expected)
        {
            var actual = trivia.NormalizeWhitespace("    ").ToFullString();
            Assert.Equal(expected, actual);
        }
    }
}
