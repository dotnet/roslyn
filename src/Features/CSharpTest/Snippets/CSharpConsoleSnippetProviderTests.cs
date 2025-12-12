// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Snippets;

[Trait(Traits.Feature, Traits.Features.Snippets)]
public sealed class CSharpConsoleSnippetProviderTests : AbstractCSharpSnippetProviderTests
{
    protected override string SnippetIdentifier => "cw";

    [Fact]
    public Task InsertConsoleSnippetInMethodTest()
        => VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    $$
                }
            }
            """, """
            using System;

            class Program
            {
                public void Method()
                {
                    Console.WriteLine($$);
                }
            }
            """);

    [Fact]
    public Task InsertNormalConsoleSnippetInAsyncContextTest()
        => VerifySnippetAsync("""
            using System.Threading.Tasks;

            class Program
            {
                public async Task MethodAsync()
                {
                    $$
                }
            }
            """, """
            using System;
            using System.Threading.Tasks;

            class Program
            {
                public async Task MethodAsync()
                {
                    Console.WriteLine($$);
                }
            }
            """);

    [Fact]
    public Task InsertConsoleSnippetInGlobalContextTest()
        => VerifySnippetAsync("""
            $$
            """, """
            using System;

            Console.WriteLine($$);
            """);

    [Fact]
    public Task NoConsoleSnippetInBlockNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);

    [Fact]
    public Task NoConsoleSnippetInFileScopedNamespaceTest()
        => VerifySnippetIsAbsentAsync("""
            namespace Namespace;

            $$
            """);

    [Fact]
    public Task InsertConsoleSnippetInConstructorTest()
        => VerifySnippetAsync("""
            class Program
            {
                public Program()
                {
                    var x = 5;
                    $$
                }
            }
            """, """
            using System;

            class Program
            {
                public Program()
                {
                    var x = 5;
                    Console.WriteLine($$);
                }
            }
            """);

    [Fact]
    public Task InsertConsoleSnippetInLocalFunctionTest()
        => VerifySnippetAsync("""
            class Program
            {
                public void Method()
                {
                    var x = 5;
                    void LocalMethod()
                    {
                        $$
                    }
                }
            }
            """, """
            using System;

            class Program
            {
                public void Method()
                {
                    var x = 5;
                    void LocalMethod()
                    {
                        Console.WriteLine($$);
                    }
                }
            }
            """);

    [Fact]
    public Task InsertConsoleSnippetInAnonymousFunctionTest()
        => VerifySnippetAsync("""
            public delegate void Print(int value);

            static void Main(string[] args)
            {
                Print print = delegate(int val)
                {
                    $$
                };

            }
            """, """
            using System;

            public delegate void Print(int value);

            static void Main(string[] args)
            {
                Print print = delegate(int val)
                {
                    Console.WriteLine($$);
                };

            }
            """);

    [Fact]
    public Task InsertConsoleSnippetInParenthesizedLambdaExpressionTest()
        => VerifySnippetAsync("""
            using System;

            Func<int, int, bool> testForEquality = (x, y) =>
            {
                $$
                return x == y;
            };
            """, """
            using System;

            Func<int, int, bool> testForEquality = (x, y) =>
            {
                Console.WriteLine($$);
                return x == y;
            };
            """);

    [Fact]
    public Task NoConsoleSnippetInSwitchExpressionTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                   var operation = 2;  

                    var result = operation switch  
                    {
                        $$
                        1 => "Case 1",  
                        2 => "Case 2",  
                        3 => "Case 3",  
                        4 => "Case 4",  
                    };
                }
            }
            """);

    [Fact]
    public Task NoConsoleSnippetInStringTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var str = "$$";
                }
            }
            """);

    [Fact]
    public Task NoConsoleSnippetInConstructorArgumentsTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var test = new Test($$);
                }
            }

            class Test
            {
                public Test(string val)
                {
                }
            }
            """);

    [Fact]
    public Task NoConsoleSnippetInParameterListTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method(int x, $$)
                {
                }
            }
            """);

    [Fact]
    public Task NoConsoleSnippetInRecordDeclarationTest()
        => VerifySnippetIsAbsentAsync("""
            public record Person
            {
                $$
                public string FirstName { get; init; }
                public string LastName { get; init; }
            };
            """);

    [Fact]
    public Task NoConsoleSnippetInVariableDeclarationTest()
        => VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var x = $$
                }
            }
            """);

    /// <summary>
    /// We want to fix this case and insert the fully qualified namespace
    /// in a future fix.
    /// </summary>
    [Fact]
    public Task InsertConsoleSnippetWithPropertyNamedConsoleTest()
        => VerifySnippetAsync("""
            class Program
            {
                public int Console { get; set; }

                public void Method()
                {
                    $$
                }
            }
            """, """
            using System;

            class Program
            {
                public int Console { get; set; }

                public void Method()
                {
                    Console.WriteLine($$);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public Task InsertConsoleSnippetInVoidReturningLambdaTest1()
        => VerifySnippetAsync("""
            using System;

            M(() => $$);

            void M(Action a)
            {
            }
            """, """
            using System;

            M(() => Console.WriteLine($$));

            void M(Action a)
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public Task InsertConsoleSnippetInVoidReturningLambdaTest2()
        => VerifySnippetAsync("""
            using System;

            Action action = () => $$
            """, """
            using System;
            
            Action action = () => Console.WriteLine($$)
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public Task InsertConsoleSnippetInVoidReturningLambdaTest_TypeInference()
        => VerifySnippetAsync("""
            using System;

            var action = () => $$
            """, """
            using System;
            
            var action = () => Console.WriteLine($$)
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public Task NoConsoleSnippetInNonVoidReturningLambdaTest1()
        => VerifySnippetIsAbsentAsync("""
            using System;
            
            M(() => $$);
            
            void M(Func<int> f)
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public Task NoConsoleSnippetInNonVoidReturningLambdaTest2()
        => VerifySnippetIsAbsentAsync("""
            using System;
            
            Func<int> f = () => $$
            """);
}
