// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.SimplifyLinqExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.SimplifyLinqExpression;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.SimplifyLinqExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpSimplifyLinqExpressionDiagnosticAnalyzer,
    SimplifyLinqExpressionCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsSimplifyLinqExpression)]
public sealed partial class CSharpSimplifyLinqExpressionTests
{
    [Theory, CombinatorialData]
    public static Task TestAllowedMethodTypes(
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
        => new VerifyCS.Test
        {
            TestCode = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                static void Main()
                {
                    static IEnumerable<int> Data()
                    {
                        yield return 1;
                        yield return 2;
                    }

                    var test = [|Data().Where({{lambda}}).{{methodName}}()|];
                }
            }
            """,
            FixedCode = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                static void Main()
                {
                    static IEnumerable<int> Data()
                    {
                        yield return 1;
                        yield return 2;
                    }

                    var test = Data().{{methodName}}({{lambda}});
                }
            }
            """
        }.RunAsync();

    [Theory, CombinatorialData]
    public static Task TestWhereWithIndexMethodTypes(
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
        => VerifyCS.VerifyAnalyzerAsync($$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                static void Main()
                {
                    static IEnumerable<int> Data()
                    {
                        yield return 1;
                        yield return 2;
                    }

                    var test = Data().Where({{lambda}}).{{methodName}}();
                }
            }
            """);

    [Theory, CombinatorialData]
    public Task TestQueryComprehensionSyntax(
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
        => new VerifyCS.Test
        {
            TestCode = $$"""
            using System.Linq;

            class Test
            {
                static void M()
                {
                    var test1 = [|(from value in Enumerable.Range(0, 10) select value).Where({{lambda}}).{{methodName}}()|];
                }
            }
            """,
            FixedCode = $$"""
            using System.Linq;

            class Test
            {
                static void M()
                {
                    var test1 = (from value in Enumerable.Range(0, 10) select value).{{methodName}}({{lambda}});
                }
            }
            """
        }.RunAsync();

    [Theory]
    [InlineData("First")]
    [InlineData("Last")]
    [InlineData("Single")]
    [InlineData("Any")]
    [InlineData("Count")]
    [InlineData("SingleOrDefault")]
    [InlineData("FirstOrDefault")]
    [InlineData("LastOrDefault")]
    public Task TestMultiLineLambda(string methodName)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                static void Main()
                {
                    static IEnumerable<int> Data()
                    {
                        yield return 1;
                        yield return 2;
                    }

                    var test = [|Data().Where(x => 
                    { 
                        Console.Write(x);
                        return x == 1;
                    }).{{methodName}}()|];
                }
            }
            """,
            FixedCode = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                static void Main()
                {
                    static IEnumerable<int> Data()
                    {
                        yield return 1;
                        yield return 2;
                    }

                    var test = Data().{{methodName}}(x => 
                    { 
                        Console.Write(x);
                        return x == 1;
                    });
                }
            }
            """
        }.RunAsync();

    [Theory]
    [InlineData("First", "string")]
    [InlineData("Last", "string")]
    [InlineData("Single", "string")]
    [InlineData("Any", "bool")]
    [InlineData("Count", "int")]
    [InlineData("SingleOrDefault", "string")]
    [InlineData("FirstOrDefault", "string")]
    [InlineData("LastOrDefault", "string")]
    public Task TestOutsideFunctionCallLambda(string methodName, string returnType)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                public static bool FooTest(string input)
                {
                    return true;
                }

                static IEnumerable<string> test = new List<string> { "hello", "world", "!" };
                {{returnType}} result = [|test.Where(x => FooTest(x)).{{methodName}}()|];
            }
            """,
            FixedCode = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                public static bool FooTest(string input)
                {
                    return true;
                }

