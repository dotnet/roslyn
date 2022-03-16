// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class SyntaxNormalizerTests
    {
        [Fact, WorkItem(52543, "https://github.com/dotnet/roslyn/issues/52543")]
        public void TestNormalizePatternInIf()
        {
            TestNormalizeStatement(
                @"{object x = 1;
                if (x is {})
                {
                }
                if (x is {} t)
                {
                }
                if (x is int {} t2)
                {
                }
                if (x is System.ValueTuple<int, int>(_, _) { Item1: > 10 } t3)
                {
                }
                if (x is System.ValueTuple<int, int>(_, _) { Item1: > 10, Item2: < 20 })
                {
                }
}",
                @"{
  object x = 1;
  if (x is { })
  {
  }

  if (x is { } t)
  {
  }

  if (x is int { } t2)
  {
  }

  if (x is System.ValueTuple<int, int> (_, _) { Item1: > 10 } t3)
  {
  }

  if (x is System.ValueTuple<int, int> (_, _) { Item1: > 10, Item2: < 20 })
  {
  }
}".NormalizeLineEndings()
            );
        }

        [Fact, WorkItem(52543, "https://github.com/dotnet/roslyn/issues/52543")]
        public void TestNormalizeSwitchExpression()
        {
            TestNormalizeStatement(
                @"var x = (int)1 switch { 1 => ""one"", 2 => ""two"", 3 => ""three"", {} => "">= 4"" };",
                @"var x = (int)1 switch
{
  1 => ""one"",
  2 => ""two"",
  3 => ""three"",
  { } => "">= 4""
};".NormalizeLineEndings()
            );
        }

        [Fact]
        public void TestNormalizeSwitchExpressionRawStrings()
        {
            TestNormalizeStatement(
                @"var x = (int)1 switch { 1 => """"""one"""""", 2 => """"""two"""""", 3 => """"""three"""""", {} => """""">= 4"""""" };",
                @"var x = (int)1 switch
{
  1 => """"""one"""""",
  2 => """"""two"""""",
  3 => """"""three"""""",
  { } => """""">= 4""""""
};".NormalizeLineEndings()
            );
        }

        [Fact, WorkItem(52543, "https://github.com/dotnet/roslyn/issues/52543")]
        public void TestNormalizeSwitchRecPattern()
        {
            TestNormalizeStatement(
                @"var x = (object)1 switch {
		int { } => ""two"",
		{ } t when t.GetHashCode() == 42 => ""42"",
		System.ValueTuple<int, int> (1, _) { Item2: > 2 and < 20 } => ""tuple.Item2 < 20"",
		System.ValueTuple<int, int> (1, _) { Item2: >= 100 } greater => greater.ToString(),
		System.ValueType {} => ""not null value"",
		object {} i when i is not 42 => ""not 42"",
		{ } => ""not null"",
		null => ""null"",
};",
                @"var x = (object)1 switch
{
  int { } => ""two"",
  { } t when t.GetHashCode() == 42 => ""42"",
  System.ValueTuple<int, int> (1, _) { Item2: > 2 and < 20 } => ""tuple.Item2 < 20"",
  System.ValueTuple<int, int> (1, _) { Item2: >= 100 } greater => greater.ToString(),
  System.ValueType { } => ""not null value"",
  object { } i when i is not 42 => ""not 42"",
  { } => ""not null"",
  null => ""null"",
};".NormalizeLineEndings()
            );
        }

        [Fact, WorkItem(52543, "https://github.com/dotnet/roslyn/issues/52543")]
        public void TestNormalizeSwitchExpressionComplex()
        {
            var a = @"var x = vehicle switch
            {
                Car { Passengers: 0 } => 2.00m + 0.50m,
                Car { Passengers: 1 } => 2.0m,
                Car { Passengers: 2 } => 2.0m - 0.50m,
                Car c => 2.00m - 1.0m,

                Taxi { Fares: 0 } => 3.50m + 1.00m,
                Taxi { Fares: 1 } => 3.50m,
                Taxi { Fares: 2 } => 3.50m - 0.50m,
                Taxi t => 3.50m - 1.00m,

                Bus b when ((double)b.Riders / (double)b.Capacity) < 0.50 => 5.00m + 2.00m,
                Bus b when ((double)b.Riders / (double)b.Capacity) > 0.90 => 5.00m - 1.00m,
                Bus b => 5.00m,

                DeliveryTruck t when (t.GrossWeightClass > 5000) => 10.00m + 5.00m,
                DeliveryTruck t when (t.GrossWeightClass < 3000) => 10.00m - 2.00m,
                DeliveryTruck t => 10.00m,
                { } => -1, //throw new ArgumentException(message: ""Not a known vehicle type"", paramName: nameof(vehicle)),
                null => 0//throw new ArgumentNullException(nameof(vehicle))
            };";
            var b = @"var x = vehicle switch
{
  Car { Passengers: 0 } => 2.00m + 0.50m,
  Car { Passengers: 1 } => 2.0m,
  Car { Passengers: 2 } => 2.0m - 0.50m,
  Car c => 2.00m - 1.0m,
  Taxi { Fares: 0 } => 3.50m + 1.00m,
  Taxi { Fares: 1 } => 3.50m,
  Taxi { Fares: 2 } => 3.50m - 0.50m,
  Taxi t => 3.50m - 1.00m,
  Bus b when ((double)b.Riders / (double)b.Capacity) < 0.50 => 5.00m + 2.00m,
  Bus b when ((double)b.Riders / (double)b.Capacity) > 0.90 => 5.00m - 1.00m,
  Bus b => 5.00m,
  DeliveryTruck t when (t.GrossWeightClass > 5000) => 10.00m + 5.00m,
  DeliveryTruck t when (t.GrossWeightClass < 3000) => 10.00m - 2.00m,
  DeliveryTruck t => 10.00m,
  { } => -1, //throw new ArgumentException(message: ""Not a known vehicle type"", paramName: nameof(vehicle)),
  null => 0 //throw new ArgumentNullException(nameof(vehicle))
};".NormalizeLineEndings();
            TestNormalizeStatement(a, b);
        }

        [Fact]
        public void TestNormalizeListPattern()
        {
            var text = "_ = this is[ 1,2,.. var rest ];";
            var expected = @"_ = this is [1, 2, ..var rest];";
            TestNormalizeStatement(text, expected);
        }

        [Fact]
        public void TestNormalizeListPattern_TrailingComma()
        {
            var text = "_ = this is[ 1,2, 3,];";
            var expected = @"_ = this is [1, 2, 3, ];";
            TestNormalizeStatement(text, expected);
        }

        [Fact]
        public void TestNormalizeListPattern_EmptyList()
        {
            var text = "_ = this is[];";
            var expected = @"_ = this is [];";
            TestNormalizeStatement(text, expected);
        }

        [Fact, WorkItem(50742, "https://github.com/dotnet/roslyn/issues/50742")]
        public void TestLineBreakInterpolations()
        {
            TestNormalizeExpression(
                @"$""Printed: {                    new Printer() { TextToPrint = ""Hello world!"" }.PrintedText }""",
                @"$""Printed: {new Printer(){TextToPrint = ""Hello world!""}.PrintedText}"""
            );
        }

        [Fact]
        public void TestLineBreakRawInterpolations()
        {
            TestNormalizeExpression(
                @"$""""""Printed: {                    new Printer() { TextToPrint = ""Hello world!"" }.PrintedText }""""""",
                @"$""""""Printed: {new Printer()
{TextToPrint = ""Hello world!""}.PrintedText}""""""".Replace("\r\n", "\n").Replace("\n", "\r\n")
            );
        }

        [Fact, WorkItem(50742, "https://github.com/dotnet/roslyn/issues/50742")]
        public void TestVerbatimStringInterpolationWithLineBreaks()
        {
            TestNormalizeStatement(@"Console.WriteLine($@""Test with line
breaks
{
                new[]{
     1, 2, 3
  }[2]
}
            "");",
            @"Console.WriteLine($@""Test with line
breaks
{new[]{1, 2, 3}[2]}
            "");"
            );
        }

        [Fact]
        public void TestRawStringInterpolationWithLineBreaks()
        {
            TestNormalizeStatement(@"Console.WriteLine($""""""
            Test with line
            breaks
            {
                            new[]{
                 1, 2, 3
              }[2]
            }
            """""");",
            @"Console.WriteLine($""""""
            Test with line
            breaks
            {new[]{1, 2, 3}[2]}
            """""");"
            );
        }

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

            TestNormalizeExpression("(IList<string?>)args", "(IList<string?>)args");
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
            TestNormalizeStatement("goo:;", "goo:\r\n  ;");
            TestNormalizeStatement("goo:a;", "goo:\r\n  a;");

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
            TestNormalizeStatement("{if(goo){}if(bar){}}", "{\r\n  if (goo)\r\n  {\r\n  }\r\n\r\n  if (bar)\r\n  {\r\n  }\r\n}");

            // Queries
            TestNormalizeStatement("int i=from v in vals select v;", "int i =\r\n  from v in vals\r\n  select v;");
            TestNormalizeStatement("Goo(from v in vals select v);", "Goo(\r\n  from v in vals\r\n  select v);");
            TestNormalizeStatement("int i=from v in vals select from x in xxx where x > 10 select x;", "int i =\r\n  from v in vals\r\n  select\r\n    from x in xxx\r\n    where x > 10\r\n    select x;");
            TestNormalizeStatement("int i=from v in vals group v by x into g where g > 10 select g;", "int i =\r\n  from v in vals\r\n  group v by x into g\r\n    where g > 10\r\n    select g;");

            // Generics
            TestNormalizeStatement("Func<string, int> f = blah;", "Func<string, int> f = blah;");
        }

        [Theory]
        [InlineData("[ return:A ]void Local( [ B ]object o){}", "[return: A]\r\nvoid Local([B] object o)\r\n{\r\n}")]
        [InlineData("[A,B][C]T Local<T>()=>default;", "[A, B]\r\n[C]\r\nT Local<T>() => default;")]
        public void TestLocalFunctionAttributes(string text, string expected)
        {
            TestNormalizeStatement(text, expected);
        }

        [Theory]
        [InlineData("( [ A ]x)=>x", "([A] x) => x")]
        [InlineData("[return:A]([B]object o)=>{}", "[return: A]\r\n([B] object o) =>\r\n{\r\n}")]
        [InlineData("[ A ,B ] [C]()=>x", "[A, B]\r\n[C]\r\n() => x")]
        [InlineData("[A]B()=>{ }", "[A]\r\nB() =>\r\n{\r\n}")]
        [WorkItem(59653, "https://github.com/dotnet/roslyn/issues/59653")]
        public void TestLambdaAttributes(string text, string expected)
        {
            TestNormalizeExpression(text, expected);
        }

        [Theory]
        [InlineData("int( x )=>x", "int (x) => x")]
        [InlineData("A( B b )=>{}", "A(B b) =>\r\n{\r\n}")]
        [InlineData("static\r\nasync\r\nA<int>()=>x", "static async A<int>() => x")]
        [WorkItem(59653, "https://github.com/dotnet/roslyn/issues/59653")]
        public void TestLambdaReturnType(string text, string expected)
        {
            TestNormalizeExpression(text, expected);
        }

        [Theory]
        [InlineData("int*p;", "int* p;")]
        [InlineData("int *p;", "int* p;")]
        [InlineData("int*p1,p2;", "int* p1, p2;")]
        [InlineData("int *p1, p2;", "int* p1, p2;")]
        [InlineData("int**p;", "int** p;")]
        [InlineData("int **p;", "int** p;")]
        [InlineData("int**p1,p2;", "int** p1, p2;")]
        [InlineData("int **p1, p2;", "int** p1, p2;")]
        [WorkItem(49733, "https://github.com/dotnet/roslyn/issues/49733")]
        public void TestNormalizeAsteriskInPointerDeclaration(string text, string expected)
        {
            TestNormalizeStatement(text, expected);
        }

        [Fact]
        [WorkItem(49733, "https://github.com/dotnet/roslyn/issues/49733")]
        public void TestNormalizeAsteriskInPointerReturnTypeOfIndexer()
        {
            var text = @"public unsafe class C
{
  int*this[int x,int y]{get=>(int*)0;}
}";
            var expected = @"public unsafe class C
{
  int* this[int x, int y] { get => (int*)0; }
}";
            TestNormalizeDeclaration(text, expected);
        }

        [Fact]
        public void TestNormalizeAsteriskInVoidPointerCast()
        {
            var text = @"public unsafe class C
{
  void*this[int x,int y]{get   =>  (  void  *   ) 0;}
}";
            var expected = @"public unsafe class C
{
  void* this[int x, int y] { get => (void*)0; }
}";
            TestNormalizeDeclaration(text, expected);
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

            TestNormalizeDeclaration("global  using  a;", "global using a;");
            TestNormalizeDeclaration("global  using  a=b;", "global using a = b;");
            TestNormalizeDeclaration("global  using  a.b;", "global using a.b;");
            TestNormalizeDeclaration("global using A; global using B; class C {}", "global using A;\r\nglobal using B;\r\n\r\nclass C\r\n{\r\n}");
            TestNormalizeDeclaration("global using A; using B; class C {}", "global using A;\r\nusing B;\r\n\r\nclass C\r\n{\r\n}");
            TestNormalizeDeclaration("using A; global using B; class C {}", "using A;\r\nglobal using B;\r\n\r\nclass C\r\n{\r\n}");

            // namespace
            TestNormalizeDeclaration("namespace a{}", "namespace a\r\n{\r\n}");
            TestNormalizeDeclaration("namespace a{using b;}", "namespace a\r\n{\r\n  using b;\r\n}");
            TestNormalizeDeclaration("namespace a{global  using  b;}", "namespace a\r\n{\r\n  global using b;\r\n}");
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
            TestNormalizeDeclaration("class a{b c{get;}}", "class a\r\n{\r\n  b c { get; }\r\n}");
            TestNormalizeDeclaration("class a {\r\nint X{get;set;}= 2;\r\n}\r\n", "class a\r\n{\r\n  int X { get; set; } = 2;\r\n}");
            TestNormalizeDeclaration("class a {\r\nint Y\r\n{get;\r\nset;\r\n}\r\n=99;\r\n}\r\n", "class a\r\n{\r\n  int Y { get; set; } = 99;\r\n}");
            TestNormalizeDeclaration("class a {\r\nint Z{get;}\r\n}\r\n", "class a\r\n{\r\n  int Z { get; }\r\n}");
            TestNormalizeDeclaration("class a {\r\nint T{get;init;}\r\nint R{get=>1;}\r\n}\r\n", "class a\r\n{\r\n  int T { get; init; }\r\n\r\n  int R { get => 1; }\r\n}");
            TestNormalizeDeclaration("class a {\r\nint Q{get{return 0;}init{}}\r\nint R{get=>1;}\r\n}\r\n", "class a\r\n{\r\n  int Q\r\n  {\r\n    get\r\n    {\r\n      return 0;\r\n    }\r\n\r\n    init\r\n    {\r\n    }\r\n  }\r\n\r\n  int R { get => 1; }\r\n}");
            TestNormalizeDeclaration("class a {\r\nint R{get=>1;}\r\n}\r\n", "class a\r\n{\r\n  int R { get => 1; }\r\n}");
            TestNormalizeDeclaration("class a {\r\nint S=>2;\r\n}\r\n", "class a\r\n{\r\n  int S => 2;\r\n}");
            TestNormalizeDeclaration("class x\r\n{\r\nint _g;\r\nint G\r\n{\r\nget\r\n{\r\nreturn\r\n_g;\r\n}\r\ninit;\r\n}\r\nint H\r\n{\r\nget;\r\nset\r\n{\r\n_g\r\n=\r\n12;\r\n}\r\n}\r\n}\r\n",
                "class x\r\n{\r\n  int _g;\r\n  int G\r\n  {\r\n    get\r\n    {\r\n      return _g;\r\n    }\r\n\r\n    init;\r\n  }\r\n\r\n  int H\r\n  {\r\n    get;\r\n    set\r\n    {\r\n      _g = 12;\r\n    }\r\n  }\r\n}");

            TestNormalizeDeclaration("class i1\r\n{\r\nint\r\np\r\n{\r\nget;\r\n}\r\n}", "class i1\r\n{\r\n  int p { get; }\r\n}");
            TestNormalizeDeclaration("class i2\r\n{\r\nint\r\np\r\n{\r\nget=>2;\r\n}\r\n}", "class i2\r\n{\r\n  int p { get => 2; }\r\n}");
            TestNormalizeDeclaration("class i2a\r\n{\r\nint _p;\r\nint\r\np\r\n{\r\nget=>\r\n_p;set\r\n=>_p\r\n=value\r\n;\r\n}\r\n}", "class i2a\r\n{\r\n  int _p;\r\n  int p { get => _p; set => _p = value; }\r\n}");
            TestNormalizeDeclaration("class i3\r\n{\r\nint\r\np\r\n{\r\nget{}\r\n}\r\n}", "class i3\r\n{\r\n  int p\r\n  {\r\n    get\r\n    {\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class i4\r\n{\r\nint\r\np\r\n{\r\nset;\r\n}\r\n}", "class i4\r\n{\r\n  int p { set; }\r\n}");
            TestNormalizeDeclaration("class i5\r\n{\r\nint\r\np\r\n{\r\nset{}\r\n}\r\n}", "class i5\r\n{\r\n  int p\r\n  {\r\n    set\r\n    {\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class i6\r\n{\r\nint\r\np\r\n{\r\ninit;\r\n}\r\n}", "class i6\r\n{\r\n  int p { init; }\r\n}");
            TestNormalizeDeclaration("class i7\r\n{\r\nint\r\np\r\n{\r\ninit{}\r\n}\r\n}", "class i7\r\n{\r\n  int p\r\n  {\r\n    init\r\n    {\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class i8\r\n{\r\nint\r\np\r\n{\r\nget{}\r\nset{}\r\n}\r\n}", "class i8\r\n{\r\n  int p\r\n  {\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class i9\r\n{\r\nint\r\np\r\n{\r\nget=>1;\r\nset{z=1;}\r\n}\r\n}", "class i9\r\n{\r\n  int p\r\n  {\r\n    get => 1;\r\n    set\r\n    {\r\n      z = 1;\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class ia\r\n{\r\nint\r\np\r\n{\r\nget{}\r\nset;\r\n}\r\n}", "class ia\r\n{\r\n  int p\r\n  {\r\n    get\r\n    {\r\n    }\r\n\r\n    set;\r\n  }\r\n}");
            TestNormalizeDeclaration("class ib\r\n{\r\nint\r\np\r\n{\r\nget;\r\nset{}\r\n}\r\n}", "class ib\r\n{\r\n  int p\r\n  {\r\n    get;\r\n    set\r\n    {\r\n    }\r\n  }\r\n}");

            // properties with initializers
            TestNormalizeDeclaration("class i4\r\n{\r\nint\r\np\r\n{\r\nset;\r\n}=1;\r\n}", "class i4\r\n{\r\n  int p { set; } = 1;\r\n}");
            TestNormalizeDeclaration("class i5\r\n{\r\nint\r\np\r\n{\r\nset{}\r\n}=1;\r\n}", "class i5\r\n{\r\n  int p\r\n  {\r\n    set\r\n    {\r\n    }\r\n  } = 1;\r\n}");
            TestNormalizeDeclaration("class i6\r\n{\r\nint\r\np\r\n{\r\ninit;\r\n}=1;\r\n}", "class i6\r\n{\r\n  int p { init; } = 1;\r\n}");
            TestNormalizeDeclaration("class i7\r\n{\r\nint\r\np\r\n{\r\ninit{}\r\n}=1;\r\n}", "class i7\r\n{\r\n  int p\r\n  {\r\n    init\r\n    {\r\n    }\r\n  } = 1;\r\n}");
            TestNormalizeDeclaration("class i8\r\n{\r\nint\r\np\r\n{\r\nget{}\r\nset{}\r\n}=1;\r\n}", "class i8\r\n{\r\n  int p\r\n  {\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n  } = 1;\r\n}");
            TestNormalizeDeclaration("class i9\r\n{\r\nint\r\np\r\n{\r\nget=>1;\r\nset{z=1;}\r\n}=1;\r\n}", "class i9\r\n{\r\n  int p\r\n  {\r\n    get => 1;\r\n    set\r\n    {\r\n      z = 1;\r\n    }\r\n  } = 1;\r\n}");
            TestNormalizeDeclaration("class ia\r\n{\r\nint\r\np\r\n{\r\nget{}\r\nset;\r\n}=1;\r\n}", "class ia\r\n{\r\n  int p\r\n  {\r\n    get\r\n    {\r\n    }\r\n\r\n    set;\r\n  } = 1;\r\n}");
            TestNormalizeDeclaration("class ib\r\n{\r\nint\r\np\r\n{\r\nget;\r\nset{}\r\n}=1;\r\n}", "class ib\r\n{\r\n  int p\r\n  {\r\n    get;\r\n    set\r\n    {\r\n    }\r\n  } = 1;\r\n}");

            // indexers
            TestNormalizeDeclaration("class a{b this[c d]{get;}}", "class a\r\n{\r\n  b this[c d] { get; }\r\n}");
            TestNormalizeDeclaration("class i1\r\n{\r\nint\r\nthis[b c]\r\n{\r\nget;\r\n}\r\n}", "class i1\r\n{\r\n  int this[b c] { get; }\r\n}");
            TestNormalizeDeclaration("class i2\r\n{\r\nint\r\nthis[b c]\r\n{\r\nget=>1;\r\n}\r\n}", "class i2\r\n{\r\n  int this[b c] { get => 1; }\r\n}");
            TestNormalizeDeclaration("class i3\r\n{\r\nint\r\nthis[b c]\r\n{\r\nget{}\r\n}\r\n}", "class i3\r\n{\r\n  int this[b c]\r\n  {\r\n    get\r\n    {\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class i4\r\n{\r\nint\r\nthis[b c]\r\n{\r\nset;\r\n}\r\n}", "class i4\r\n{\r\n  int this[b c] { set; }\r\n}");
            TestNormalizeDeclaration("class i5\r\n{\r\nint\r\nthis[b c]\r\n{\r\nset{}\r\n}\r\n}", "class i5\r\n{\r\n  int this[b c]\r\n  {\r\n    set\r\n    {\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class i6\r\n{\r\nint\r\nthis[b c]\r\n{\r\ninit;\r\n}\r\n}", "class i6\r\n{\r\n  int this[b c] { init; }\r\n}");
            TestNormalizeDeclaration("class i7\r\n{\r\nint\r\nthis[b c]\r\n{\r\ninit{}\r\n}\r\n}", "class i7\r\n{\r\n  int this[b c]\r\n  {\r\n    init\r\n    {\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class i8\r\n{\r\nint\r\nthis[b c]\r\n{\r\nget{}\r\nset{}\r\n}\r\n}", "class i8\r\n{\r\n  int this[b c]\r\n  {\r\n    get\r\n    {\r\n    }\r\n\r\n    set\r\n    {\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class i9\r\n{\r\nint\r\nthis[b c]\r\n{\r\nget=>1;\r\nset{z=1;}\r\n}\r\n}", "class i9\r\n{\r\n  int this[b c]\r\n  {\r\n    get => 1;\r\n    set\r\n    {\r\n      z = 1;\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class ia\r\n{\r\nint\r\nthis[b c]\r\n{\r\nget{}\r\nset;\r\n}\r\n}", "class ia\r\n{\r\n  int this[b c]\r\n  {\r\n    get\r\n    {\r\n    }\r\n\r\n    set;\r\n  }\r\n}");
            TestNormalizeDeclaration("class ib\r\n{\r\nint\r\nthis[b c]\r\n{\r\nget;\r\nset{}\r\n}\r\n}", "class ib\r\n{\r\n  int this[b c]\r\n  {\r\n    get;\r\n    set\r\n    {\r\n    }\r\n  }\r\n}");

            // events
            TestNormalizeDeclaration("class a\r\n{\r\npublic\r\nevent\r\nw\r\ne;\r\n}", "class a\r\n{\r\n  public event w e;\r\n}");
            TestNormalizeDeclaration("abstract class b\r\n{\r\nevent\r\nw\r\ne\r\n;\r\n}", "abstract class b\r\n{\r\n  event w e;\r\n}");
            TestNormalizeDeclaration("interface c1\r\n{\r\nevent\r\nw\r\ne\r\n;\r\n}", "interface c1\r\n{\r\n  event w e;\r\n}");
            TestNormalizeDeclaration("interface c2 : c1\r\n{\r\nabstract\r\nevent\r\nw\r\nc1\r\n.\r\ne\r\n;\r\n}", "interface c2 : c1\r\n{\r\n  abstract event w c1.e;\r\n}");
            TestNormalizeDeclaration("class d\r\n{\r\nevent w x;\r\nevent\r\nw\r\ne\r\n{\r\nadd\r\n=>\r\nx+=\r\nvalue;\r\nremove\r\n=>x\r\n-=\r\nvalue;\r\n}}", "class d\r\n{\r\n  event w x;\r\n  event w e { add => x += value; remove => x -= value; }\r\n}");
            TestNormalizeDeclaration("class e\r\n{\r\nevent w e\r\n{\r\nadd{}\r\nremove{\r\n}\r\n}\r\n}", "class e\r\n{\r\n  event w e\r\n  {\r\n    add\r\n    {\r\n    }\r\n\r\n    remove\r\n    {\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class f\r\n{\r\nevent w x;\r\nevent w e\r\n{\r\nadd\r\n{\r\nx\r\n+=\r\nvalue;\r\n}\r\nremove\r\n{\r\nx\r\n-=\r\nvalue;\r\n}\r\n}\r\n}", "class f\r\n{\r\n  event w x;\r\n  event w e\r\n  {\r\n    add\r\n    {\r\n      x += value;\r\n    }\r\n\r\n    remove\r\n    {\r\n      x -= value;\r\n    }\r\n  }\r\n}");
            TestNormalizeDeclaration("class g\r\n{\r\nextern\r\nevent\r\nw\r\ne\r\n=\r\nnull\r\n;\r\n}", "class g\r\n{\r\n  extern event w e = null;\r\n}");
            TestNormalizeDeclaration("class h\r\n{\r\npublic event w e\r\n{\r\nadd\r\n=>\r\nc\r\n(\r\n);\r\nremove\r\n=>\r\nd(\r\n);\r\n}\r\n}", "class h\r\n{\r\n  public event w e { add => c(); remove => d(); }\r\n}");
            TestNormalizeDeclaration("class i\r\n{\r\nevent w e\r\n{\r\nadd;\r\nremove;\r\n}\r\n}", "class i\r\n{\r\n  event w e { add; remove; }\r\n}");

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

        [Fact]
        public void TestFileScopedNamespace()
        {
            TestNormalizeDeclaration("namespace NS;class C{}", "namespace NS;\r\nclass C\r\n{\r\n}");
        }

        [Fact]
        public void TestSpacingOnRecord()
        {
            TestNormalizeDeclaration("record  class  C(int I, int J);", "record class C(int I, int J);");
            TestNormalizeDeclaration("record  struct  S(int I, int J);", "record struct S(int I, int J);");
        }

        [Fact]
        [WorkItem(23618, "https://github.com/dotnet/roslyn/issues/23618")]
        public void TestSpacingOnInvocationLikeKeywords()
        {
            // no space between typeof and (
            TestNormalizeExpression("typeof (T)", "typeof(T)");

            // no space between sizeof and (
            TestNormalizeExpression("sizeof (T)", "sizeof(T)");

            // no space between default and (
            TestNormalizeExpression("default (T)", "default(T)");

            // no space between new and (
            // newline between > and where
            TestNormalizeDeclaration(
                "class C<T> where T : new() { }",
                "class C<T>\r\n  where T : new()\r\n{\r\n}");

            // no space between this and (
            TestNormalizeDeclaration(
                "class C { C() : this () { } }",
                "class C\r\n{\r\n  C() : this()\r\n  {\r\n  }\r\n}");

            // no space between base and (
            TestNormalizeDeclaration(
                "class C { C() : base () { } }",
                "class C\r\n{\r\n  C() : base()\r\n  {\r\n  }\r\n}");

            // no space between checked and (
            TestNormalizeExpression("checked (a)", "checked(a)");

            // no space between unchecked and (
            TestNormalizeExpression("unchecked (a)", "unchecked(a)");

            // no space between __arglist and (
            TestNormalizeExpression("__arglist (a)", "__arglist(a)");
        }

        [Fact]
        [WorkItem(24454, "https://github.com/dotnet/roslyn/issues/24454")]
        public void TestSpacingOnInterpolatedString()
        {
            TestNormalizeExpression("$\"{3:C}\"", "$\"{3:C}\"");
            TestNormalizeExpression("$\"{3: C}\"", "$\"{3: C}\"");
        }

        [Fact]
        public void TestSpacingOnRawInterpolatedString()
        {
            TestNormalizeExpression("$\"\"\"{3:C}\"\"\"", "$\"\"\"{3:C}\"\"\"");
            TestNormalizeExpression("$\"\"\"{3: C}\"\"\"", "$\"\"\"{3: C}\"\"\"");
            TestNormalizeExpression("$\"\"\"{3:C }\"\"\"", "$\"\"\"{3:C }\"\"\"");
            TestNormalizeExpression("$\"\"\"{3: C }\"\"\"", "$\"\"\"{3: C }\"\"\"");

            TestNormalizeExpression("$\"\"\"{ 3:C}\"\"\"", "$\"\"\"{3:C}\"\"\"");
            TestNormalizeExpression("$\"\"\"{ 3: C}\"\"\"", "$\"\"\"{3: C}\"\"\"");
            TestNormalizeExpression("$\"\"\"{ 3:C }\"\"\"", "$\"\"\"{3:C }\"\"\"");
            TestNormalizeExpression("$\"\"\"{ 3: C }\"\"\"", "$\"\"\"{3: C }\"\"\"");
            TestNormalizeExpression("$\"\"\"{3 :C}\"\"\"", "$\"\"\"{3:C}\"\"\"");
            TestNormalizeExpression("$\"\"\"{3 : C}\"\"\"", "$\"\"\"{3: C}\"\"\"");
            TestNormalizeExpression("$\"\"\"{3 :C }\"\"\"", "$\"\"\"{3:C }\"\"\"");
            TestNormalizeExpression("$\"\"\"{3 : C }\"\"\"", "$\"\"\"{3: C }\"\"\"");

            TestNormalizeExpression("$\"\"\"{ 3 :C}\"\"\"", "$\"\"\"{3:C}\"\"\"");
            TestNormalizeExpression("$\"\"\"{ 3 : C}\"\"\"", "$\"\"\"{3: C}\"\"\"");
            TestNormalizeExpression("$\"\"\"{ 3 :C }\"\"\"", "$\"\"\"{3:C }\"\"\"");
            TestNormalizeExpression("$\"\"\"{ 3 : C }\"\"\"", "$\"\"\"{3: C }\"\"\"");
        }

        [Fact]
        [WorkItem(23618, "https://github.com/dotnet/roslyn/issues/23618")]
        public void TestSpacingOnMethodConstraint()
        {
            // newline between ) and where
            TestNormalizeDeclaration(
                "class C { void M<T>() where T : struct { } }",
                "class C\r\n{\r\n  void M<T>()\r\n    where T : struct\r\n  {\r\n  }\r\n}");
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

        [Fact]
        public void TestNormalizeRawInterpolatedString()
        {
            TestNormalizeExpression(@"$""""""Message is {a}""""""", @"$""""""Message is {a}""""""");
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
            Assert.Equal(text.NormalizeLineEndings(), node.ToFullString().NormalizeLineEndings());
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected.NormalizeLineEndings(), actual.NormalizeLineEndings());
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
            TestNormalizeStatement("{\r\n/// <goo/>\r\na}", "{\r\n  /// <goo/>\r\n  a\r\n}");
            TestNormalizeStatement("{\r\n///<goo/>\r\na}", "{\r\n  ///<goo/>\r\n  a\r\n}");
            TestNormalizeStatement("{\r\n/// <goo>\r\n/// </goo>\r\na}", "{\r\n  /// <goo>\r\n  /// </goo>\r\n  a\r\n}");
            TestNormalizeToken("/// <goo>\r\n/// </goo>\r\na", "/// <goo>\r\n/// </goo>\r\na");
            TestNormalizeStatement("{\r\n/*** <goo/> ***/\r\na}", "{\r\n  /*** <goo/> ***/\r\n  a\r\n}");
            TestNormalizeStatement("{\r\n/*** <goo/>\r\n ***/\r\na}", "{\r\n  /*** <goo/>\r\n ***/\r\n  a\r\n}");
        }

        private void TestNormalizeToken(string text, string expected)
        {
            var token = SyntaxFactory.ParseToken(text);
            var actual = token.NormalizeWhitespace().ToFullString();
            Assert.Equal(expected, actual);
        }

        [Fact]
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

            TestNormalizeTrivia("#endregion goo", "#endregion goo\r\n");

            TestNormalizeDeclaration(
@"#pragma warning disable 123

namespace goo {
}

#pragma warning restore 123",
@"#pragma warning disable 123
namespace goo
{
}
#pragma warning restore 123
");
        }

        [Fact]
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

        [Fact]
        public void TestNormalizeLineSpanDirectiveNode()
        {
            TestNormalize(
                SyntaxFactory.LineSpanDirectiveTrivia(
                    SyntaxFactory.Token(SyntaxKind.HashToken),
                    SyntaxFactory.Token(SyntaxKind.LineKeyword),
                    SyntaxFactory.LineDirectivePosition(SyntaxFactory.Literal(1), SyntaxFactory.Literal(2)),
                    SyntaxFactory.Token(SyntaxKind.MinusToken),
                    SyntaxFactory.LineDirectivePosition(SyntaxFactory.Literal(3), SyntaxFactory.Literal(4)),
                    SyntaxFactory.Literal(5),
                    SyntaxFactory.Literal("a.txt"),
                    SyntaxFactory.Token(SyntaxKind.EndOfDirectiveToken),
                    isActive: true),
                "#line (1, 2) - (3, 4) 5 \"a.txt\"\r\n");
        }

        [Fact]
        public void TestNormalizeLineSpanDirectiveTrivia()
        {
            TestNormalizeTrivia("  #  line( 1,2 )-(3,4)5\"a.txt\"", "#line (1, 2) - (3, 4) 5 \"a.txt\"\r\n");
        }

        [WorkItem(538115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538115")]
        [Fact]
        public void TestNormalizeWithinDirectives()
        {
            TestNormalizeDeclaration(
"class C\r\n{\r\n#if true\r\nvoid Goo(A x) { }\r\n#else\r\n#endif\r\n}\r\n",
"class C\r\n{\r\n#if true\r\n  void Goo(A x)\r\n  {\r\n  }\r\n#else\r\n#endif\r\n}");
        }

        [WorkItem(542887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542887")]
        [Fact]
        public void TestFormattingForBlockSyntax()
        {
            var code =
@"class c1
{
void goo()
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
  void goo()
  {
    {
      int i = 1;
    }
  }
}".NormalizeLineEndings());
        }

        [WorkItem(1079042, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079042")]
        [Fact]
        public void TestNormalizeDocumentationComments()
        {
            var code =
@"class c1
{
    ///<summary>
    /// A documentation comment
    ///</summary>
    void goo()
    {
    }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            TestNormalize(tree.GetCompilationUnitRoot(),
"class c1\r\n" +
"{\r\n"
+ // The normalizer doesn't change line endings in comments,
  // see https://github.com/dotnet/roslyn/issues/8536
$"  ///<summary>{Environment.NewLine}" +
$"  /// A documentation comment{Environment.NewLine}" +
$"  ///</summary>{Environment.NewLine}" +
"  void goo()\r\n" +
"  {\r\n" +
"  }\r\n" +
"}");
        }

        [Fact]
        public void TestNormalizeDocumentationComments2()
        {
            var code =
@"class c1
{
  ///  <summary>
  ///  A documentation comment
  ///  </summary>
  void goo()
  {
  }
}";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            TestNormalize(tree.GetCompilationUnitRoot(),
"class c1\r\n" +
"{\r\n" + // The normalizer doesn't change line endings in comments,
          // see https://github.com/dotnet/roslyn/issues/8536
$"  ///  <summary>{Environment.NewLine}" +
$"  ///  A documentation comment{Environment.NewLine}" +
$"  ///  </summary>{Environment.NewLine}" +
"  void goo()\r\n" +
"  {\r\n" +
"  }\r\n" +
"}");
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

        [Fact]
        [WorkItem(29390, "https://github.com/dotnet/roslyn/issues/29390")]
        public void TestNormalizeTuples()
        {
            TestNormalizeDeclaration("new(string prefix,string uri)[10]", "new (string prefix, string uri)[10]");
            TestNormalizeDeclaration("(string prefix,string uri)[]ns", "(string prefix, string uri)[] ns");
            TestNormalizeDeclaration("(string prefix,(string uri,string help))ns", "(string prefix, (string uri, string help)) ns");
            TestNormalizeDeclaration("(string prefix,string uri)ns", "(string prefix, string uri) ns");
            TestNormalizeDeclaration("public void Foo((string prefix,string uri)ns)", "public void Foo((string prefix, string uri) ns)");
            TestNormalizeDeclaration("public (string prefix,string uri)Foo()", "public (string prefix, string uri) Foo()");
        }

        [Fact]
        [WorkItem(50664, "https://github.com/dotnet/roslyn/issues/50664")]
        public void TestNormalizeFunctionPointer()
        {
            var content =
@"unsafe class C
{
  delegate * < int ,  int > functionPointer;
}";

            var expected =
@"unsafe class C
{
  delegate*<int, int> functionPointer;
}";

            TestNormalizeDeclaration(content, expected);
        }

        [Fact]
        [WorkItem(50664, "https://github.com/dotnet/roslyn/issues/50664")]
        public void TestNormalizeFunctionPointerWithManagedCallingConvention()
        {
            var content =
@"unsafe class C
{
  delegate *managed < int ,  int > functionPointer;
}";

            var expected =
@"unsafe class C
{
  delegate* managed<int, int> functionPointer;
}";

            TestNormalizeDeclaration(content, expected);
        }

        [Fact]
        [WorkItem(50664, "https://github.com/dotnet/roslyn/issues/50664")]
        public void TestNormalizeFunctionPointerWithUnmanagedCallingConvention()
        {
            var content =
@"unsafe class C
{
  delegate *unmanaged < int ,  int > functionPointer;
}";

            var expected =
@"unsafe class C
{
  delegate* unmanaged<int, int> functionPointer;
}";

            TestNormalizeDeclaration(content, expected);
        }

        [Fact]
        [WorkItem(50664, "https://github.com/dotnet/roslyn/issues/50664")]
        public void TestNormalizeFunctionPointerWithUnmanagedCallingConventionAndSpecifiers()
        {
            var content =
@"unsafe class C
{
  delegate *unmanaged [ Cdecl ,  Thiscall ] < int ,  int > functionPointer;
}";

            var expected =
@"unsafe class C
{
  delegate* unmanaged[Cdecl, Thiscall]<int, int> functionPointer;
}";

            TestNormalizeDeclaration(content, expected);
        }

        [Fact]
        [WorkItem(53254, "https://github.com/dotnet/roslyn/issues/53254")]
        public void TestNormalizeColonInConstructorInitializer()
        {
            var content =
@"class Base
{
}

class Derived : Base
{
  public Derived():base(){}
}";

            var expected =
@"class Base
{
}

class Derived : Base
{
  public Derived() : base()
  {
  }
}";

            TestNormalizeDeclaration(content, expected);
        }

        [Fact]
        [WorkItem(49732, "https://github.com/dotnet/roslyn/issues/49732")]
        public void TestNormalizeXmlInDocComment()
        {
            var code = @"/// <returns>
/// If this method succeeds, it returns <b xmlns:loc=""http://microsoft.com/wdcml/l10n"">S_OK</b>.
/// </returns>";
            TestNormalizeDeclaration(code, code);
        }

        [Theory]
        [InlineData("_=()=>{};", "_ = () =>\r\n{\r\n};")]
        [InlineData("_=x=>{};", "_ = x =>\r\n{\r\n};")]
        [InlineData("Add(()=>{});", "Add(() =>\r\n{\r\n});")]
        [InlineData("Add(delegate(){});", "Add(delegate ()\r\n{\r\n});")]
        [InlineData("Add(()=>{{_=x=>{};}});", "Add(() =>\r\n{\r\n  {\r\n    _ = x =>\r\n    {\r\n    };\r\n  }\r\n});")]
        [WorkItem(46656, "https://github.com/dotnet/roslyn/issues/46656")]
        public void TestNormalizeBlockAnonymousFunctions(string actual, string expected)
        {
            TestNormalizeStatement(actual, expected);
        }

        [Fact]
        public void TestNormalizeExtendedPropertyPattern()
        {
            var text = "_ = this is{Property . Property :2};";

            var expected = @"_ = this is { Property.Property: 2 };";
            TestNormalizeStatement(text, expected);
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
            var actual = trivia.NormalizeWhitespace("    ").ToFullString().NormalizeLineEndings();
            Assert.Equal(expected.NormalizeLineEndings(), actual);
        }
    }
}
