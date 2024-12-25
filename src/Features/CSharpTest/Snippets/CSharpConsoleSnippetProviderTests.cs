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
    public async Task InsertConsoleSnippetInMethodTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertNormalConsoleSnippetInAsyncContextTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConsoleSnippetInGlobalContextTest()
    {
        await VerifySnippetAsync("""
            $$
            """, """
            using System;

            Console.WriteLine($$);
            """);
    }

    [Fact]
    public async Task NoConsoleSnippetInBlockNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace
            {
                $$
            }
            """);
    }

    [Fact]
    public async Task NoConsoleSnippetInFileScopedNamespaceTest()
    {
        await VerifySnippetIsAbsentAsync("""
            namespace Namespace;

            $$
            """);
    }

    [Fact]
    public async Task InsertConsoleSnippetInConstructorTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConsoleSnippetInLocalFunctionTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConsoleSnippetInAnonymousFunctionTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task InsertConsoleSnippetInParenthesizedLambdaExpressionTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact]
    public async Task NoConsoleSnippetInSwitchExpressionTest()
    {
        await VerifySnippetIsAbsentAsync("""
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
    }

    [Fact]
    public async Task NoConsoleSnippetInStringTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var str = "$$";
                }
            }
            """);
    }

    [Fact]
    public async Task NoConsoleSnippetInConstructorArgumentsTest()
    {
        await VerifySnippetIsAbsentAsync("""
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
    }

    [Fact]
    public async Task NoConsoleSnippetInParameterListTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method(int x, $$)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task NoConsoleSnippetInRecordDeclarationTest()
    {
        await VerifySnippetIsAbsentAsync("""
            public record Person
            {
                $$
                public string FirstName { get; init; }
                public string LastName { get; init; }
            };
            """);
    }

    [Fact]
    public async Task NoConsoleSnippetInVariableDeclarationTest()
    {
        await VerifySnippetIsAbsentAsync("""
            class Program
            {
                public void Method()
                {
                    var x = $$
                }
            }
            """);
    }

    /// <summary>
    /// We want to fix this case and insert the fully qualified namespace
    /// in a future fix.
    /// </summary>
    [Fact]
    public async Task InsertConsoleSnippetWithPropertyNamedConsoleTest()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public async Task InsertConsoleSnippetInVoidReturningLambdaTest1()
    {
        await VerifySnippetAsync("""
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public async Task InsertConsoleSnippetInVoidReturningLambdaTest2()
    {
        await VerifySnippetAsync("""
            using System;

            Action action = () => $$
            """, """
            using System;
            
            Action action = () => Console.WriteLine($$)
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public async Task InsertConsoleSnippetInVoidReturningLambdaTest_TypeInference()
    {
        await VerifySnippetAsync("""
            using System;

            var action = () => $$
            """, """
            using System;
            
            var action = () => Console.WriteLine($$)
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public async Task NoConsoleSnippetInNonVoidReturningLambdaTest1()
    {
        await VerifySnippetIsAbsentAsync("""
            using System;
            
            M(() => $$);
            
            void M(Func<int> f)
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72266")]
    public async Task NoConsoleSnippetInNonVoidReturningLambdaTest2()
    {
        await VerifySnippetIsAbsentAsync("""
            using System;
            
            Func<int> f = () => $$
            """);
    }
}