                static IEnumerable<string> test = new List<string> { "hello", "world", "!" };
                {{returnType}} result = test.{{methodName}}(x => FooTest(x));
            }
            """
        }.RunAsync();

    [Theory]
    [InlineData("First")]
    [InlineData("Last")]
    [InlineData("Single")]
    [InlineData("Any")]
    [InlineData("Count")]
    [InlineData("SingleOrDefault")]
    [InlineData("FirstOrDefault")]
    [InlineData("LastOrDefault")]
    public Task TestQueryableIsNotConsidered(string methodName)
        => VerifyCS.VerifyAnalyzerAsync($$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;
            namespace demo
            {
                class Test
                {
                    void M()
                    {
                        List<int> testvar1 = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 };
                        IQueryable<int> testvar2 = testvar1.AsQueryable().Where(x => x % 2 == 0);
                        var output = testvar2.Where(x => x == 4).{{methodName}}();
                    }
                }
            }
            """);

    [Theory, CombinatorialData]
    public Task TestNestedLambda(
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
        => VerifyCS.VerifyCodeFixAsync(
            $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                void M()
                {
                    IEnumerable<string> test = new List<string> { "hello", "world", "!" };
                    var test5 = [|test.Where(a => [|a.Where(s => s.Equals("hello")).{{secondMethod}}()|].Equals("hello")).{{firstMethod}}()|];
                }
            }
            """,
            $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                void M()
                {
                    IEnumerable<string> test = new List<string> { "hello", "world", "!" };
                    var test5 = test.{{firstMethod}}(a => a.{{secondMethod}}(s => s.Equals("hello")).Equals("hello"));
                }
            }
            """);

    [Theory]
    [InlineData("First")]
    [InlineData("Last")]
    [InlineData("Single")]
    [InlineData("Any")]
    [InlineData("Count")]
    [InlineData("SingleOrDefault")]
    [InlineData("FirstOrDefault")]
    [InlineData("LastOrDefault")]
    public Task TestExplicitEnumerableCall(string methodName)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using System.Linq.Expressions;

            class Test
            {
                static void Main()
                {
                    IEnumerable<int> test = new List<int> { 1, 2, 3, 4, 5};
                    [|Enumerable.Where(test, (x => x == 1)).{{methodName}}()|];
                }
            }
            """,
            FixedCode = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using System.Linq.Expressions;

            class Test
            {
                static void Main()
                {
                    IEnumerable<int> test = new List<int> { 1, 2, 3, 4, 5};
                    Enumerable.{{methodName}}(test, (x => x == 1));
                }
            }
            """
        }.RunAsync();

    [Fact]
    public Task TestUserDefinedWhere()
        => VerifyCS.VerifyAnalyzerAsync("""
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
            """);

    [Theory]
    [InlineData("First")]
    [InlineData("Last")]
    [InlineData("Single")]
    [InlineData("Any")]
    [InlineData("Count")]
    [InlineData("SingleOrDefault")]
    [InlineData("FirstOrDefault")]
    [InlineData("LastOrDefault")]
    public Task TestArgumentsInSecondCall(string methodName)
        => VerifyCS.VerifyAnalyzerAsync($$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;

            class Test
            {
                static void M()
                {
                    IEnumerable<string> test1 = new List<string>{ "hello", "world", "!" };
                    var test2 = test1.Where(x => x == "!").{{methodName}}(x => x.Length == 1);
                }
            }
            """);

    [Fact]
    public Task TestUnsupportedMethod()
        => VerifyCS.VerifyAnalyzerAsync("""
            using System;
            using System.Linq;
            using System.Collections;
            using System.Collections.Generic;

            class Test : IEnumerable<int>
            {
                public IEnumerator<int> GetEnumerator() => null;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
                public int Count() => 0;

                void M()
                {
                    int test2 = new Test().Where(x => x > 0).Count();
                }
            }
            """);

    [Fact]
    public Task TestExpressionTreeInput()
        => VerifyCS.VerifyAnalyzerAsync("""
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
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/52283")]
    public static Task TestTrivia1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;

                class C
                {
                    static void Main(string[] args)
                    {
                        var v = [|args.Skip(1)
                            .Where(a => a.Length == 1).Count()|];
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    static void Main(string[] args)
                    {
                        var v = args.Skip(1)
                            .Count(a => a.Length == 1);
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71293")]
    public static Task TestOffOfObjectCreation()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;

                class C
                {
                    public void Test()
                    {
                        int cnt2 = [|new List<string>().Where(x => x.Equals("hello")).Count()|];
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    public void Test()
                    {
                        int cnt2 = new List<string>().Count(x => x.Equals("hello"));
                    }
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71293")]
    public static Task TestOffOfFieldReference()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;

                class C
                {
                    public void Test()
                    {
                        int cnt3 = [|s_wordsField.Where(x => x.Equals("hello")).Count()|];
                    }

                    private static readonly List<string> s_wordsField;
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    public void Test()
                    {
                        int cnt3 = s_wordsField.Count(x => x.Equals("hello"));
                    }
                
                    private static readonly List<string> s_wordsField;
                }
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75845")]
    public static Task TestSelectSum()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;

                class C
                {
                    public void Test(int[] numbers)
                    {
                        var sumOfSquares = [|numbers.Select(n => n * n).Sum()|];
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    public void Test(int[] numbers)
                    {
                        var sumOfSquares = numbers.Sum(n => n * n);
                    }
                }
                """
        }.RunAsync();
}
