// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.SimplifyLinqExpression
{
    using VerifyCS = CSharpCodeFixVerifier<
        CSharpSimplifyLinqExpressionDiagnosticAnalyzer,
        CSharpSimplifyLinqExpressionCodeFixProvider>;

    [Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpression)]
    public partial class CSharpSimplifyLinqExpressionTests
    {
        [Theory, CombinatorialData]
        public static async Task TestAllowedMethodTypes(
            [CombinatorialValues(
                "x => x==1",
                "(x) => x==1",
                "x => { return x==1; }",
                "(x) => { return x==1; }")]
            string lambda,
            [CombinatorialValues(
                "First",
                "Last",
                "Single",
                "Any",
                "Count",
                "SingleOrDefault",
                "FirstOrDefault",
                "LastOrDefault")]
            string methodName)
        {
            await new VerifyCS.Test
            {
                TestCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{{
    static void Main()
    {{
        static IEnumerable<int> Data()
        {{
            yield return 1;
            yield return 2;
        }}

        var test = [|Data().Where({lambda}).{methodName}()|];
    }}
}}",
                FixedCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{{
    static void Main()
    {{
        static IEnumerable<int> Data()
        {{
            yield return 1;
            yield return 2;
        }}

        var test = Data().{methodName}({lambda});
    }}
}}"
            }.RunAsync();
        }

        [Theory, CombinatorialData]
        public static async Task TestWhereWithIndexMethodTypes(
            [CombinatorialValues(
                "(x, index) => x==index",
                "(x, index) => { return x==index; }")]
            string lambda,
            [CombinatorialValues(
                "First",
                "Last",
                "Single",
                "Any",
                "Count",
                "SingleOrDefault",
                "FirstOrDefault",
                "LastOrDefault")]
            string methodName)
        {
            var testCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;
 
class Test
{{
    static void Main()
    {{
        static IEnumerable<int> Data()
        {{
            yield return 1;
            yield return 2;
        }}

        var test = Data().Where({lambda}).{methodName}();
    }}
}}";
            await VerifyCS.VerifyAnalyzerAsync(testCode);
        }

        [Theory, CombinatorialData]
        public async Task TestQueryComprehensionSyntax(
            [CombinatorialValues(
                "x => x==1",
                "x => { return x==1; }")]
            string lambda,
            [CombinatorialValues(
                "First",
                "Last",
                "Single",
                "Any",
                "Count",
                "SingleOrDefault",
                "FirstOrDefault",
                "LastOrDefault")]
            string methodName)
        {
            await new VerifyCS.Test
            {
                TestCode = $@"
using System.Linq;

class Test
{{
    static void M()
    {{
        var test1 = [|(from value in Enumerable.Range(0, 10) select value).Where({lambda}).{methodName}()|];
    }}
}}",
                FixedCode = $@"
using System.Linq;

class Test
{{
    static void M()
    {{
        var test1 = (from value in Enumerable.Range(0, 10) select value).{methodName}({lambda});
    }}
}}"
            }.RunAsync();
        }

        [Theory]
        [InlineData("First")]
        [InlineData("Last")]
        [InlineData("Single")]
        [InlineData("Any")]
        [InlineData("Count")]
        [InlineData("SingleOrDefault")]
        [InlineData("FirstOrDefault")]
        [InlineData("LastOrDefault")]
        public async Task TestMultiLineLambda(string methodName)
        {
            await new VerifyCS.Test
            {
                TestCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{{
    static void Main()
    {{
        static IEnumerable<int> Data()
        {{
            yield return 1;
            yield return 2;
        }}

        var test = [|Data().Where(x => 
        {{ 
            Console.Write(x);
            return x == 1;
        }}).{methodName}()|];
    }}
}}",
                FixedCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{{
    static void Main()
    {{
        static IEnumerable<int> Data()
        {{
            yield return 1;
            yield return 2;
        }}

        var test = Data().{methodName}(x => 
        {{ 
            Console.Write(x);
            return x == 1;
        }});
    }}
}}"
            }.RunAsync();
        }

        [Theory]
        [InlineData("First", "string")]
        [InlineData("Last", "string")]
        [InlineData("Single", "string")]
        [InlineData("Any", "bool")]
        [InlineData("Count", "int")]
        [InlineData("SingleOrDefault", "string")]
        [InlineData("FirstOrDefault", "string")]
        [InlineData("LastOrDefault", "string")]
        public async Task TestOutsideFunctionCallLambda(string methodName, string returnType)
        {
            await new VerifyCS.Test
            {
                TestCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{{
    public static bool FooTest(string input)
    {{
        return true;
    }}

    static IEnumerable<string> test = new List<string> {{ ""hello"", ""world"", ""!"" }};
    {returnType} result = [|test.Where(x => FooTest(x)).{methodName}()|];
}}",
                FixedCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{{
    public static bool FooTest(string input)
    {{
        return true;
    }}

    static IEnumerable<string> test = new List<string> {{ ""hello"", ""world"", ""!"" }};
    {returnType} result = test.{methodName}(x => FooTest(x));
}}"
            }.RunAsync();
        }

        [Theory]
        [InlineData("First")]
        [InlineData("Last")]
        [InlineData("Single")]
        [InlineData("Any")]
        [InlineData("Count")]
        [InlineData("SingleOrDefault")]
        [InlineData("FirstOrDefault")]
        [InlineData("LastOrDefault")]
        public async Task TestQueryableIsNotConsidered(string methodName)
        {
            var source = $@"
using System;
using System.Linq;
using System.Collections.Generic;
namespace demo
{{
    class Test
    {{
        void M()
        {{
            List<int> testvar1 = new List<int> {{ 1, 2, 3, 4, 5, 6, 7, 8 }};
            IQueryable<int> testvar2 = testvar1.AsQueryable().Where(x => x % 2 == 0);
            var output = testvar2.Where(x => x == 4).{methodName}();
        }}
    }}
}}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Theory, CombinatorialData]
        public async Task TestNestedLambda(
            [CombinatorialValues(
                "First",
                "Last",
                "Single",
                "Any",
                "Count",
                "SingleOrDefault",
                "FirstOrDefault",
                "LastOrDefault")]
            string firstMethod,
            [CombinatorialValues(
                "First",
                "Last",
                "Single",
                "Any",
                "Count",
                "SingleOrDefault",
                "FirstOrDefault",
                "LastOrDefault")]
            string secondMethod)
        {
            var testCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{{
    void M()
    {{
        IEnumerable<string> test = new List<string> {{ ""hello"", ""world"", ""!"" }};
        var test5 = [|test.Where(a => [|a.Where(s => s.Equals(""hello"")).{secondMethod}()|].Equals(""hello"")).{firstMethod}()|];
    }}
}}";
            var fixedCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{{
    void M()
    {{
        IEnumerable<string> test = new List<string> {{ ""hello"", ""world"", ""!"" }};
        var test5 = test.{firstMethod}(a => a.{secondMethod}(s => s.Equals(""hello"")).Equals(""hello""));
    }}
}}";
            await VerifyCS.VerifyCodeFixAsync(
                testCode,
                fixedCode);
        }

        [Theory]
        [InlineData("First")]
        [InlineData("Last")]
        [InlineData("Single")]
        [InlineData("Any")]
        [InlineData("Count")]
        [InlineData("SingleOrDefault")]
        [InlineData("FirstOrDefault")]
        [InlineData("LastOrDefault")]
        public async Task TestExplicitEnumerableCall(string methodName)
        {
            await new VerifyCS.Test
            {
                TestCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

class Test
{{
    static void Main()
    {{
        IEnumerable<int> test = new List<int> {{ 1, 2, 3, 4, 5}};
        [|Enumerable.Where(test, (x => x == 1)).{methodName}()|];
    }}
}}",
                FixedCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;

class Test
{{
    static void Main()
    {{
        IEnumerable<int> test = new List<int> {{ 1, 2, 3, 4, 5}};
        Enumerable.{methodName}(test, (x => x == 1));
    }}
}}"
            }.RunAsync();
        }

        [Fact]
        public async Task TestUserDefinedWhere()
        {
            var source = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                namespace demo
                {
                    class Test
                    {
                        public class TestClass4
                        {
                            private string test;
                            public TestClass4() => test = "hello";

                            public TestClass4 Where(Func<string, bool> input)
                            {
                                return this;
                            }

                            public string Single()
                            {
                                return test;
                            }
                        }
                        static void Main()
                        {
                            TestClass4 Test1 = new TestClass4();
                            TestClass4 test = Test1.Where(y => true);
                        }
                    }
                }
                """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Theory]
        [InlineData("First")]
        [InlineData("Last")]
        [InlineData("Single")]
        [InlineData("Any")]
        [InlineData("Count")]
        [InlineData("SingleOrDefault")]
        [InlineData("FirstOrDefault")]
        [InlineData("LastOrDefault")]
        public async Task TestArgumentsInSecondCall(string methodName)
        {
            var source = $@"
using System;
using System.Linq;
using System.Collections.Generic;

class Test
{{
    static void M()
    {{
        IEnumerable<string> test1 = new List<string>{{ ""hello"", ""world"", ""!"" }};
        var test2 = test1.Where(x => x == ""!"").{methodName}(x => x.Length == 1);
    }}
}}";
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task TestUnsupportedFunction()
        {
            var source = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                namespace demo
                {
                    class Test
                    {
                        static List<int> test1 = new List<int> { 3, 12, 4, 6, 20 };
                        int test2 = test1.Where(x => x > 0).Count();
                    }
                }
                """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }

        [Fact]
        public async Task TestExpressionTreeInput()
        {
            var source = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class Test
                {
                    void Main()
                    {
                        string[] places = { "Beach", "Pool", "Store", "House",
                                   "Car", "Salon", "Mall", "Mountain"};

                        IQueryable<String> queryableData = places.AsQueryable<string>();
                        ParameterExpression pe = Expression.Parameter(typeof(string), "place");

                        Expression left = Expression.Call(pe, typeof(string).GetMethod("ToLower", System.Type.EmptyTypes));
                        Expression right = Expression.Constant("coho winery");
                        Expression e1 = Expression.Equal(left, right);

                        left = Expression.Property(pe, typeof(string).GetProperty("Length"));
                        right = Expression.Constant(16, typeof(int));
                        Expression e2 = Expression.GreaterThan(left, right);

                        Expression predicateBody = Expression.OrElse(e1, e2);
                        Expression<Func<int, bool>> lambda1 = num => num < 5;

                        string result = queryableData.Where(Expression.Lambda<Func<string, bool>>(predicateBody, new ParameterExpression[] { pe })).First();
                    }
                }
                """;
            await VerifyCS.VerifyAnalyzerAsync(source);
        }
    }
}
