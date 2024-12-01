// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            TestNormalizeStatement("""
                {object x = 1;
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
                }
                """, """
                {
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
                }
                """
            );
        }

        [Fact, WorkItem(52543, "https://github.com/dotnet/roslyn/issues/52543")]
        public void TestNormalizeSwitchExpression()
        {
            TestNormalizeStatement(
                """var x = (int)1 switch { 1 => "one", 2 => "two", 3 => "three", {} => ">= 4" };""", """
                var x = (int)1 switch
                {
                  1 => "one",
                  2 => "two",
                  3 => "three",
                  { } => ">= 4"
                };
                """
            );
        }

        [Fact]
        public void TestNormalizeSwitchExpressionRawStrings()
        {
            TestNormalizeStatement(
                """"var x = (int)1 switch { 1 => """one""", 2 => """two""", 3 => """three""", {} => """>= 4""" };"""", """"
                var x = (int)1 switch
                {
                  1 => """one""",
                  2 => """two""",
                  3 => """three""",
                  { } => """>= 4"""
                };
                """"
            );
        }

        [Fact]
        public void TestNormalizeSwitchExpressionRawStringsUtf8_01()
        {
            TestNormalizeStatement(
                """"var x = (int)1 switch { 1 => """one"""u8, 2 => """two"""U8, 3 => """three"""u8, {} => """>= 4"""U8 };"""", """"
                var x = (int)1 switch
                {
                  1 => """one"""u8,
                  2 => """two"""U8,
                  3 => """three"""u8,
                  { } => """>= 4"""U8
                };
                """"
            );
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void TestNormalizeSwitchExpressionRawStringsMultiline()
        {
            TestNormalizeStatement(""""
                var x = (int)1 switch { 1 => """
                       one
                  """, 2 =>
                """
                   two
                """ };
                """", """"
                var x = (int)1 switch
                {
                  1 => """
                       one
                  """,
                  2 => """
                   two
                """
                };
                """"
            );
        }

        [ConditionalFact(typeof(WindowsOnly))]
        public void TestNormalizeSwitchExpressionRawStringsMultilineUtf8_01()
        {
            TestNormalizeStatement(""""
                var x = (int)1 switch { 1 => """
                       one
                  """U8, 2 =>
                """
                   two
                """u8 };
                """", """"
                var x = (int)1 switch
                {
                  1 => """
                       one
                  """U8,
                  2 => """
                   two
                """u8
                };
                """"
            );
        }

        [Fact]
        public void TestNormalizeSwitchExpressionStringsUtf8()
        {
            TestNormalizeStatement("""
                var x = (int)1 switch { 1 =>
                    "one"u8     , 2 =>
                  @"two"u8   , 3 =>
                 "three"U8  , {} =>
                @">= 4"U8 };
                """, """
                var x = (int)1 switch
                {
                  1 => "one"u8,
                  2 => @"two"u8,
                  3 => "three"U8,
                  { } => @">= 4"U8
                };
                """
            );
        }

        [Fact, WorkItem(52543, "https://github.com/dotnet/roslyn/issues/52543")]
        public void TestNormalizeSwitchRecPattern()
        {
            TestNormalizeStatement("""
                var x = (object)1 switch {
                		int { } => "two",
                		{ } t when t.GetHashCode() == 42 => "42",
                		System.ValueTuple<int, int> (1, _) { Item2: > 2 and < 20 } => "tuple.Item2 < 20",
                		System.ValueTuple<int, int> (1, _) { Item2: >= 100 } greater => greater.ToString(),
                		System.ValueType {} => "not null value",
                		object {} i when i is not 42 => "not 42",
                		{ } => "not null",
                		null => "null",
                };
                """, """
                var x = (object)1 switch
                {
                  int { } => "two",
                  { } t when t.GetHashCode() == 42 => "42",
                  System.ValueTuple<int, int> (1, _) { Item2: > 2 and < 20 } => "tuple.Item2 < 20",
                  System.ValueTuple<int, int> (1, _) { Item2: >= 100 } greater => greater.ToString(),
                  System.ValueType { } => "not null value",
                  object { } i when i is not 42 => "not 42",
                  { } => "not null",
                  null => "null",
                };
                """
            );
        }

        [Fact, WorkItem(52543, "https://github.com/dotnet/roslyn/issues/52543")]
        public void TestNormalizeSwitchExpressionComplex()
        {
            TestNormalizeStatement("""
                var x = vehicle switch
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
                                { } => -1, //throw new ArgumentException(message: "Not a known vehicle type", paramName: nameof(vehicle)),
                                null => 0//throw new ArgumentNullException(nameof(vehicle))
                            };
                """, """
                var x = vehicle switch
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
                  { } => -1, //throw new ArgumentException(message: "Not a known vehicle type", paramName: nameof(vehicle)),
                  null => 0 //throw new ArgumentNullException(nameof(vehicle))
                };
                """);
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
            TestNormalizeExpression("""
                $"Printed: {                    new Printer() { TextToPrint = "Hello world!" }.PrintedText }"
                """, """
                $"Printed: {new Printer() { TextToPrint = "Hello world!" }.PrintedText}"
                """
            );
        }

        [Fact]
        public void TestLineBreakRawInterpolations()
        {
            TestNormalizeExpression(""""
                $"""Printed: {                    new Printer() { TextToPrint = "Hello world!" }.PrintedText }"""
                """", """"
                $"""Printed: {new Printer() { TextToPrint = "Hello world!" }.PrintedText}"""
                """"
            );
        }

        [Fact, WorkItem(50742, "https://github.com/dotnet/roslyn/issues/50742")]
        public void TestVerbatimStringInterpolationWithLineBreaks()
        {
            TestNormalizeStatement("""
                Console.WriteLine($@"Test with line
                breaks
                {
                                new[]{
                     1, 2, 3
                  }[2]
                }
                            ");
                """, """
                Console.WriteLine($@"Test with line
                breaks
                {new[] { 1, 2, 3 }[2]}
                            ");
                """
            );
        }

        [Fact]
        public void TestRawStringInterpolationWithLineBreaks()
        {
            TestNormalizeStatement(""""
                Console.WriteLine($"""
                            Test with line
                            breaks
                            {
                                            new[]{
                                 1, 2, 3
                              }[2]
                            }
                            """);
                """", """"
                Console.WriteLine($"""
                            Test with line
                            breaks
                            {new[] { 1, 2, 3 }[2]}
                            """);
                """"
            );
        }

        [Fact]
        public void TestNormalizeDifferentExpressions()
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
            TestNormalizeExpression("a>>>b", "a >>> b");
            TestNormalizeExpression("a>>=b", "a >>= b");
            TestNormalizeExpression("a>>>=b", "a >>>= b");
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
            TestNormalizeExpression(
                "from a in b where c select d", """
                from a in b
                where c
                select d
                """);

            TestNormalizeExpression("a().b().c()", "a().b().c()");
            TestNormalizeExpression("a->b->c", "a->b->c");
            TestNormalizeExpression("global :: a", "global::a");

            TestNormalizeExpression("(IList<int>)args", "(IList<int>)args");
            TestNormalizeExpression("(IList<IList<int>>)args", "(IList<IList<int>>)args");
            TestNormalizeExpression("(IList<IList<IList<int>>>)args", "(IList<IList<IList<int>>>)args");

            TestNormalizeExpression("(IList<string?>)args", "(IList<string?>)args");
        }

        private static void TestNormalizeExpression(string text, string expected)
        {
            var node = SyntaxFactory.ParseExpression(text.NormalizeLineEndings());
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected.NormalizeLineEndings(), actual.NormalizeLineEndings());
        }

        [Fact]
        public void TestNormalizeExpressionStatement()
        {
            TestNormalizeStatement("a;", "a;");
        }

        [Fact]
        public void TestNormalizeBlockStatements()
        {
            TestNormalizeStatement(
                "{a;}", """
                {
                  a;
                }
                """);
            TestNormalizeStatement(
                "{a;b;}", """
                {
                  a;
                  b;
                }
                """);
            TestNormalizeStatement(
                "\t{a;}", """
                {
                  a;
                }
                """);
            TestNormalizeStatement(
                "\t{a;b;}", """
                {
                  a;
                  b;
                }
                """);
        }

        [Fact]
        public void TestNormalizeIfStatements()
        {
            TestNormalizeStatement(
                "if(a)b;", """
                if (a)
                  b;
                """);
            TestNormalizeStatement(
                "if(a){b;}", """
                if (a)
                {
                  b;
                }
                """);
            TestNormalizeStatement(
                "if(a){b;c;}", """
                if (a)
                {
                  b;
                  c;
                }
                """);
            TestNormalizeStatement(
                "if(a)b;else c;", """
                if (a)
                  b;
                else
                  c;
                """);
            TestNormalizeStatement(
                "if(a)b;else if(c)d;", """
                if (a)
                  b;
                else if (c)
                  d;
                """);
        }

        [Fact]
        public void TestNormalizeWhileStatements()
        {
            TestNormalizeStatement(
                "while(a)b;", """
                while (a)
                  b;
                """);
            TestNormalizeStatement(
                "while(a){b;}", """
                while (a)
                {
                  b;
                }
                """);
        }

        [Fact]
        public void TestNormalizeDoWhileStatement()
        {
            TestNormalizeStatement(
                "do{a;}while(b);", """
                do
                {
                  a;
                }
                while (b);
                """);
        }

        [Fact]
        public void TestNormalizeForStatements()
        {
            TestNormalizeStatement(
                "for(a;b;c)d;", """
                for (a; b; c)
                  d;
                """);
            TestNormalizeStatement(
                "for(;;)a;", """
                for (;;)
                  a;
                """);
        }

        [Fact]
        public void TestNormalizeForeachStatement()
        {
            TestNormalizeStatement(
                "foreach(a in b)c;", """
                foreach (a in b)
                  c;
                """);
        }

        [Fact]
        public void TestNormalizeTryStatements()
        {
            TestNormalizeStatement(
                "try{a;}catch(b){c;}", """
                try
                {
                  a;
                }
                catch (b)
                {
                  c;
                }
                """);
            TestNormalizeStatement(
                "try{a;}finally{b;}", """
                try
                {
                  a;
                }
                finally
                {
                  b;
                }
                """);
        }

        [Fact]
        public void TestNormalizeOtherStatements()
        {
            TestNormalizeStatement(
                "lock(a)b;", """
                lock (a)
                  b;
                """);
            TestNormalizeStatement(
                "fixed(a)b;", """
                fixed (a)
                  b;
                """);
            TestNormalizeStatement(
                "using(a)b;", """
                using (a)
                  b;
                """);
            TestNormalizeStatement(
                "checked{a;}", """
                checked
                {
                  a;
                }
                """);
            TestNormalizeStatement(
                "unchecked{a;}", """
                unchecked
                {
                  a;
                }
                """);
            TestNormalizeStatement(
                "unsafe{a;}", """
                unsafe
                {
                  a;
                }
                """);
        }

        [Fact]
        public void TestNormalizeDeclarationStatements()
        {
            TestNormalizeStatement("a b;", "a b;");
            TestNormalizeStatement("a?b;", "a? b;");
            TestNormalizeStatement("a b,c;", "a b, c;");
            TestNormalizeStatement("a b=c;", "a b = c;");
            TestNormalizeStatement("a b=c,d=e;", "a b = c, d = e;");
        }

        [Fact]
        public void TestNormalizeEmptyStatements()
        {
            TestNormalizeStatement(";", ";");
            TestNormalizeStatement(
                "{;;}", """
                {
                  ;
                  ;
                }
                """);
        }

        [Fact]
        public void TestNormalizeLabelStatements()
        {
            TestNormalizeStatement(
                "goo:;", """
                goo:
                  ;
                """);
            TestNormalizeStatement(
                "goo:a;", """
                goo:
                  a;
                """);
        }

        [Fact]
        public void TestNormalizeReturnAndGotoStatements()
        {
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
        }

        [Fact]
        public void TestNormalizeSwitchStatements()
        {
            TestNormalizeStatement(
                "switch(a){case b:c;}", """
                switch (a)
                {
                  case b:
                    c;
                }
                """);
            TestNormalizeStatement(
                "switch(a){case b:c;case d:e;}", """
                switch (a)
                {
                  case b:
                    c;
                  case d:
                    e;
                }
                """);
            TestNormalizeStatement(
                "switch(a){case b:c;default:d;}", """
                switch (a)
                {
                  case b:
                    c;
                  default:
                    d;
                }
                """);
            TestNormalizeStatement(
                "switch(a){case b:{}default:{}}", """
                switch (a)
                {
                  case b:
                  {
                  }

                  default:
                  {
                  }
                }
                """);
            TestNormalizeStatement(
                "switch(a){case b:c();d();default:e();f();}", """
                switch (a)
                {
                  case b:
                    c();
                    d();
                  default:
                    e();
                    f();
                }
                """);
            TestNormalizeStatement(
                "switch(a){case b:{c();}}", """
                switch (a)
                {
                  case b:
                  {
                    c();
                  }
                }
                """);
        }

        [Fact]
        public void TestNormalizeStatements_Curlies()
        {
            TestNormalizeStatement(
                "{if(goo){}if(bar){}}", """
                {
                  if (goo)
                  {
                  }

                  if (bar)
                  {
                  }
                }
                """);

        }

        [Fact]
        public void TestNormalizeStatements_Queries()
        {
            TestNormalizeStatement(
                "int i=from v in vals select v;", """
                int i =
                  from v in vals
                  select v;
                """);
            TestNormalizeStatement(
                "Goo(from v in vals select v);", """
                Goo(
                  from v in vals
                  select v);
                """);
            TestNormalizeStatement(
                "int i=from v in vals select from x in xxx where x > 10 select x;", """
                int i =
                  from v in vals
                  select
                    from x in xxx
                    where x > 10
                    select x;
                """);
            TestNormalizeStatement(
                "int i=from v in vals group v by x into g where g > 10 select g;", """
                int i =
                  from v in vals
                  group v by x into g
                    where g > 10
                    select g;
                """);
        }

        [Fact]
        public void TestNormalizeStatements_Generics()
        {
            TestNormalizeStatement("Func<string, int> f = blah;", "Func<string, int> f = blah;");
        }

        [Fact]
        public void TestLocalFunctionAttributes()
        {
            TestNormalizeStatement(
                "[ return:A ]void Local( [ B ]object o){}", """
                [return: A]
                void Local([B] object o)
                {
                }
                """);
            TestNormalizeStatement(
                "[A,B][C]T Local<T>()=>default;", """
                [A, B]
                [C]
                T Local<T>() => default;
                """);
        }

        [Fact, WorkItem(59653, "https://github.com/dotnet/roslyn/issues/59653")]
        public void TestLambdaAttributes()
        {
            TestNormalizeExpression("( [ A ]x)=>x", "([A] x) => x");
            TestNormalizeExpression("( [ A ]int x=1)=>x", "([A] int x = 1) => x");
            TestNormalizeExpression(
                "[return:A]([B]object o)=>{}", """
                [return: A]
                ([B] object o) =>
                {
                }
                """);
            TestNormalizeExpression(
                "[ A ,B ] [C]()=>x", """
                [A, B]
                [C]
                () => x
                """);
            TestNormalizeExpression(
                "[A]B()=>{ }", """
                [A]
                B () =>
                {
                }
                """);
        }

        [Fact, WorkItem(59653, "https://github.com/dotnet/roslyn/issues/59653")]
        public void TestLambdaReturnType()
        {
            TestNormalizeExpression("int( x )=>x", "int (x) => x");
            TestNormalizeExpression(
                "A( B b )=>{}", """
                A (B b) =>
                {
                }
                """);
            TestNormalizeExpression("""
                static
                async
                A<int>()=>x
                """,
                "static async A<int> () => x");
            TestNormalizeExpression("(A,B)()=>(new A(),new B())", "(A, B) () => (new A(), new B())");
            TestNormalizeExpression("A.B()=>null", "A.B () => null");
            TestNormalizeExpression("A.B.C()=>null", "A.B.C () => null");
            TestNormalizeExpression("int[]()=>null", "int[] () => null");
            TestNormalizeExpression("A.B[]()=>null", "A.B[] () => null");
            TestNormalizeExpression("A.B.C[]()=>null", "A.B.C[] () => null");
            TestNormalizeExpression("int*()=>null", "int* () => null");
            TestNormalizeExpression("A.B*()=>null", "A.B* () => null");
            TestNormalizeExpression("A.B.C*()=>null", "A.B.C* () => null");
        }

        [Fact]
        public void TestLambdaOptionalParameters()
        {
            TestNormalizeExpression("( int x=1 )=>x", "(int x = 1) => x");
            TestNormalizeExpression(
                "(int  x  =  1,int y,int z=2)=>{}", """
                (int x = 1, int y, int z = 2) =>
                {
                }
                """);
        }

        [Fact]
        public void TestLambdaParamsArray()
        {
            TestNormalizeExpression("( params  int []xs)=>xs.Length", "(params int[] xs) => xs.Length");
            TestNormalizeExpression(
                "(int  x  =  1,int y,int z=2,params int  []xs)=>{}", """
                (int x = 1, int y, int z = 2, params int[] xs) =>
                {
                }
                """);
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

        [Fact, WorkItem(49733, "https://github.com/dotnet/roslyn/issues/49733")]
        public void TestNormalizeAsteriskInPointerReturnTypeOfIndexer()
        {
            TestNormalizeDeclaration("""
                public unsafe class C
                {
                  int*this[int x,int y]{get=>(int*)0;}
                }
                """, """
                public unsafe class C
                {
                  int* this[int x, int y] { get => (int*)0; }
                }
                """);
        }

        [Fact]
        public void TestNormalizeAsteriskInVoidPointerCast()
        {
            TestNormalizeDeclaration("""
                public unsafe class C
                {
                  void*this[int x,int y]{get   =>  (  void  *   ) 0;}
                }
                """, """
                public unsafe class C
                {
                  void* this[int x, int y] { get => (void*)0; }
                }
                """);
        }

        private static void TestNormalizeStatement(string text, string expected)
        {
            var node = SyntaxFactory.ParseStatement(text.NormalizeLineEndings());
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected.NormalizeLineEndings(), actual.NormalizeLineEndings());
        }

        [Fact]
        public void TestNormalizeUsingDeclarations()
        {
            TestNormalizeDeclaration("using a;", "using a;");
            TestNormalizeDeclaration("using a=b;", "using a = b;");
            TestNormalizeDeclaration("using a.b;", "using a.b;");
            TestNormalizeDeclaration(
                "using A; using B; class C {}", """
                using A;
                using B;

                class C
                {
                }
                """);

            TestNormalizeDeclaration("global  using  a;", "global using a;");
            TestNormalizeDeclaration("global  using  a=b;", "global using a = b;");
            TestNormalizeDeclaration("global  using  a.b;", "global using a.b;");
            TestNormalizeDeclaration(
                "global using A; global using B; class C {}", """
                global using A;
                global using B;

                class C
                {
                }
                """);
            TestNormalizeDeclaration(
                "global using A; using B; class C {}", """
                global using A;
                using B;

                class C
                {
                }
                """);
            TestNormalizeDeclaration(
                "using A; global using B; class C {}", """
                using A;
                global using B;

                class C
                {
                }
                """);
        }

        [Fact]
        public void TestNormalizeNamespaceDeclarations()
        {
            TestNormalizeDeclaration(
                "namespace a{}", """
                namespace a
                {
                }
                """);
            TestNormalizeDeclaration(
                "namespace a{using b;}", """
                namespace a
                {
                  using b;
                }
                """);
            TestNormalizeDeclaration(
                "namespace a{global  using  b;}", """
                namespace a
                {
                  global using b;
                }
                """);
            TestNormalizeDeclaration(
                "namespace a{namespace b{}}", """
                namespace a
                {
                  namespace b
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "namespace a{}namespace b{}", """
                namespace a
                {
                }

                namespace b
                {
                }
                """);
        }

        [Fact]
        public void TestNormalizeTypeDeclarations()
        {
            TestNormalizeDeclaration(
                "class a{}", """
                class a
                {
                }
                """);
            TestNormalizeDeclaration(
                "class a{class b{}}", """
                class a
                {
                  class b
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a<b>where a:c{}", """
                class a<b>
                  where a : c
                {
                }
                """);
            TestNormalizeDeclaration(
                "class a<b,c>where a:c{}", """
                class a<b, c>
                  where a : c
                {
                }
                """);
            TestNormalizeDeclaration(
                "class a:b{}", """
                class a : b
                {
                }
                """);
        }

        [Fact]
        public void TestNormalizeMethodDeclarations()
        {
            TestNormalizeDeclaration(
                "class a{void b(){}}", """
                class a
                {
                  void b()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{void b(){}void c(){}}", """
                class a
                {
                  void b()
                  {
                  }

                  void c()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{a(){}}", """
                class a
                {
                  a()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{~a(){}}", """
                class a
                {
                  ~a()
                  {
                  }
                }
                """);
        }

        [Fact]
        public void TestNormalizeOperatorDeclarations()
        {
            TestNormalizeDeclaration(
                "class a{b operator    checked-(c d){}}", """
                class a
                {
                  b operator checked -(c d)
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{ implicit operator    checked    b(c d){}}", """
                class a
                {
                  implicit operator checked b(c d)
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{ explicit operator    checked    b(c d){}}", """
                class a
                {
                  explicit operator checked b(c d)
                  {
                  }
                }
                """);

            TestNormalizeDeclaration(
                "class a{b I1 . operator    checked-(c d){}}", """
                class a
                {
                  b I1.operator checked -(c d)
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{ implicit I1 . operator    checked    b(c d){}}", """
                class a
                {
                  implicit I1.operator checked b(c d)
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{ explicit I1 . operator    checked    b(c d){}}", """
                class a
                {
                  explicit I1.operator checked b(c d)
                  {
                  }
                }
                """);

            TestNormalizeDeclaration(
                "class a{b operator    >>>  ( c  d , e f ){}}", """
                class a
                {
                  b operator >>>(c d, e f)
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{b I1 . operator    >>>  ( c  d , e f ){}}", """
                class a
                {
                  b I1.operator >>>(c d, e f)
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{b operator>>>  ( c  d , e f ){}}", """
                class a
                {
                  b operator >>>(c d, e f)
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class a{b I1 . operator>>>  ( c  d , e f ){}}", """
                class a
                {
                  b I1.operator >>>(c d, e f)
                  {
                  }
                }
                """);
        }

        [Fact]
        public void TestNormalizePropertyDeclarations()
        {
            TestNormalizeDeclaration(
                "class a{b c{get;}}", """
                class a
                {
                  b c { get; }
                }
                """);
            TestNormalizeDeclaration("""
                class a {
                int X{get;set;}= 2;
                }

                """, """
                class a
                {
                  int X { get; set; } = 2;
                }
                """);
            TestNormalizeDeclaration("""
                class a {
                int Y
                {get;
                set;
                }
                =99;
                }

                """, """
                class a
                {
                  int Y { get; set; } = 99;
                }
                """);
            TestNormalizeDeclaration("""
                class a {
                int Z{get;}
                }

                """, """
                class a
                {
                  int Z { get; }
                }
                """);
            TestNormalizeDeclaration("""
                class a {
                int T{get;init;}
                int R{get=>1;}
                }

                """, """
                class a
                {
                  int T { get; init; }

                  int R { get => 1; }
                }
                """);
            TestNormalizeDeclaration("""
                class a {
                int Q{get{return 0;}init{}}
                int R{get=>1;}
                }

                """, """
                class a
                {
                  int Q
                  {
                    get
                    {
                      return 0;
                    }

                    init
                    {
                    }
                  }

                  int R { get => 1; }
                }
                """);
            TestNormalizeDeclaration("""
                class a {
                int R{get=>1;}
                }

                """, """
                class a
                {
                  int R { get => 1; }
                }
                """);
            TestNormalizeDeclaration("""
                class a {
                int S=>2;
                }

                """, """
                class a
                {
                  int S => 2;
                }
                """);
            TestNormalizeDeclaration("""
                class x
                {
                int _g;
                int G
                {
                get
                {
                return
                _g;
                }
                init;
                }
                int H
                {
                get;
                set
                {
                _g
                =
                12;
                }
                }
                }

                """, """
                class x
                {
                  int _g;
                  int G
                  {
                    get
                    {
                      return _g;
                    }

                    init;
                  }

                  int H
                  {
                    get;
                    set
                    {
                      _g = 12;
                    }
                  }
                }
                """);

            TestNormalizeDeclaration("""
                class i1
                {
                int
                p
                {
                get;
                }
                }
                """, """
                class i1
                {
                  int p { get; }
                }
                """);
            TestNormalizeDeclaration("""
                class i2
                {
                int
                p
                {
                get=>2;
                }
                }
                """, """
                class i2
                {
                  int p { get => 2; }
                }
                """);
            TestNormalizeDeclaration("""
                class i2a
                {
                int _p;
                int
                p
                {
                get=>
                _p;set
                =>_p
                =value
                ;
                }
                }
                """, """
                class i2a
                {
                  int _p;
                  int p { get => _p; set => _p = value; }
                }
                """);
            TestNormalizeDeclaration("""
                class i3
                {
                int
                p
                {
                get{}
                }
                }
                """, """
                class i3
                {
                  int p
                  {
                    get
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class i4
                {
                int
                p
                {
                set;
                }
                }
                """, """
                class i4
                {
                  int p { set; }
                }
                """);
            TestNormalizeDeclaration("""
                class i5
                {
                int
                p
                {
                set{}
                }
                }
                """, """
                class i5
                {
                  int p
                  {
                    set
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class i6
                {
                int
                p
                {
                init;
                }
                }
                """, """
                class i6
                {
                  int p { init; }
                }
                """);
            TestNormalizeDeclaration("""
                class i7
                {
                int
                p
                {
                init{}
                }
                }
                """, """
                class i7
                {
                  int p
                  {
                    init
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class i8
                {
                int
                p
                {
                get{}
                set{}
                }
                }
                """, """
                class i8
                {
                  int p
                  {
                    get
                    {
                    }

                    set
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class i9
                {
                int
                p
                {
                get=>1;
                set{z=1;}
                }
                }
                """, """
                class i9
                {
                  int p
                  {
                    get => 1;
                    set
                    {
                      z = 1;
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class ia
                {
                int
                p
                {
                get{}
                set;
                }
                }
                """, """
                class ia
                {
                  int p
                  {
                    get
                    {
                    }

                    set;
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class ib
                {
                int
                p
                {
                get;
                set{}
                }
                }
                """, """
                class ib
                {
                  int p
                  {
                    get;
                    set
                    {
                    }
                  }
                }
                """);
        }

        [Fact]
        public void TestNormalizePropertyDeclarations_WithInitializers()
        {
            TestNormalizeDeclaration("""
                class i4
                {
                int
                p
                {
                set;
                }=1;
                }
                """, """
                class i4
                {
                  int p { set; } = 1;
                }
                """);
            TestNormalizeDeclaration("""
                class i5
                {
                int
                p
                {
                set{}
                }=1;
                }
                """, """
                class i5
                {
                  int p
                  {
                    set
                    {
                    }
                  } = 1;
                }
                """);
            TestNormalizeDeclaration("""
                class i6
                {
                int
                p
                {
                init;
                }=1;
                }
                """, """
                class i6
                {
                  int p { init; } = 1;
                }
                """);
            TestNormalizeDeclaration("""
                class i7
                {
                int
                p
                {
                init{}
                }=1;
                }
                """, """
                class i7
                {
                  int p
                  {
                    init
                    {
                    }
                  } = 1;
                }
                """);
            TestNormalizeDeclaration("""
                class i8
                {
                int
                p
                {
                get{}
                set{}
                }=1;
                }
                """, """
                class i8
                {
                  int p
                  {
                    get
                    {
                    }

                    set
                    {
                    }
                  } = 1;
                }
                """);
            TestNormalizeDeclaration("""
                class i9
                {
                int
                p
                {
                get=>1;
                set{z=1;}
                }=1;
                }
                """, """
                class i9
                {
                  int p
                  {
                    get => 1;
                    set
                    {
                      z = 1;
                    }
                  } = 1;
                }
                """);
            TestNormalizeDeclaration("""
                class ia
                {
                int
                p
                {
                get{}
                set;
                }=1;
                }
                """, """
                class ia
                {
                  int p
                  {
                    get
                    {
                    }

                    set;
                  } = 1;
                }
                """);
            TestNormalizeDeclaration("""
                class ib
                {
                int
                p
                {
                get;
                set{}
                }=1;
                }
                """, """
                class ib
                {
                  int p
                  {
                    get;
                    set
                    {
                    }
                  } = 1;
                }
                """);
        }

        [Fact]
        public void TestNormalizePropertyDeclarations_LineBreaksBetweenPropertyAndOtherMembers()
        {
            TestNormalizeDeclaration(
                "class A{public string Prop{get;}public int f;}", """
                class A
                {
                  public string Prop { get; }

                  public int f;
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;}="xyz";public int f;}""", """
                class A
                {
                  public string Prop { get; } = "xyz";

                  public int f;
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;set;}public int f;}", """
                class A
                {
                  public string Prop { get; set; }

                  public int f;
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;set;}="xyz";public int f;}""", """
                class A
                {
                  public string Prop { get; set; } = "xyz";

                  public int f;
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get{}}public int f;}""", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  }

                  public int f;
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get{}}="xyz";public int f;}""", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  } = "xyz";

                  public int f;
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get=>string.Empty;set=>_=value;}public int f;}", """
                class A
                {
                  public string Prop { get => string.Empty; set => _ = value; }

                  public int f;
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop=>"xyz";public int f;}""", """
                class A
                {
                  public string Prop => "xyz";

                  public int f;
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;}public int Prop2{get;set;}}", """
                class A
                {
                  public string Prop { get; }
                  public int Prop2 { get; set; }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;}public int Prop2{get{}}}", """
                class A
                {
                  public string Prop { get; }

                  public int Prop2
                  {
                    get
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;}="xyz";public int Prop2{get;set;}}""", """
                class A
                {
                  public string Prop { get; } = "xyz";
                  public int Prop2 { get; set; }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;}="xyz";public int Prop2{get{}}}""", """
                class A
                {
                  public string Prop { get; } = "xyz";

                  public int Prop2
                  {
                    get
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;set;}public int Prop2{get;set;}}", """
                class A
                {
                  public string Prop { get; set; }
                  public int Prop2 { get; set; }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;set;}public int Prop2{get{}}}", """
                class A
                {
                  public string Prop { get; set; }

                  public int Prop2
                  {
                    get
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;set;}="xyz";public int Prop2{get;set;}}""", """
                class A
                {
                  public string Prop { get; set; } = "xyz";
                  public int Prop2 { get; set; }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;set;}="xyz";public int Prop2{get{}}}""", """
                class A
                {
                  public string Prop { get; set; } = "xyz";

                  public int Prop2
                  {
                    get
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get{}}public int Prop2{get;set;}}", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  }

                  public int Prop2 { get; set; }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get{}}public int Prop2{get{}}}", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  }

                  public int Prop2
                  {
                    get
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get{}}="xyz";public int Prop2{get;set;}}""", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  } = "xyz";

                  public int Prop2 { get; set; }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get{}}="xyz";public int Prop2{get{}}}""", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  } = "xyz";

                  public int Prop2
                  {
                    get
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get=>string.Empty;set=>_=value;}public int Prop2{get;set;}}", """
                class A
                {
                  public string Prop { get => string.Empty; set => _ = value; }
                  public int Prop2 { get; set; }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop=>"xyz";public int Prop2{get;set;}}""", """
                class A
                {
                  public string Prop => "xyz";
                  public int Prop2 { get; set; }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;}public A(){}}", """
                class A
                {
                  public string Prop { get; }

                  public A()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;}="xyz";public A(){}}""", """
                class A
                {
                  public string Prop { get; } = "xyz";

                  public A()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;set;}public A(){}}", """
                class A
                {
                  public string Prop { get; set; }

                  public A()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;set;}="xyz";public A(){}}""", """
                class A
                {
                  public string Prop { get; set; } = "xyz";

                  public A()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get{}}public A(){}}", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  }

                  public A()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get{}}="xyz";public A(){}}""", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  } = "xyz";

                  public A()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get=>string.Empty;set=>_=value;}public A(){}}", """
                class A
                {
                  public string Prop { get => string.Empty; set => _ = value; }

                  public A()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop=>"xyz";public A(){}}""", """
                class A
                {
                  public string Prop => "xyz";

                  public A()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;}public void M(){}}", """
                class A
                {
                  public string Prop { get; }

                  public void M()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;}="xyz";public void M(){}}""", """
                class A
                {
                  public string Prop { get; } = "xyz";

                  public void M()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;set;}public void M(){}}", """
                class A
                {
                  public string Prop { get; set; }

                  public void M()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;set;}="xyz";public void M(){}}""", """
                class A
                {
                  public string Prop { get; set; } = "xyz";

                  public void M()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get{}}public void M(){}}", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  }

                  public void M()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get{}}="xyz";public void M(){}}""", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  } = "xyz";

                  public void M()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get=>string.Empty;set=>_=value;}public void M(){}}", """
                class A
                {
                  public string Prop { get => string.Empty; set => _ = value; }

                  public void M()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop=>"xyz";public void M(){}}""", """
                class A
                {
                  public string Prop => "xyz";

                  public void M()
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;}public event EventHandler E;}", """
                class A
                {
                  public string Prop { get; }

                  public event EventHandler E;
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;}="xyz";public event EventHandler E;}""", """
                class A
                {
                  public string Prop { get; } = "xyz";

                  public event EventHandler E;
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;set;}public event EventHandler E;}", """
                class A
                {
                  public string Prop { get; set; }

                  public event EventHandler E;
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;set;}="xyz";public event EventHandler E;}""", """
                class A
                {
                  public string Prop { get; set; } = "xyz";

                  public event EventHandler E;
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get{}}public event EventHandler E;}", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  }

                  public event EventHandler E;
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get{}}="xyz";public event EventHandler E;}""", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  } = "xyz";

                  public event EventHandler E;
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get=>string.Empty;set=>_=value;}public event EventHandler E;}", """
                class A
                {
                  public string Prop { get => string.Empty; set => _ = value; }

                  public event EventHandler E;
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop=>"xyz";public event EventHandler E;}""", """
                class A
                {
                  public string Prop => "xyz";

                  public event EventHandler E;
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;}public class Nested{}}", """
                class A
                {
                  public string Prop { get; }

                  public class Nested
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;}="xyz";public class Nested{}}""", """
                class A
                {
                  public string Prop { get; } = "xyz";

                  public class Nested
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;set;}public class Nested{}}", """
                class A
                {
                  public string Prop { get; set; }

                  public class Nested
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;set;}="xyz";public class Nested{}}""", """
                class A
                {
                  public string Prop { get; set; } = "xyz";

                  public class Nested
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get{}}public class Nested{}}", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  }

                  public class Nested
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get{}}="xyz";public class Nested{}}""", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  } = "xyz";

                  public class Nested
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get=>string.Empty;set=>_=value;}public class Nested{}}", """
                class A
                {
                  public string Prop { get => string.Empty; set => _ = value; }

                  public class Nested
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop=>"xyz";public class Nested{}}""", """
                class A
                {
                  public string Prop => "xyz";

                  public class Nested
                  {
                  }
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;}public delegate int D();}", """
                class A
                {
                  public string Prop { get; }

                  public delegate int D();
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;}="xyz";public delegate int D();}""", """
                class A
                {
                  public string Prop { get; } = "xyz";

                  public delegate int D();
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get;set;}public delegate int D();}", """
                class A
                {
                  public string Prop { get; set; }

                  public delegate int D();
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get;set;}="xyz";public delegate int D();}""", """
                class A
                {
                  public string Prop { get; set; } = "xyz";

                  public delegate int D();
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get{}}public delegate int D();}", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  }

                  public delegate int D();
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop{get{}}="xyz";public delegate int D();}""", """
                class A
                {
                  public string Prop
                  {
                    get
                    {
                    }
                  } = "xyz";

                  public delegate int D();
                }
                """);
            TestNormalizeDeclaration(
                "class A{public string Prop{get=>string.Empty;set=>_=value;}public delegate int D();}", """
                class A
                {
                  public string Prop { get => string.Empty; set => _ = value; }

                  public delegate int D();
                }
                """);
            TestNormalizeDeclaration(
                """class A{public string Prop=>"xyz";public delegate int D();}""", """
                class A
                {
                  public string Prop => "xyz";

                  public delegate int D();
                }
                """);
        }

        [Fact]
        public void TestNormalizeIndexerDeclarations()
        {
            TestNormalizeDeclaration(
                "class a{b this[c d]{get;}}", """
                class a
                {
                  b this[c d] { get; }
                }
                """);
            TestNormalizeDeclaration("""
                class i1
                {
                int
                this[b c]
                {
                get;
                }
                }
                """, """
                class i1
                {
                  int this[b c] { get; }
                }
                """);
            TestNormalizeDeclaration("""
                class i2
                {
                int
                this[b c]
                {
                get=>1;
                }
                }
                """, """
                class i2
                {
                  int this[b c] { get => 1; }
                }
                """);
            TestNormalizeDeclaration("""
                class i3
                {
                int
                this[b c]
                {
                get{}
                }
                }
                """, """
                class i3
                {
                  int this[b c]
                  {
                    get
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class i4
                {
                int
                this[b c]
                {
                set;
                }
                }
                """, """
                class i4
                {
                  int this[b c] { set; }
                }
                """);
            TestNormalizeDeclaration("""
                class i5
                {
                int
                this[b c]
                {
                set{}
                }
                }
                """, """
                class i5
                {
                  int this[b c]
                  {
                    set
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class i6
                {
                int
                this[b c]
                {
                init;
                }
                }
                """, """
                class i6
                {
                  int this[b c] { init; }
                }
                """);
            TestNormalizeDeclaration("""
                class i7
                {
                int
                this[b c]
                {
                init{}
                }
                }
                """, """
                class i7
                {
                  int this[b c]
                  {
                    init
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class i8
                {
                int
                this[b c]
                {
                get{}
                set{}
                }
                }
                """, """
                class i8
                {
                  int this[b c]
                  {
                    get
                    {
                    }

                    set
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class i9
                {
                int
                this[b c]
                {
                get=>1;
                set{z=1;}
                }
                }
                """, """
                class i9
                {
                  int this[b c]
                  {
                    get => 1;
                    set
                    {
                      z = 1;
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class ia
                {
                int
                this[b c]
                {
                get{}
                set;
                }
                }
                """, """
                class ia
                {
                  int this[b c]
                  {
                    get
                    {
                    }

                    set;
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class ib
                {
                int
                this[b c]
                {
                get;
                set{}
                }
                }
                """, """
                class ib
                {
                  int this[b c]
                  {
                    get;
                    set
                    {
                    }
                  }
                }
                """);
        }

        [Fact]
        public void TestNormalizeEventDeclarations()
        {
            TestNormalizeDeclaration("""
                class a
                {
                public
                event
                w
                e;
                }
                """, """
                class a
                {
                  public event w e;
                }
                """);
            TestNormalizeDeclaration("""
                abstract class b
                {
                event
                w
                e
                ;
                }
                """, """
                abstract class b
                {
                  event w e;
                }
                """);
            TestNormalizeDeclaration("""
                interface c1
                {
                event
                w
                e
                ;
                }
                """, """
                interface c1
                {
                  event w e;
                }
                """);
            TestNormalizeDeclaration("""
                interface c2 : c1
                {
                abstract
                event
                w
                c1
                .
                e
                ;
                }
                """, """
                interface c2 : c1
                {
                  abstract event w c1.e;
                }
                """);
            TestNormalizeDeclaration("""
                class d
                {
                event w x;
                event
                w
                e
                {
                add
                =>
                x+=
                value;
                remove
                =>x
                -=
                value;
                }}
                """, """
                class d
                {
                  event w x;
                  event w e { add => x += value; remove => x -= value; }
                }
                """);
            TestNormalizeDeclaration("""
                class e
                {
                event w e
                {
                add{}
                remove{
                }
                }
                }
                """, """
                class e
                {
                  event w e
                  {
                    add
                    {
                    }

                    remove
                    {
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class f
                {
                event w x;
                event w e
                {
                add
                {
                x
                +=
                value;
                }
                remove
                {
                x
                -=
                value;
                }
                }
                }
                """, """
                class f
                {
                  event w x;
                  event w e
                  {
                    add
                    {
                      x += value;
                    }

                    remove
                    {
                      x -= value;
                    }
                  }
                }
                """);
            TestNormalizeDeclaration("""
                class g
                {
                extern
                event
                w
                e
                =
                null
                ;
                }
                """, """
                class g
                {
                  extern event w e = null;
                }
                """);
            TestNormalizeDeclaration("""
                class h
                {
                public event w e
                {
                add
                =>
                c
                (
                );
                remove
                =>
                d(
                );
                }
                }
                """, """
                class h
                {
                  public event w e { add => c(); remove => d(); }
                }
                """);
            TestNormalizeDeclaration("""
                class i
                {
                event w e
                {
                add;
                remove;
                }
                }
                """, """
                class i
                {
                  event w e { add; remove; }
                }
                """);
        }

        [Fact]
        public void TestNormalizeFieldDeclarations()
        {
            TestNormalizeDeclaration(
                "class a{b c;}", """
                class a
                {
                  b c;
                }
                """);
            TestNormalizeDeclaration(
                "class a{b c;d e;}", """
                class a
                {
                  b c;
                  d e;
                }
                """);
            TestNormalizeDeclaration(
                "class a{b c=d;}", """
                class a
                {
                  b c = d;
                }
                """);
            TestNormalizeDeclaration(
                "class a{b c=d,e=f;}", """
                class a
                {
                  b c = d, e = f;
                }
                """);
            TestNormalizeDeclaration(
                "class a{b c=d;e f=g;}", """
                class a
                {
                  b c = d;
                  e f = g;
                }
                """);
        }

        [Fact]
        public void TestNormalizeDelegateDeclarations()
        {
            TestNormalizeDeclaration("delegate a b();", "delegate a b();");
            TestNormalizeDeclaration("delegate a b(c);", "delegate a b(c);");
            TestNormalizeDeclaration("delegate a b(c,d);", "delegate a b(c, d);");
        }

        [Fact]
        public void TestNormalizeEnumDeclarations()
        {
            TestNormalizeDeclaration(
                "enum a{}", """
                enum a
                {
                }
                """);
            TestNormalizeDeclaration(
                "enum a{b}", """
                enum a
                {
                  b
                }
                """);
            TestNormalizeDeclaration(
                "enum a{b,c}", """
                enum a
                {
                  b,
                  c
                }
                """);
            TestNormalizeDeclaration(
                "enum a{b=c}", """
                enum a
                {
                  b = c
                }
                """);
        }

        [Fact]
        public void TestNormalizeDeclarations_Attributes()
        {
            // declaration attributes
            TestNormalizeDeclaration(
                "[a]class b{}", """
                [a]
                class b
                {
                }
                """);
            TestNormalizeDeclaration(
                "\t[a]class b{}", """
                [a]
                class b
                {
                }
                """);
            TestNormalizeDeclaration(
                "[a,b]class c{}", """
                [a, b]
                class c
                {
                }
                """);
            TestNormalizeDeclaration(
                "[a(b)]class c{}", """
                [a(b)]
                class c
                {
                }
                """);
            TestNormalizeDeclaration(
                "[a(b,c)]class d{}", """
                [a(b, c)]
                class d
                {
                }
                """);
            TestNormalizeDeclaration(
                "[a][b]class c{}", """
                [a]
                [b]
                class c
                {
                }
                """);
            TestNormalizeDeclaration(
                "[a:b]class c{}", """
                [a: b]
                class c
                {
                }
                """);

            // parameter attributes
            TestNormalizeDeclaration(
                "class c{void M([a]int x,[b] [c,d]int y){}}", """
                class c
                {
                  void M([a] int x, [b][c, d] int y)
                  {
                  }
                }
                """);
        }

        [Fact]
        public void TestFileScopedNamespace()
        {
            TestNormalizeDeclaration(
                "namespace NS;class C{}", """
                namespace NS;
                class C
                {
                }
                """);
        }

        [Fact]
        public void TestSpacingOnRecord()
        {
            TestNormalizeDeclaration("record  class  C(int I, int J);", "record class C(int I, int J);");
            TestNormalizeDeclaration("record  struct  S(int I, int J);", "record struct S(int I, int J);");
        }

        [Fact]
        public void TestSpacingOnPrimaryConstructor()
        {
            TestNormalizeDeclaration("class  C     (   int    I   ,    int    J   )   ;    ", "class C(int I, int J);");
            TestNormalizeDeclaration("struct  S     (   int    I   ,    int    J   )   ;    ", "struct S(int I, int J);");
            TestNormalizeDeclaration("interface  S     (   int    I   ,    int    J   )   ;    ", "interface S(int I, int J);");
            TestNormalizeDeclaration("class  C     (   )   ;    ", "class C();");
            TestNormalizeDeclaration("struct   S  (  )  ;    ", "struct S();");
            TestNormalizeDeclaration("interface   S  (  )  ;    ", "interface S();");
        }

        [Fact]
        public void TestSemicolonBody()
        {
            TestNormalizeDeclaration("class      C       ;    ", "class C;");
            TestNormalizeDeclaration("struct      C       ;    ", "struct C;");
            TestNormalizeDeclaration("interface      C       ;    ", "interface C;");
            TestNormalizeDeclaration("enum      C       ;    ", "enum C;");
        }

        [Fact]
        public void RefReadonlyParameters()
        {
            TestNormalizeDeclaration("""
                class   C  {  int  this  [  ref  readonly   int  x ,  ref  readonly  int  y ]  {  get  ;  } 
                void  M ( ref  readonly  int  x ,  ref  readonly  int  y ) ; }
                """, """
                class C
                {
                  int this[ref readonly int x, ref readonly int y] { get; }

                  void M(ref readonly int x, ref readonly int y);
                }
                """);
        }

        [Fact, WorkItem(23618, "https://github.com/dotnet/roslyn/issues/23618")]
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
                "class C<T> where T : new() { }", """
                class C<T>
                  where T : new()
                {
                }
                """);

            // no space between this and (
            TestNormalizeDeclaration(
                "class C { C() : this () { } }", """
                class C
                {
                  C() : this()
                  {
                  }
                }
                """);

            // no space between base and (
            TestNormalizeDeclaration(
                "class C { C() : base () { } }", """
                class C
                {
                  C() : base()
                  {
                  }
                }
                """);

            // no space between checked and (
            TestNormalizeExpression("checked (a)", "checked(a)");

            // no space between unchecked and (
            TestNormalizeExpression("unchecked (a)", "unchecked(a)");

            // no space between __arglist and (
            TestNormalizeExpression("__arglist (a)", "__arglist(a)");
        }

        [Fact, WorkItem(24454, "https://github.com/dotnet/roslyn/issues/24454")]
        public void TestSpacingOnInterpolatedString()
        {
            TestNormalizeExpression("$\"{3:C}\"", "$\"{3:C}\"");
            TestNormalizeExpression("$\"{3: C}\"", "$\"{3: C}\"");
        }

        [Fact]
        public void TestSpacingOnRawInterpolatedString()
        {
            TestNormalizeExpression(""""
                $"""{3:C}"""
                """", """"
                $"""{3:C}"""
                """");
            TestNormalizeExpression(""""
                $"""{3: C}"""
                """", """"
                $"""{3: C}"""
                """");
            TestNormalizeExpression(""""
                $"""{3:C }"""
                """", """"
                $"""{3:C }"""
                """");
            TestNormalizeExpression(""""
                $"""{3: C }"""
                """", """"
                $"""{3: C }"""
                """");

            TestNormalizeExpression(""""
                $"""{ 3:C}"""
                """", """"
                $"""{3:C}"""
                """");
            TestNormalizeExpression(""""
                $"""{ 3: C}"""
                """", """"
                $"""{3: C}"""
                """");
            TestNormalizeExpression(""""
                $"""{ 3:C }"""
                """", """"
                $"""{3:C }"""
                """");
            TestNormalizeExpression(""""
                $"""{ 3: C }"""
                """", """"
                $"""{3: C }"""
                """");
            TestNormalizeExpression(""""
                $"""{3 :C}"""
                """", """"
                $"""{3:C}"""
                """");
            TestNormalizeExpression(""""
                $"""{3 : C}"""
                """", """"
                $"""{3: C}"""
                """");
            TestNormalizeExpression(""""
                $"""{3 :C }"""
                """", """"
                $"""{3:C }"""
                """");
            TestNormalizeExpression(""""
                $"""{3 : C }"""
                """", """"
                $"""{3: C }"""
                """");

            TestNormalizeExpression(""""
                $"""{ 3 :C}"""
                """", """"
                $"""{3:C}"""
                """");
            TestNormalizeExpression(""""
                $"""{ 3 : C}"""
                """", """"
                $"""{3: C}"""
                """");
            TestNormalizeExpression(""""
                $"""{ 3 :C }"""
                """", """"
                $"""{3:C }"""
                """");
            TestNormalizeExpression(""""
                $"""{ 3 : C }"""
                """", """"
                $"""{3: C }"""
                """");
        }

        [Fact, WorkItem(23618, "https://github.com/dotnet/roslyn/issues/23618")]
        public void TestSpacingOnMethodConstraint()
        {
            // newline between ) and where
            TestNormalizeDeclaration(
                "class C { void M<T>() where T : struct { } }", """
                class C
                {
                  void M<T>()
                    where T : struct
                  {
                  }
                }
                """);
        }

        [Fact, WorkItem(541684, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541684")]
        public void TestNormalizeRegion1()
        {
            // NOTE: the space after the region name is retained, since the text after the space
            // following "#region" is a single, unstructured trivia element.
            TestNormalizeDeclaration("""

                class Class 
                { 
                #region Methods 
                void Method() 
                { 
                } 
                #endregion 
                }
                """, """
                class Class
                {
                #region Methods 
                  void Method()
                  {
                  }
                #endregion
                }
                """);
            TestNormalizeDeclaration("""

                #region
                #endregion
                """, """
                #region
                #endregion

                """);
            TestNormalizeDeclaration("""

                #region  
                #endregion
                """, """
                #region
                #endregion

                """);
            TestNormalizeDeclaration("""

                #region name //comment
                #endregion
                """, """
                #region name //comment
                #endregion

                """);
            TestNormalizeDeclaration("""

                #region /*comment*/
                #endregion
                """, """
                #region /*comment*/
                #endregion

                """);
        }

        [Fact, WorkItem(2076, "github")]
        public void TestNormalizeInterpolatedString()
        {
            TestNormalizeExpression(@"$""Message is {a}""", @"$""Message is {a}""");
        }

        [Fact]
        public void TestNormalizeRawInterpolatedString()
        {
            TestNormalizeExpression(""""
                $"""Message is {a}"""
                """", """"
                $"""Message is {a}"""
                """");
        }

        [Fact, WorkItem(528584, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/528584")]
        public void TestNormalizeRegion2()
        {
            TestNormalizeDeclaration("""

                #region //comment
                #endregion
                """,
                // NOTE: the extra newline should be removed, but it's not worth the
                // effort (see DevDiv #8564)
                """
                #region //comment

                #endregion

                """);
            TestNormalizeDeclaration("""

                #region //comment

                #endregion
                """,
                // NOTE: the extra newline should be removed, but it's not worth the
                // effort (see DevDiv #8564).
                """
                #region //comment

                #endregion

                """);
        }

        private static void TestNormalizeDeclaration(string text, string expected)
        {
            var node = SyntaxFactory.ParseCompilationUnit(text.NormalizeLineEndings());
            Assert.Equal(text.NormalizeLineEndings(), node.ToFullString().NormalizeLineEndings());
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            AssertEx.Equal(expected.NormalizeLineEndings(), actual.NormalizeLineEndings());
        }

        [Fact]
        public void TestNormalizeComments()
        {
            TestNormalizeToken(
                "a//b", """
                a //b

                """);
            TestNormalizeToken("a/*b*/", "a /*b*/");
            TestNormalizeToken("""
                //a
                b
                """, """
                //a
                b
                """);
            TestNormalizeExpression("a/*b*/+c", "a /*b*/ + c");
            TestNormalizeExpression(
                "/*a*/b", """
                /*a*/
                b
                """);
            TestNormalizeExpression("""
                /*a
                */b
                """, """
                /*a
                */
                b
                """);
            TestNormalizeStatement(
                "{/*a*/b}", """
                { /*a*/
                  b
                }
                """);
            TestNormalizeStatement("""
                {
                a//b
                }
                """, """
                {
                  a //b
                }
                """);
            TestNormalizeStatement("""
                {
                //a
                }
                """, """
                {
                //a
                }
                """);
            TestNormalizeStatement("""
                {
                //a
                b}
                """, """
                {
                  //a
                  b
                }
                """);
            TestNormalizeStatement("""
                {
                /*a*/b}
                """, """
                {
                  /*a*/
                  b
                }
                """);
            TestNormalizeStatement("""
                {
                /// <goo/>
                a}
                """, """
                {
                  /// <goo/>
                  a
                }
                """);
            TestNormalizeStatement("""
                {
                ///<goo/>
                a}
                """, """
                {
                  ///<goo/>
                  a
                }
                """);
            TestNormalizeStatement("""
                {
                /// <goo>
                /// </goo>
                a}
                """, """
                {
                  /// <goo>
                  /// </goo>
                  a
                }
                """);
            TestNormalizeToken("""
                /// <goo>
                /// </goo>
                a
                """, """
                /// <goo>
                /// </goo>
                a
                """);
            TestNormalizeStatement("""
                {
                /*** <goo/> ***/
                a}
                """, """
                {
                  /*** <goo/> ***/
                  a
                }
                """);
            TestNormalizeStatement("""
                {
                /*** <goo/>
                 ***/
                a}
                """, """
                {
                  /*** <goo/>
                 ***/
                  a
                }
                """);
        }

        private static void TestNormalizeToken(string text, string expected)
        {
            var token = SyntaxFactory.ParseToken(text.NormalizeLineEndings());
            var actual = token.NormalizeWhitespace().ToFullString();
            Assert.Equal(expected.NormalizeLineEndings(), actual.NormalizeLineEndings());
        }

        [Fact, WorkItem(1066, "github")]
        public void TestNormalizePreprocessorDirectives()
        {
            // directive as node
            TestNormalize(
                SyntaxFactory.DefineDirectiveTrivia(
                    SyntaxFactory.Identifier("a"), false), """
                #define a

                """);

            // directive as trivia
            TestNormalizeTrivia(
                "  #  define a", """
                #define a

                """);
            TestNormalizeTrivia(
                "#if(a||b)", """
                #if (a || b)

                """);
            TestNormalizeTrivia(
                "#if(a&&b)", """
                #if (a && b)

                """);
            TestNormalizeTrivia("""
                  #if a
                  #endif
                """, """
                #if a
                #endif

                """);

            TestNormalize(
                SyntaxFactory.TriviaList(
                    SyntaxFactory.Trivia(
                        SyntaxFactory.IfDirectiveTrivia(
                            SyntaxFactory.IdentifierName("a"), false, false, false)),
                    SyntaxFactory.Trivia(
                        SyntaxFactory.EndIfDirectiveTrivia(false))), """
                #if a
                #endif

                """);

            TestNormalizeTrivia(
                "#endregion goo", """
                #endregion goo

                """);

            TestNormalizeDeclaration("""
                #pragma warning disable 123

                namespace goo {
                }

                #pragma warning restore 123
                """, """
                #pragma warning disable 123
                namespace goo
                {
                }
                #pragma warning restore 123

                """);
        }

        [Fact, WorkItem(531607, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531607")]
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
                                        .WithTokens([SyntaxFactory.Literal(@"""a\b""")]))),
                                SyntaxKind.EndOfDirectiveToken,
                                default(SyntaxTriviaList))))), """
                #line 1 "\"a\\b\""

                """);
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
                    isActive: true), """
                #line (1, 2) - (3, 4) 5 "a.txt"

                """);
        }

        [Fact]
        public void TestNormalizeLineSpanDirectiveTrivia()
        {
            TestNormalizeTrivia(
                "  #  line( 1,2 )-(3,4)5\"a.txt\"", """
                #line (1, 2) - (3, 4) 5 "a.txt"

                """);
        }

        [Fact, WorkItem(538115, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538115")]
        public void TestNormalizeWithinDirectives()
        {
            TestNormalizeDeclaration("""
                class C
                {
                #if true
                void Goo(A x) { }
                #else
                #endif
                }

                """, """
                class C
                {
                #if true
                  void Goo(A x)
                  {
                  }
                #else
                #endif
                }
                """);
        }

        [Fact, WorkItem(542887, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542887")]
        public void TestFormattingForBlockSyntax()
        {
            var code = """
                class c1
                {
                void goo()
                {
                {
                int i = 1;
                }
                }
                }
                """;
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            TestNormalize(tree.GetCompilationUnitRoot(), """
                class c1
                {
                  void goo()
                  {
                    {
                      int i = 1;
                    }
                  }
                }
                """.NormalizeLineEndings());
        }

        [Fact, WorkItem(1079042, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1079042")]
        public void TestNormalizeDocumentationComments()
        {
            var code = """
                class c1
                {
                    ///<summary>
                    /// A documentation comment
                    ///</summary>
                    void goo()
                    {
                    }
                }
                """;
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
            var code = """
                class c1
                {
                  ///  <summary>
                  ///  A documentation comment
                  ///  </summary>
                  void goo()
                  {
                  }
                }
                """;
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
            var expected = """
                class c
                {
                	void m()
                	{
                	}
                }
                """;
            var actual = SyntaxFactory.ParseCompilationUnit(code).NormalizeWhitespace(indentation: "	").ToFullString();
            Assert.Equal(expected.NormalizeLineEndings(), actual);
        }

        [Fact, WorkItem(29390, "https://github.com/dotnet/roslyn/issues/29390")]
        public void TestNormalizeTuples()
        {
            TestNormalizeDeclaration("new(string prefix,string uri)[10]", "new (string prefix, string uri)[10]");
            TestNormalizeDeclaration("(string prefix,string uri)[]ns", "(string prefix, string uri)[] ns");
            TestNormalizeDeclaration("(string prefix,(string uri,string help))ns", "(string prefix, (string uri, string help)) ns");
            TestNormalizeDeclaration("(string prefix,string uri)ns", "(string prefix, string uri) ns");
            TestNormalizeDeclaration("public void Foo((string prefix,string uri)ns)", "public void Foo((string prefix, string uri) ns)");
            TestNormalizeDeclaration("public (string prefix,string uri)Foo()", "public (string prefix, string uri) Foo()");
        }

        [Fact, WorkItem(50664, "https://github.com/dotnet/roslyn/issues/50664")]
        public void TestNormalizeFunctionPointer()
        {
            TestNormalizeDeclaration("""
                unsafe class C
                {
                  delegate * < int ,  int > functionPointer;
                }
                """, """
                unsafe class C
                {
                  delegate*<int, int> functionPointer;
                }
                """);
        }

        [Fact, WorkItem(50664, "https://github.com/dotnet/roslyn/issues/50664")]
        public void TestNormalizeFunctionPointerWithManagedCallingConvention()
        {
            TestNormalizeDeclaration("""
                unsafe class C
                {
                  delegate *managed < int ,  int > functionPointer;
                }
                """, """
                unsafe class C
                {
                  delegate* managed<int, int> functionPointer;
                }
                """);
        }

        [Fact, WorkItem(50664, "https://github.com/dotnet/roslyn/issues/50664")]
        public void TestNormalizeFunctionPointerWithUnmanagedCallingConvention()
        {
            TestNormalizeDeclaration("""
                unsafe class C
                {
                  delegate *unmanaged < int ,  int > functionPointer;
                }
                """, """
                unsafe class C
                {
                  delegate* unmanaged<int, int> functionPointer;
                }
                """);
        }

        [Fact, WorkItem(50664, "https://github.com/dotnet/roslyn/issues/50664")]
        public void TestNormalizeFunctionPointerWithUnmanagedCallingConventionAndSpecifiers()
        {
            TestNormalizeDeclaration("""
                unsafe class C
                {
                  delegate *unmanaged [ Cdecl ,  Thiscall ] < int ,  int > functionPointer;
                }
                """, """
                unsafe class C
                {
                  delegate* unmanaged[Cdecl, Thiscall]<int, int> functionPointer;
                }
                """);
        }

        [Fact, WorkItem(53254, "https://github.com/dotnet/roslyn/issues/53254")]
        public void TestNormalizeColonInConstructorInitializer()
        {
            TestNormalizeDeclaration("""
                class Base
                {
                }

                class Derived : Base
                {
                  public Derived():base(){}
                }
                """, """
                class Base
                {
                }

                class Derived : Base
                {
                  public Derived() : base()
                  {
                  }
                }
                """);
        }

        [Fact, WorkItem(49732, "https://github.com/dotnet/roslyn/issues/49732")]
        public void TestNormalizeXmlInDocComment()
        {
            var code = """
                /// <returns>
                /// If this method succeeds, it returns <b xmlns:loc="http://microsoft.com/wdcml/l10n">S_OK</b>.
                /// </returns>
                """;
            TestNormalizeDeclaration(code, code);
        }

        [Fact, WorkItem(46656, "https://github.com/dotnet/roslyn/issues/46656")]
        public void TestNormalizeBlockAnonymousFunctions()
        {
            TestNormalizeStatement(
                "_=()=>{};", """
                _ = () =>
                {
                };
                """);
            TestNormalizeStatement(
                "_=x=>{};", """
                _ = x =>
                {
                };
                """);
            TestNormalizeStatement(
                "Add(()=>{});", """
                Add(() =>
                {
                });
                """);
            TestNormalizeStatement(
                "Add(delegate(){});", """
                Add(delegate ()
                {
                });
                """);
            TestNormalizeStatement(
                "Add(()=>{{_=x=>{};}});", """
                Add(() =>
                {
                  {
                    _ = x =>
                    {
                    };
                  }
                });
                """);
        }

        [Fact]
        public void TestNormalizeExtendedPropertyPattern()
        {
            TestNormalizeStatement(
                "_ = this is{Property . Property :2};",
                "_ = this is { Property.Property: 2 };");
        }

        private static void TestNormalize(CSharpSyntaxNode node, string expected)
        {
            var actual = node.NormalizeWhitespace("  ").ToFullString();
            Assert.Equal(expected.NormalizeLineEndings(), actual.NormalizeLineEndings());
        }

        private static void TestNormalizeTrivia(string text, string expected)
        {
            var list = SyntaxFactory.ParseLeadingTrivia(text.NormalizeLineEndings());
            TestNormalize(list, expected.NormalizeLineEndings());
        }

        private static void TestNormalize(SyntaxTriviaList trivia, string expected)
        {
            var actual = trivia.NormalizeWhitespace("    ").ToFullString();
            Assert.Equal(expected.NormalizeLineEndings(), actual.NormalizeLineEndings());
        }

        [Fact, WorkItem(60884, "https://github.com/dotnet/roslyn/issues/60884")]
        public void TestNormalizeXmlArgumentsInDocComment1()
        {
            TestNormalizeDeclaration(
                """/// Prefix <b    a="x"  b="y" >S_OK</b> suffix""",
                """/// Prefix <b a="x" b="y">S_OK</b> suffix""");
        }

        [Fact, WorkItem(60884, "https://github.com/dotnet/roslyn/issues/60884")]
        public void TestNormalizeXmlArgumentsInDocComment2()
        {
            var code = """/// Prefix <b a="x" b="y">S_OK</b> suffix""";
            TestNormalizeDeclaration(code, code);
        }

        [Fact, WorkItem(60884, "https://github.com/dotnet/roslyn/issues/60884")]
        public void TestNormalizeXmlArgumentsInDocComment3()
        {
            TestNormalizeDeclaration(
                """/// Prefix <b a="x" b="y" /> suffix""",
                """/// Prefix <b a="x" b="y"/> suffix""");
        }

        [Fact, WorkItem(60884, "https://github.com/dotnet/roslyn/issues/60884")]
        public void TestNormalizeXmlArgumentsInDocComment4()
        {
            TestNormalizeDeclaration(
                """/// Prefix <b    a="x"	>S_OK</b> suffix""",
                """/// Prefix <b a="x">S_OK</b> suffix""");
        }

        [Fact, WorkItem(60884, "https://github.com/dotnet/roslyn/issues/60884")]
        public void TestNormalizeXmlArgumentsInDocComment5()
        {
            var code = """/// Prefix <b a="x" b="y"/> suffix""";
            TestNormalizeDeclaration(code, code);
        }

        [Fact, WorkItem(60884, "https://github.com/dotnet/roslyn/issues/60884")]
        public void TestNormalizeXmlArgumentsInDocComment6()
        {
            TestNormalizeDeclaration(
                """/// Prefix <b a="x"b="y"/> suffix""",
                """/// Prefix <b a="x" b="y"/> suffix""");
        }

        [Fact, WorkItem(60884, "https://github.com/dotnet/roslyn/issues/60884")]
        public void TestNormalizeXmlArgumentsInDocComment7()
        {
            TestNormalizeDeclaration(
                """/// Prefix <b    b="y"a="x"	>S_OK</b> suffix""",
                """/// Prefix <b b="y" a="x">S_OK</b> suffix""");
        }

        [Fact]
        public void TestRequiredKeywordNormalization()
        {
            TestNormalizeDeclaration(
                "public  required  partial int Field;",
                "public required partial int Field;");
        }

        [Fact, WorkItem(61518, "https://github.com/dotnet/roslyn/issues/61518")]
        public void TestNormalizeNestedUsingStatements1()
        {
            TestNormalizeStatement(
                "using(a)using(b)c;", """
                using (a)
                using (b)
                  c;
                """);
            TestNormalizeStatement(
                "using(a)using(b){c;}", """
                using (a)
                using (b)
                {
                  c;
                }
                """);
            TestNormalizeStatement(
                "using(a)using(b)using(c)d;", """
                using (a)
                using (b)
                using (c)
                  d;
                """);
            TestNormalizeStatement(
                "using(a)using(b)using(c){d;}", """
                using (a)
                using (b)
                using (c)
                {
                  d;
                }
                """);

            TestNormalizeStatement(
                "using(a){using(b)c;}", """
                using (a)
                {
                  using (b)
                    c;
                }
                """);
            TestNormalizeStatement(
                "using(a){using(b)using(c)d;}", """
                using (a)
                {
                  using (b)
                  using (c)
                    d;
                }
                """);
            TestNormalizeStatement(
                "using(a)using(b){using(c)d;}", """
                using (a)
                using (b)
                {
                  using (c)
                    d;
                }
                """);
            TestNormalizeStatement(
                "using(a){using(b){using(c)d;}}", """
                using (a)
                {
                  using (b)
                  {
                    using (c)
                      d;
                  }
                }
                """);
        }

        [Fact, WorkItem(61518, "https://github.com/dotnet/roslyn/issues/61518")]
        public void TestNormalizeNestedFixedStatements1()
        {
            TestNormalizeStatement(
                "fixed(int* a = null)fixed(int* b = null)c;", """
                fixed (int* a = null)
                fixed (int* b = null)
                  c;
                """);
            TestNormalizeStatement(
                "fixed(int* a = null)fixed(int* b = null){c;}", """
                fixed (int* a = null)
                fixed (int* b = null)
                {
                  c;
                }
                """);
            TestNormalizeStatement(
                "fixed(int* a = null)fixed(int* b = null)fixed(int* c = null)d;", """
                fixed (int* a = null)
                fixed (int* b = null)
                fixed (int* c = null)
                  d;
                """);
            TestNormalizeStatement(
                "fixed(int* a = null)fixed(int* b = null)fixed(int* c = null){d;}", """
                fixed (int* a = null)
                fixed (int* b = null)
                fixed (int* c = null)
                {
                  d;
                }
                """);

            TestNormalizeStatement(
                "fixed(int* a = null){fixed(int* b = null)c;}", """
                fixed (int* a = null)
                {
                  fixed (int* b = null)
                    c;
                }
                """);
            TestNormalizeStatement(
                "fixed(int* a = null){fixed(int* b = null)fixed(int* c = null)d;}", """
                fixed (int* a = null)
                {
                  fixed (int* b = null)
                  fixed (int* c = null)
                    d;
                }
                """);
            TestNormalizeStatement(
                "fixed(int* a = null)fixed(int* b = null){fixed(int* c = null)d;}", """
                fixed (int* a = null)
                fixed (int* b = null)
                {
                  fixed (int* c = null)
                    d;
                }
                """);
            TestNormalizeStatement(
                "fixed(int* a = null){fixed(int* b = null){fixed(int* c = null)d;}}", """
                fixed (int* a = null)
                {
                  fixed (int* b = null)
                  {
                    fixed (int* c = null)
                      d;
                  }
                }
                """);
        }

        [Fact, WorkItem(61518, "https://github.com/dotnet/roslyn/issues/61518")]
        public void TestNormalizeNestedFixedUsingStatements1()
        {
            TestNormalizeStatement(
                "using(a)fixed(int* b = null)c;", """
                using (a)
                  fixed (int* b = null)
                    c;
                """);
            TestNormalizeStatement(
                "fixed(int* b = null)using(a)c;", """
                fixed (int* b = null)
                  using (a)
                    c;
                """);
        }

        [Fact]
        public void TestNormalizeScopedParameters()
        {
            TestNormalizeStatement(
                "static  void  F  (  scoped  R  x  ,  scoped  ref  R  y  ,  ref  scoped  R  z  )  {  }", """
                static void F(scoped R x, scoped ref R y, ref scoped R z)
                {
                }
                """);
        }

        [Fact]
        public void TestNormalizeScopedLocals()
        {
            TestNormalizeStatement("scoped  R  x  ;", "scoped R x;");
            TestNormalizeStatement("scoped  ref  R  y  ;", "scoped ref R y;");
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeObjectInitializer()
        {
            TestNormalizeExpression(
                "new{}", """
                new
                {
                }
                """);
            TestNormalizeExpression(
                "new{A=1,B=2}", """
                new
                {
                  A = 1,
                  B = 2
                }
                """);
            TestNormalizeExpression(
                "new{A=1,B=2,}", """
                new
                {
                  A = 1,
                  B = 2,
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{}", """
                new SomeClass
                {
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{A=1,B=2}", """
                new SomeClass
                {
                  A = 1,
                  B = 2
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{A=1,B=2,}", """
                new SomeClass
                {
                  A = 1,
                  B = 2,
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){}", """
                new SomeClass()
                {
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new{}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new{D=5l,E=2.5f}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new
                  {
                    D = 5l,
                    E = 2.5f
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new{D=5l,E=2.5f,}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new
                  {
                    D = 5l,
                    E = 2.5f,
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new{D=5l,E=2.5f,},}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new
                  {
                    D = 5l,
                    E = 2.5f,
                  },
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass{}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass{D=5l,E=2.5f}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass
                  {
                    D = 5l,
                    E = 2.5f
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass{D=5l,E=2.5f,}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass
                  {
                    D = 5l,
                    E = 2.5f,
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass{D=5l,E=2.5f,},}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass
                  {
                    D = 5l,
                    E = 2.5f,
                  },
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,},}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                  },
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{}}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new
                    {
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{G=7u,H=3.72m}}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new
                    {
                      G = 7u,
                      H = 3.72m
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{G=7u,H=3.72m,}}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new
                    {
                      G = 7u,
                      H = 3.72m,
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{G=7u,H=3.72m,},}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new
                    {
                      G = 7u,
                      H = 3.72m,
                    },
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{G=7u,H=3.72m,},},}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new
                    {
                      G = 7u,
                      H = 3.72m,
                    },
                  },
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{}}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass
                    {
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{G=7u,H=3.72m}}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass
                    {
                      G = 7u,
                      H = 3.72m
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{G=7u,H=3.72m,}}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass
                    {
                      G = 7u,
                      H = 3.72m,
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{G=7u,H=3.72m,},}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass
                    {
                      G = 7u,
                      H = 3.72m,
                    },
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{G=7u,H=3.72m,},},}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass
                    {
                      G = 7u,
                      H = 3.72m,
                    },
                  },
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){}}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass()
                    {
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){G=7u,H=3.72m}}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass()
                    {
                      G = 7u,
                      H = 3.72m
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){G=7u,H=3.72m,}}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass()
                    {
                      G = 7u,
                      H = 3.72m,
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){G=7u,H=3.72m,},}}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass()
                    {
                      G = 7u,
                      H = 3.72m,
                    },
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){G=7u,H=3.72m,},},}", """
                new SomeClass()
                {
                  A = 1,
                  B = 2,
                  C = new SomeOtherClass()
                  {
                    D = 5l,
                    E = 2.5f,
                    F = new AndAnotherClass()
                    {
                      G = 7u,
                      H = 3.72m,
                    },
                  },
                }
                """);
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeObjectInitializer_SingleLineContext()
        {
            VerifySingleLineInitializer(
                "new{}",
                "new { }");
            VerifySingleLineInitializer(
                "new{A=1,B=2}",
                "new { A = 1, B = 2 }");
            VerifySingleLineInitializer(
                "new{A=1,B=2,}",
                "new { A = 1, B = 2, }");
            VerifySingleLineInitializer(
                "new SomeClass{}",
                "new SomeClass { }");
            VerifySingleLineInitializer(
                "new SomeClass{A=1,B=2}",
                "new SomeClass { A = 1, B = 2 }");
            VerifySingleLineInitializer(
                "new SomeClass{A=1,B=2,}",
                "new SomeClass { A = 1, B = 2, }");
            VerifySingleLineInitializer(
                "new SomeClass{A=1,B=2,}",
                "new SomeClass { A = 1, B = 2, }");
            VerifySingleLineInitializer(
                "new SomeClass(){}",
                "new SomeClass() { }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2}",
                "new SomeClass() { A = 1, B = 2 }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,}",
                "new SomeClass() { A = 1, B = 2, }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new{}}",
                "new SomeClass() { A = 1, B = 2, C = new { } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new{D=5l,E=2.5f}}",
                "new SomeClass() { A = 1, B = 2, C = new { D = 5l, E = 2.5f } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new{D=5l,E=2.5f,}}",
                "new SomeClass() { A = 1, B = 2, C = new { D = 5l, E = 2.5f, } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new{D=5l,E=2.5f,},}",
                "new SomeClass() { A = 1, B = 2, C = new { D = 5l, E = 2.5f, }, }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass{}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass { } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass{D=5l,E=2.5f}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass { D = 5l, E = 2.5f } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass{D=5l,E=2.5f,}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass { D = 5l, E = 2.5f, } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass{D=5l,E=2.5f,},}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass { D = 5l, E = 2.5f, }, }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,},}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, }, }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{}}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new { } } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{G=7u,H=3.72m}}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new { G = 7u, H = 3.72m } } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{G=7u,H=3.72m,}}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new { G = 7u, H = 3.72m, } } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{G=7u,H=3.72m,},}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new { G = 7u, H = 3.72m, }, } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new{G=7u,H=3.72m,},},}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new { G = 7u, H = 3.72m, }, }, }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{}}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass { } } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{G=7u,H=3.72m}}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass { G = 7u, H = 3.72m } } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{G=7u,H=3.72m,}}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass { G = 7u, H = 3.72m, } } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{G=7u,H=3.72m,},}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass { G = 7u, H = 3.72m, }, } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass{G=7u,H=3.72m,},},}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass { G = 7u, H = 3.72m, }, }, }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){}}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass() { } } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){G=7u,H=3.72m}}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass() { G = 7u, H = 3.72m } } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){G=7u,H=3.72m,}}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass() { G = 7u, H = 3.72m, } } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){G=7u,H=3.72m,},}}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass() { G = 7u, H = 3.72m, }, } }");
            VerifySingleLineInitializer(
                "new SomeClass(){A=1,B=2,C=new SomeOtherClass(){D=5l,E=2.5f,F=new AndAnotherClass(){G=7u,H=3.72m,},},}",
                "new SomeClass() { A = 1, B = 2, C = new SomeOtherClass() { D = 5l, E = 2.5f, F = new AndAnotherClass() { G = 7u, H = 3.72m, }, }, }");
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeArrayAndCollectionInitializers()
        {
            TestNormalizeExpression(
                "new int[]{}", """
                new int[]
                {
                }
                """);
            TestNormalizeExpression(
                "new int[]{1,2,3}", """
                new int[]
                {
                  1,
                  2,
                  3
                }
                """);
            TestNormalizeExpression(
                "new int[]{1,2,3,}", """
                new int[]
                {
                  1,
                  2,
                  3,
                }
                """);
            TestNormalizeExpression(
                "new int[]{1,2,3,}.Length", """
                new int[]
                {
                  1,
                  2,
                  3,
                }.Length
                """);
            TestNormalizeExpression(
                "new int[]{1,2,3,}[0]", """
                new int[]
                {
                  1,
                  2,
                  3,
                }[0]
                """);

            TestNormalizeExpression(
                "new List<int>(){}", """
                new List<int>()
                {
                }
                """);
            TestNormalizeExpression(
                "new List<int>(){1,2,3}", """
                new List<int>()
                {
                  1,
                  2,
                  3
                }
                """);
            TestNormalizeExpression(
                "new List<int>(){1,2,3,}", """
                new List<int>()
                {
                  1,
                  2,
                  3,
                }
                """);
            TestNormalizeExpression(
                "new List<int>(){1,2,3,}.Count", """
                new List<int>()
                {
                  1,
                  2,
                  3,
                }.Count
                """);
            TestNormalizeExpression(
                "new List<int>(){1,2,3,}[0]", """
                new List<int>()
                {
                  1,
                  2,
                  3,
                }[0]
                """);

            TestNormalizeExpression(
                "new string[]{\"test1\",\"test2\",\"test3\"}", """
                new string[]
                {
                  "test1",
                  "test2",
                  "test3"
                }
                """);
            TestNormalizeExpression(
                "new string[]{\"test1\",\"test2\",\"test3\",}", """
                new string[]
                {
                  "test1",
                  "test2",
                  "test3",
                }
                """);
            TestNormalizeExpression(
                "new string[]{\"test1\",\"test2\",\"test3\",}.Length", """
                new string[]
                {
                  "test1",
                  "test2",
                  "test3",
                }.Length
                """);
            TestNormalizeExpression(
                "new string[]{\"test1\",\"test2\",\"test3\",}[0]", """
                new string[]
                {
                  "test1",
                  "test2",
                  "test3",
                }[0]
                """);

            TestNormalizeExpression(
                "new List<string>(){\"test1\",\"test2\",\"test3\"}", """
                new List<string>()
                {
                  "test1",
                  "test2",
                  "test3"
                }
                """);
            TestNormalizeExpression(
                "new List<string>(){\"test1\",\"test2\",\"test3\",}", """
                new List<string>()
                {
                  "test1",
                  "test2",
                  "test3",
                }
                """);
            TestNormalizeExpression(
                "new List<string>(){\"test1\",\"test2\",\"test3\",}.Count", """
                new List<string>()
                {
                  "test1",
                  "test2",
                  "test3",
                }.Count
                """);
            TestNormalizeExpression(
                "new List<string>(){\"test1\",\"test2\",\"test3\",}[0]", """
                new List<string>()
                {
                  "test1",
                  "test2",
                  "test3",
                }[0]
                """);

            TestNormalizeExpression(
                "new SomeClass[]{new SomeClass(),new SomeClass(),new SomeClass()}", """
                new SomeClass[]
                {
                  new SomeClass(),
                  new SomeClass(),
                  new SomeClass()
                }
                """);
            TestNormalizeExpression(
                "new SomeClass[]{new SomeClass(),new SomeClass(),new SomeClass(),}", """
                new SomeClass[]
                {
                  new SomeClass(),
                  new SomeClass(),
                  new SomeClass(),
                }
                """);
            TestNormalizeExpression(
                "new SomeClass[]{new SomeClass(),new SomeClass(),new SomeClass(),}.Length", """
                new SomeClass[]
                {
                  new SomeClass(),
                  new SomeClass(),
                  new SomeClass(),
                }.Length
                """);
            TestNormalizeExpression(
                "new SomeClass[]{new SomeClass(),new SomeClass(),new SomeClass(),}[0]", """
                new SomeClass[]
                {
                  new SomeClass(),
                  new SomeClass(),
                  new SomeClass(),
                }[0]
                """);

            TestNormalizeExpression(
                "new List<SomeClass>(){new SomeClass(),new SomeClass(),new SomeClass()}", """
                new List<SomeClass>()
                {
                  new SomeClass(),
                  new SomeClass(),
                  new SomeClass()
                }
                """);
            TestNormalizeExpression(
                "new List<SomeClass>(){new SomeClass(),new SomeClass(),new SomeClass(),}", """
                new List<SomeClass>()
                {
                  new SomeClass(),
                  new SomeClass(),
                  new SomeClass(),
                }
                """);
            TestNormalizeExpression(
                "new List<SomeClass>(){new SomeClass(),new SomeClass(),new SomeClass(),}.Count", """
                new List<SomeClass>()
                {
                  new SomeClass(),
                  new SomeClass(),
                  new SomeClass(),
                }.Count
                """);
            TestNormalizeExpression(
                "new List<SomeClass>(){new SomeClass(),new SomeClass(),new SomeClass(),}[0]", """
                new List<SomeClass>()
                {
                  new SomeClass(),
                  new SomeClass(),
                  new SomeClass(),
                }[0]
                """);

            TestNormalizeExpression(
                "new int[]{2+2,2+2*2,arr2[0]}", """
                new int[]
                {
                  2 + 2,
                  2 + 2 * 2,
                  arr2[0]
                }
                """);
            TestNormalizeExpression(
                "new List<int>(){2+2,2+2*2,arr2[0]}", """
                new List<int>()
                {
                  2 + 2,
                  2 + 2 * 2,
                  arr2[0]
                }
                """);
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeArrayAndCollectionInitializers_SingleLineContext()
        {
            VerifySingleLineInitializer(
                "new int[]{}",
                "new int[] { }");
            VerifySingleLineInitializer(
                "new int[]{1,2,3}",
                "new int[] { 1, 2, 3 }");
            VerifySingleLineInitializer(
                "new int[]{1,2,3,}",
                "new int[] { 1, 2, 3, }");
            VerifySingleLineInitializer(
                "new int[]{1,2,3,}.Length",
                "new int[] { 1, 2, 3, }.Length");
            VerifySingleLineInitializer(
                "new int[]{1,2,3,}[0]",
                "new int[] { 1, 2, 3, }[0]");

            VerifySingleLineInitializer(
                "new List<int>(){}",
                "new List<int>() { }");
            VerifySingleLineInitializer(
                "new List<int>(){1,2,3}",
                "new List<int>() { 1, 2, 3 }");
            VerifySingleLineInitializer(
                "new List<int>(){1,2,3,}",
                "new List<int>() { 1, 2, 3, }");
            VerifySingleLineInitializer(
                "new List<int>(){1,2,3,}.Count",
                "new List<int>() { 1, 2, 3, }.Count");
            VerifySingleLineInitializer(
                "new List<int>(){1,2,3,}[0]",
                "new List<int>() { 1, 2, 3, }[0]");

            VerifySingleLineInitializer(
                "new SomeClass[]{}",
                "new SomeClass[] { }");
            VerifySingleLineInitializer(
                "new SomeClass[]{new SomeClass(),new SomeClass(),new SomeClass()}",
                "new SomeClass[] { new SomeClass(), new SomeClass(), new SomeClass() }");
            VerifySingleLineInitializer(
                "new SomeClass[]{new SomeClass(),new SomeClass(),new SomeClass(),}",
                "new SomeClass[] { new SomeClass(), new SomeClass(), new SomeClass(), }");
            VerifySingleLineInitializer(
                "new SomeClass[]{new SomeClass(),new SomeClass(),new SomeClass(),}.Length",
                "new SomeClass[] { new SomeClass(), new SomeClass(), new SomeClass(), }.Length");
            VerifySingleLineInitializer(
                "new SomeClass[]{new SomeClass(),new SomeClass(),new SomeClass(),}[0]",
                "new SomeClass[] { new SomeClass(), new SomeClass(), new SomeClass(), }[0]");

            VerifySingleLineInitializer(
                "new List<SomeClass>(){}",
                "new List<SomeClass>() { }");
            VerifySingleLineInitializer(
                "new List<SomeClass>(){new SomeClass(),new SomeClass(),new SomeClass()}",
                "new List<SomeClass>() { new SomeClass(), new SomeClass(), new SomeClass() }");
            VerifySingleLineInitializer(
                "new List<SomeClass>(){new SomeClass(),new SomeClass(),new SomeClass(),}",
                "new List<SomeClass>() { new SomeClass(), new SomeClass(), new SomeClass(), }");
            VerifySingleLineInitializer(
                "new List<SomeClass>(){new SomeClass(),new SomeClass(),new SomeClass(),}.Length",
                "new List<SomeClass>() { new SomeClass(), new SomeClass(), new SomeClass(), }.Length");
            VerifySingleLineInitializer(
                "new List<SomeClass>(){new SomeClass(),new SomeClass(),new SomeClass(),}[0]",
                "new List<SomeClass>() { new SomeClass(), new SomeClass(), new SomeClass(), }[0]");

            VerifySingleLineInitializer(
                "new int[]{2+2,2+2*2,arr2[0]}",
                "new int[] { 2 + 2, 2 + 2 * 2, arr2[0] }");
            VerifySingleLineInitializer(
                "new List<int>(){2+2,2+2*2,arr2[0]}",
                "new List<int>() { 2 + 2, 2 + 2 * 2, arr2[0] }");
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeIndexerInitializer()
        {
            TestNormalizeExpression(
                "new Dictionary<int,int>(){}", """
                new Dictionary<int, int>()
                {
                }
                """);
            TestNormalizeExpression(
                "new Dictionary<int,int>(){[0]=1,[1]=2,[2]=3}", """
                new Dictionary<int, int>()
                {
                  [0] = 1,
                  [1] = 2,
                  [2] = 3
                }
                """);
            TestNormalizeExpression(
                "new Dictionary<int,int>(){[0]=1,[1]=2,[2]=3,}", """
                new Dictionary<int, int>()
                {
                  [0] = 1,
                  [1] = 2,
                  [2] = 3,
                }
                """);
            TestNormalizeExpression(
                "new Dictionary<int,int>(){[0]=1,[1]=2,[2]=3,}.Count", """
                new Dictionary<int, int>()
                {
                  [0] = 1,
                  [1] = 2,
                  [2] = 3,
                }.Count
                """);
            TestNormalizeExpression(
                "new Dictionary<int,int>(){[0]=1,[1]=2,[2]=3,}[0]", """
                new Dictionary<int, int>()
                {
                  [0] = 1,
                  [1] = 2,
                  [2] = 3,
                }[0]
                """);

            TestNormalizeExpression(
                "new Dictionary<string,string>(){[\"test0\"]=\"test1\",[\"test1\"]=\"test2\",[\"test2\"]=\"test3\"}", """
                new Dictionary<string, string>()
                {
                  ["test0"] = "test1",
                  ["test1"] = "test2",
                  ["test2"] = "test3"
                }
                """);
            TestNormalizeExpression(
                "new Dictionary<string,string>(){[\"test0\"]=\"test1\",[\"test1\"]=\"test2\",[\"test2\"]=\"test3\",}", """
                new Dictionary<string, string>()
                {
                  ["test0"] = "test1",
                  ["test1"] = "test2",
                  ["test2"] = "test3",
                }
                """);
            TestNormalizeExpression(
                "new Dictionary<string,string>(){[\"test0\"]=\"test1\",[\"test1\"]=\"test2\",[\"test2\"]=\"test3\",}.Count", """
                new Dictionary<string, string>()
                {
                  ["test0"] = "test1",
                  ["test1"] = "test2",
                  ["test2"] = "test3",
                }.Count
                """);
            TestNormalizeExpression(
                "new Dictionary<string,string>(){[\"test0\"]=\"test1\",[\"test1\"]=\"test2\",[\"test2\"]=\"test3\",}[0]", """
                new Dictionary<string, string>()
                {
                  ["test0"] = "test1",
                  ["test1"] = "test2",
                  ["test2"] = "test3",
                }[0]
                """);

            TestNormalizeExpression(
                "new Dictionary<SomeClass,SomeOtherClass>(){[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass()}", """
                new Dictionary<SomeClass, SomeOtherClass>()
                {
                  [new SomeClass()] = new SomeOtherClass(),
                  [new SomeClass()] = new SomeOtherClass(),
                  [new SomeClass()] = new SomeOtherClass()
                }
                """);
            TestNormalizeExpression("new Dictionary<SomeClass,SomeOtherClass>(){[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),}", """
                new Dictionary<SomeClass, SomeOtherClass>()
                {
                  [new SomeClass()] = new SomeOtherClass(),
                  [new SomeClass()] = new SomeOtherClass(),
                  [new SomeClass()] = new SomeOtherClass(),
                }
                """);
            TestNormalizeExpression("new Dictionary<SomeClass,SomeOtherClass>(){[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),}.Count", """
                new Dictionary<SomeClass, SomeOtherClass>()
                {
                  [new SomeClass()] = new SomeOtherClass(),
                  [new SomeClass()] = new SomeOtherClass(),
                  [new SomeClass()] = new SomeOtherClass(),
                }.Count
                """);
            TestNormalizeExpression("new Dictionary<SomeClass,SomeOtherClass>(){[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),}[0]", """
                new Dictionary<SomeClass, SomeOtherClass>()
                {
                  [new SomeClass()] = new SomeOtherClass(),
                  [new SomeClass()] = new SomeOtherClass(),
                  [new SomeClass()] = new SomeOtherClass(),
                }[0]
                """);

            TestNormalizeExpression(
                "new Dictionary<int,int>(){[2+2*2]=2+2*2,[2+2*2]=2+2*2,[arr[0]]=arr[0]}", """
                new Dictionary<int, int>()
                {
                  [2 + 2 * 2] = 2 + 2 * 2,
                  [2 + 2 * 2] = 2 + 2 * 2,
                  [arr[0]] = arr[0]
                }
                """);
            TestNormalizeExpression(
                "new Dictionary<int,int>(){{0,1},{1,2},{2,3}}", """
                new Dictionary<int, int>()
                {
                  {
                    0,
                    1
                  },
                  {
                    1,
                    2
                  },
                  {
                    2,
                    3
                  }
                }
                """);
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeIndexerInitializer_SingleLineContext()
        {
            VerifySingleLineInitializer(
                "new Dictionary<int,int>(){}",
                "new Dictionary<int, int>() { }");
            VerifySingleLineInitializer(
                "new Dictionary<int,int>(){[0]=1,[1]=2,[2]=3}",
                "new Dictionary<int, int>() { [0] = 1, [1] = 2, [2] = 3 }");
            VerifySingleLineInitializer(
                "new Dictionary<int,int>(){[0]=1,[1]=2,[2]=3,}",
                "new Dictionary<int, int>() { [0] = 1, [1] = 2, [2] = 3, }");
            VerifySingleLineInitializer(
                "new Dictionary<int,int>(){[0]=1,[1]=2,[2]=3,}.Count",
                "new Dictionary<int, int>() { [0] = 1, [1] = 2, [2] = 3, }.Count");
            VerifySingleLineInitializer(
                "new Dictionary<int,int>(){[0]=1,[1]=2,[2]=3,}[0]",
                "new Dictionary<int, int>() { [0] = 1, [1] = 2, [2] = 3, }[0]");

            VerifySingleLineInitializer(
                "new Dictionary<SomeClass,SomeOtherClass>(){[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass()}",
                "new Dictionary<SomeClass, SomeOtherClass>() { [new SomeClass()] = new SomeOtherClass(), [new SomeClass()] = new SomeOtherClass(), [new SomeClass()] = new SomeOtherClass() }");
            VerifySingleLineInitializer(
                "new Dictionary<SomeClass,SomeOtherClass>(){[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),}",
                "new Dictionary<SomeClass, SomeOtherClass>() { [new SomeClass()] = new SomeOtherClass(), [new SomeClass()] = new SomeOtherClass(), [new SomeClass()] = new SomeOtherClass(), }");
            VerifySingleLineInitializer(
                "new Dictionary<SomeClass,SomeOtherClass>(){[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),}.Count",
                "new Dictionary<SomeClass, SomeOtherClass>() { [new SomeClass()] = new SomeOtherClass(), [new SomeClass()] = new SomeOtherClass(), [new SomeClass()] = new SomeOtherClass(), }.Count");
            VerifySingleLineInitializer(
                "new Dictionary<SomeClass,SomeOtherClass>(){[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),[new SomeClass()]=new SomeOtherClass(),}[0]",
                "new Dictionary<SomeClass, SomeOtherClass>() { [new SomeClass()] = new SomeOtherClass(), [new SomeClass()] = new SomeOtherClass(), [new SomeClass()] = new SomeOtherClass(), }[0]");

            VerifySingleLineInitializer(
                "new Dictionary<int,int>(){[2+2*2]=2+2*2,[2+2*2]=2+2*2,[arr[0]]=arr[0]}",
                "new Dictionary<int, int>() { [2 + 2 * 2] = 2 + 2 * 2, [2 + 2 * 2] = 2 + 2 * 2, [arr[0]] = arr[0] }");
            VerifySingleLineInitializer(
                "new Dictionary<int,int>(){{0,1},{1,2},{2,3}}",
                "new Dictionary<int, int>() { { 0, 1 }, { 1, 2 }, { 2, 3 } }");
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeWithInitializer()
        {
            TestNormalizeExpression(
                "obj with{}", """
                obj with
                {
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2}", """
                obj with
                {
                  A = 1,
                  B = 2
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,}", """
                obj with
                {
                  A = 1,
                  B = 2,
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,C=obj2 with{}}", """
                obj with
                {
                  A = 1,
                  B = 2,
                  C = obj2 with
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f}}", """
                obj with
                {
                  A = 1,
                  B = 2,
                  C = obj2 with
                  {
                    D = 5l,
                    E = 2.5f
                  }
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,}}", """
                obj with
                {
                  A = 1,
                  B = 2,
                  C = obj2 with
                  {
                    D = 5l,
                    E = 2.5f,
                  }
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,},}", """
                obj with
                {
                  A = 1,
                  B = 2,
                  C = obj2 with
                  {
                    D = 5l,
                    E = 2.5f,
                  },
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{}}}", """
                obj with
                {
                  A = 1,
                  B = 2,
                  C = obj2 with
                  {
                    D = 5l,
                    E = 2.5f,
                    F = obj3 with
                    {
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{G=7u,H=3.72m}}}", """
                obj with
                {
                  A = 1,
                  B = 2,
                  C = obj2 with
                  {
                    D = 5l,
                    E = 2.5f,
                    F = obj3 with
                    {
                      G = 7u,
                      H = 3.72m
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{G=7u,H=3.72m,}}}", """
                obj with
                {
                  A = 1,
                  B = 2,
                  C = obj2 with
                  {
                    D = 5l,
                    E = 2.5f,
                    F = obj3 with
                    {
                      G = 7u,
                      H = 3.72m,
                    }
                  }
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{G=7u,H=3.72m,},}}", """
                obj with
                {
                  A = 1,
                  B = 2,
                  C = obj2 with
                  {
                    D = 5l,
                    E = 2.5f,
                    F = obj3 with
                    {
                      G = 7u,
                      H = 3.72m,
                    },
                  }
                }
                """);
            TestNormalizeExpression(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{G=7u,H=3.72m,},},}", """
                obj with
                {
                  A = 1,
                  B = 2,
                  C = obj2 with
                  {
                    D = 5l,
                    E = 2.5f,
                    F = obj3 with
                    {
                      G = 7u,
                      H = 3.72m,
                    },
                  },
                }
                """);
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeWithInitializer_SingleLineContext()
        {
            VerifySingleLineInitializer(
                "obj with{}",
                "obj with { }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2}",
                "obj with { A = 1, B = 2 }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,}",
                "obj with { A = 1, B = 2, }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,C=obj2 with{}}",
                "obj with { A = 1, B = 2, C = obj2 with { } }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f}}",
                "obj with { A = 1, B = 2, C = obj2 with { D = 5l, E = 2.5f } }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,}}",
                "obj with { A = 1, B = 2, C = obj2 with { D = 5l, E = 2.5f, } }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,},}",
                "obj with { A = 1, B = 2, C = obj2 with { D = 5l, E = 2.5f, }, }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{}}}",
                "obj with { A = 1, B = 2, C = obj2 with { D = 5l, E = 2.5f, F = obj3 with { } } }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{G=7u,H=3.72m}}}",
                "obj with { A = 1, B = 2, C = obj2 with { D = 5l, E = 2.5f, F = obj3 with { G = 7u, H = 3.72m } } }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{G=7u,H=3.72m,}}}",
                "obj with { A = 1, B = 2, C = obj2 with { D = 5l, E = 2.5f, F = obj3 with { G = 7u, H = 3.72m, } } }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{G=7u,H=3.72m,},}}",
                "obj with { A = 1, B = 2, C = obj2 with { D = 5l, E = 2.5f, F = obj3 with { G = 7u, H = 3.72m, }, } }");
            VerifySingleLineInitializer(
                "obj with{A=1,B=2,C=obj2 with{D=5l,E=2.5f,F=obj3 with{G=7u,H=3.72m,},},}",
                "obj with { A = 1, B = 2, C = obj2 with { D = 5l, E = 2.5f, F = obj3 with { G = 7u, H = 3.72m, }, }, }");
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeMixedInitializer()
        {
            TestNormalizeExpression(
                "new SomeClass{A=1,[1]=2,[2,'c']=\"test\"}", """
                new SomeClass
                {
                  A = 1,
                  [1] = 2,
                  [2, 'c'] = "test"
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{A=1,[1]=2,[2,'c']=\"test\",}", """
                new SomeClass
                {
                  A = 1,
                  [1] = 2,
                  [2, 'c'] = "test",
                }
                """);
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeMixedInitializer_SingleLineContext()
        {
            VerifySingleLineInitializer(
                "new SomeClass{A=1,[1]=2,[2,'c']=3.5f}",
                "new SomeClass { A = 1, [1] = 2, [2, 'c'] = 3.5f }");
            VerifySingleLineInitializer(
                "new SomeClass{A=1,[1]=2,[2,'c']=3.5f,}",
                "new SomeClass { A = 1, [1] = 2, [2, 'c'] = 3.5f, }");
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeNestedInitializers()
        {
            TestNormalizeExpression(
                "new{A=new{}}", """
                new
                {
                  A = new
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new{A=new{B=1,C=2}}", """
                new
                {
                  A = new
                  {
                    B = 1,
                    C = 2
                  }
                }
                """);
            TestNormalizeExpression(
                "new{A=new SomeOtherClass{}}", """
                new
                {
                  A = new SomeOtherClass
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new{A=new SomeOtherClass{B=1,C=2}}", """
                new
                {
                  A = new SomeOtherClass
                  {
                    B = 1,
                    C = 2
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{A=new{}}", """
                new SomeClass
                {
                  A = new
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{A=new{B=1,C=2}}", """
                new SomeClass
                {
                  A = new
                  {
                    B = 1,
                    C = 2
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{A=new SomeOtherClass{}}", """
                new SomeClass
                {
                  A = new SomeOtherClass
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{A=new SomeOtherClass{}}", """
                new SomeClass
                {
                  A = new SomeOtherClass
                  {
                  }
                }
                """);

            TestNormalizeExpression(
                "new{A=new int[]{}}", """
                new
                {
                  A = new int[]
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new{A=new int[]{1,2,3}}", """
                new
                {
                  A = new int[]
                  {
                    1,
                    2,
                    3
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{A=new int[]{}}", """
                new SomeClass
                {
                  A = new int[]
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass{A=new int[]{1,2,3}}", """
                new SomeClass
                {
                  A = new int[]
                  {
                    1,
                    2,
                    3
                  }
                }
                """);

            TestNormalizeExpression(
                "new SomeClass[]{new SomeClass{},new SomeClass{},new SomeClass{}}", """
                new SomeClass[]
                {
                  new SomeClass
                  {
                  },
                  new SomeClass
                  {
                  },
                  new SomeClass
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new SomeClass[]{new SomeClass{A=1,B=2,C=3},new SomeClass{A=1,B=2,C=3},new SomeClass{A=1,B=2,C=3}}", """
                new SomeClass[]
                {
                  new SomeClass
                  {
                    A = 1,
                    B = 2,
                    C = 3
                  },
                  new SomeClass
                  {
                    A = 1,
                    B = 2,
                    C = 3
                  },
                  new SomeClass
                  {
                    A = 1,
                    B = 2,
                    C = 3
                  }
                }
                """);

            TestNormalizeExpression(
                "new Dictionary<int,SomeClass>{[0]=new SomeClass(){},[1]=new SomeClass(){},[2]=new SomeClass(){}}", """
                new Dictionary<int, SomeClass>
                {
                  [0] = new SomeClass()
                  {
                  },
                  [1] = new SomeClass()
                  {
                  },
                  [2] = new SomeClass()
                  {
                  }
                }
                """);
            TestNormalizeExpression(
                "new Dictionary<int,SomeClass>{[0]=new SomeClass(){A=1,B=2,C=3},[1]=new SomeClass(){A=1,B=2,C=3},[2]=new SomeClass(){A=1,B=2,C=3}}", """
                new Dictionary<int, SomeClass>
                {
                  [0] = new SomeClass()
                  {
                    A = 1,
                    B = 2,
                    C = 3
                  },
                  [1] = new SomeClass()
                  {
                    A = 1,
                    B = 2,
                    C = 3
                  },
                  [2] = new SomeClass()
                  {
                    A = 1,
                    B = 2,
                    C = 3
                  }
                }
                """);

            TestNormalizeExpression(
                "new SomeClass{Arr={1,2,3}}", """
                new SomeClass
                {
                  Arr =
                  {
                    1,
                    2,
                    3
                  }
                }
                """);

            TestNormalizeExpression(
                "new SomeClass{A=1,B=new SomeOtherClass(){D=7,E=\"test\",F=new int[]{1,2,3}},C=new{G=new List<AndAnotherClass>{new AndAnotherClass{J=8,K=new Dictionary<int,string>{[1]=\"test1\",[2]=\"test2\",[3]=\"test3\"},L=new List<Whatever>(){}}},H=new{},I=new MixedClass(){[\"test1\"]=new MixedClass{[\"innerTest\"]=new MixedClass{M=5.01m}},M=2.71m,[\"test2\"]=new MixedClass()}}}", """
                new SomeClass
                {
                  A = 1,
                  B = new SomeOtherClass()
                  {
                    D = 7,
                    E = "test",
                    F = new int[]
                    {
                      1,
                      2,
                      3
                    }
                  },
                  C = new
                  {
                    G = new List<AndAnotherClass>
                    {
                      new AndAnotherClass
                      {
                        J = 8,
                        K = new Dictionary<int, string>
                        {
                          [1] = "test1",
                          [2] = "test2",
                          [3] = "test3"
                        },
                        L = new List<Whatever>()
                        {
                        }
                      }
                    },
                    H = new
                    {
                    },
                    I = new MixedClass()
                    {
                      ["test1"] = new MixedClass
                      {
                        ["innerTest"] = new MixedClass
                        {
                          M = 5.01m
                        }
                      },
                      M = 2.71m,
                      ["test2"] = new MixedClass()
                    }
                  }
                }
                """);
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeNestedInitializers_SingleLineContext()
        {
            VerifySingleLineInitializer(
                "new SomeClass{A=1,B=new SomeOtherClass(){D=7,E=0,F=new int[]{1,2,3}},C=new{G=new List<AndAnotherClass>{new AndAnotherClass{J=8,K=new Dictionary<int,int>{[1]=0,[2]=0,[3]=0},L=new List<Whatever>(){}}},H=new{},I=new MixedClass(){[0]=new MixedClass{[0]=new MixedClass{M=5.01m}},M=2.71m,[0]=new MixedClass()}}}",
                "new SomeClass { A = 1, B = new SomeOtherClass() { D = 7, E = 0, F = new int[] { 1, 2, 3 } }, C = new { G = new List<AndAnotherClass> { new AndAnotherClass { J = 8, K = new Dictionary<int, int> { [1] = 0, [2] = 0, [3] = 0 }, L = new List<Whatever>() { } } }, H = new { }, I = new MixedClass() { [0] = new MixedClass { [0] = new MixedClass { M = 5.01m } }, M = 2.71m, [0] = new MixedClass() } } }");
        }

        [Fact, WorkItem(61204, "https://github.com/dotnet/roslyn/issues/61204")]
        public void TestNormalizeInitializers_Statements()
        {
            TestNormalizeStatement(
                "var someVar=new SomeClass{A=1,B=2,C=3};", """
                var someVar = new SomeClass
                {
                  A = 1,
                  B = 2,
                  C = 3
                };
                """);
            TestNormalizeStatement(
                "if(true){new SomeClass{A=1,B=2,C=3};}", """
                if (true)
                {
                  new SomeClass
                  {
                    A = 1,
                    B = 2,
                    C = 3
                  };
                }
                """);
            TestNormalizeDeclaration(
                "class C{void M(){new SomeClass{A=1,B=2,C=3};}}", """
                class C
                {
                  void M()
                  {
                    new SomeClass
                    {
                      A = 1,
                      B = 2,
                      C = 3
                    };
                  }
                }
                """);
        }

        [Theory]
        [InlineData("using X=int ;", "using X = int;")]
        [InlineData("global   using X=int ;", "global using X = int;")]
        [InlineData("using X=nint;", "using X = nint;")]
        [InlineData("using X=dynamic;", "using X = dynamic;")]
        [InlineData("using X=int [] ;", "using X = int[];")]
        [InlineData("using X=(int,int) ;", "using X = (int, int);")]
        [InlineData("using  unsafe  X=int * ;", "using unsafe X = int*;")]
        [InlineData("global   using  unsafe  X=int * ;", "global using unsafe X = int*;")]
        [InlineData("using X=int ?;", "using X = int?;")]
        [InlineData("using X=delegate * <int,int> ;", "using X = delegate*<int, int>;")]
        public void TestNormalizeUsingAlias(string text, string expected)
        {
            TestNormalizeDeclaration(text, expected);
        }

        private static void VerifySingleLineInitializer(string text, string expected)
        {
            TestNormalizeExpression(
                "$\"{" + text + "}\"",
                "$\"{" + expected + "}\"");
            TestNormalizeDeclaration(
                $"[SomeAttribute({text})]",
                $"[SomeAttribute({expected})]");
            TestNormalizeExpression(
                $"new SomeClass({text})",
                $"new SomeClass({expected})");
            TestNormalizeExpression(
                $"Call({text})",
                $"Call({expected})");
            TestNormalizeDeclaration(
                $"class C{{C():base({text}){{}}}}", $$"""
                class C
                {
                  C() : base({{expected}})
                  {
                  }
                }
                """);
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70135")]
        [InlineData("if (\"\" is var I)")]
        [InlineData("if ('' is var I)")]
        [InlineData("if ('x' is var I)")]
        public void TestNormalizeParseStatementLiteralCharacter(string expression)
        {
            var syntaxNode = SyntaxFactory.ParseStatement(expression).NormalizeWhitespace();
            Assert.Equal(expression, syntaxNode.ToFullString());
        }

        [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/70135")]
        [InlineData("1 is var i")]
        [InlineData("@\"\" is var s")]
        [InlineData("\"\"\"a\"\"\" is var s")]
        [InlineData("$@\"\" is var s")]
        [InlineData("$\"\"\"a\"\"\" is var s")]
        [InlineData("\"\"u8 is var s")]
        public void TestNormalizeParseExpressionLiteralCharacter(string expression)
        {
            var syntaxNode = SyntaxFactory.ParseExpression(expression).NormalizeWhitespace();
            Assert.Equal(expression, syntaxNode.ToFullString());
        }

        [Fact]
        public void TestNormalizeAllowsRefStructConstraint_01()
        {
            TestNormalizeDeclaration("""
                class C1<T> where T:allows   ref   struct   ;
                class C2<T, S> where T:allows   ref   struct,where S:struct     ;
                class C3<T> where T:struct,allows   ref   struct            ;
                class C4<T> where T:new(),allows   ref   struct          ;
                class C5<T>
                where
                T
                :
                allows
                ref
                struct
                ;
                class C6<T, S> where T:allows   ref   struct        where S:struct     ;
                """, """
                class C1<T>
                  where T : allows ref struct;
                class C2<T, S>
                  where T : allows ref struct , where S : struct;
                class C3<T>
                  where T : struct, allows ref struct;
                class C4<T>
                  where T : new(), allows ref struct;
                class C5<T>
                  where T : allows ref struct;
                class C6<T, S>
                  where T : allows ref struct where S : struct;
                """);
        }

        [Fact]
        public void TestNormalizeAllowsRefStructConstraint_02()
        {
            TestNormalizeDeclaration("""
                class C
                {
                    void M1<T>() where T:allows   ref   struct   {}
                    void M2<T, S>() where T:allows   ref   struct,where S:struct     {}
                    void M3<T>() where T:struct,allows   ref   struct            {}
                    void M4<T>() where T:new(),allows   ref   struct          {}
                    void M5<T>()
                    where
                    T
                    :
                    allows
                    ref
                    struct
                    {
                    }
                    void M6<T, S>() where T:allows   ref   struct       where S:struct     {}
                }
                """, """
                class C
                {
                  void M1<T>()
                    where T : allows ref struct
                  {
                  }

                  void M2<T, S>()
                    where T : allows ref struct , where S : struct
                  {
                  }

                  void M3<T>()
                    where T : struct, allows ref struct
                  {
                  }

                  void M4<T>()
                    where T : new(), allows ref struct
                  {
                  }

                  void M5<T>()
                    where T : allows ref struct
                  {
                  }

                  void M6<T, S>()
                    where T : allows ref struct where S : struct
                  {
                  }
                }
                """);
        }
    }
}
