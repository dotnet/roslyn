// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.MoveDeclarationNearReference;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveDeclarationNearReference;

[Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
public sealed class MoveDeclarationNearReferenceTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpMoveDeclarationNearReferenceCodeRefactoringProvider();

    [Fact]
    public Task TestMove1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x;
                    {
                        Console.WriteLine(x);
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    {
                        int x;
                        Console.WriteLine(x);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMove1_TopLevelStatement()
        => TestAsync(
            """
            int [||]x;
            {
                Console.WriteLine(x);
            }
            """,
            """
            {
                int x;
                Console.WriteLine(x);
            }
            """,
            new(TestOptions.Regular));

    [Fact]
    public Task TestMove2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x;
                    Console.WriteLine();
                    Console.WriteLine(x);
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    Console.WriteLine();
                    int x;
                    Console.WriteLine(x);
                }
            }
            """);

    [Fact]
    public Task TestMove2_TopLevelStatement()
        => TestAsync(
            """
            int [||]x;
            Console.WriteLine();
            Console.WriteLine(x);
            """,
            """
            Console.WriteLine();
            int x;
            Console.WriteLine(x);
            """,
            new(TestOptions.Regular));

    [Fact]
    public Task TestMove3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x;
                    Console.WriteLine();
                    {
                        Console.WriteLine(x);
                    }

                    {
                        Console.WriteLine(x);
                    }
                }
            """,
            """
            class C
            {
                void M()
                {
                    Console.WriteLine();
                    int x;
                    {
                        Console.WriteLine(x);
                    }

                    {
                        Console.WriteLine(x);
                    }
                }
            """);

    [Fact]
    public Task TestMove3_TopLevelStatement()
        => TestAsync(
            """
            int [||]x;
            Console.WriteLine();
            {
                Console.WriteLine(x);
            }

            {
                Console.WriteLine(x);
            }
            """,
            """
            Console.WriteLine();
            int x;
            {
            Console.WriteLine(x);
            }

            {
                Console.WriteLine(x);
            }
            """,
            new(TestOptions.Regular));

    [Fact]
    public Task TestMove4()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x;
                    Console.WriteLine();
                    {
                        Console.WriteLine(x);
                    }
                }
            """,
            """
            class C
            {
                void M()
                {
                    Console.WriteLine();
                    {
                        int x;
                        Console.WriteLine(x);
                    }
                }
            """);

    [Fact]
    public Task TestMove4_TopLevelStatement()
        => TestAsync(
            """
            int [||]x;
            Console.WriteLine();
            {
                Console.WriteLine(x);
            }
            """,
            """
            Console.WriteLine();
            {
                int x;
                Console.WriteLine(x);
            }
            """,
            new(TestOptions.Regular));

    [Fact]
    public Task TestAssign1()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x;
                    {
                        x = 5;
                        Console.WriteLine(x);
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    {
                        int x = 5;
                        Console.WriteLine(x);
                    }
                }
            }
            """);

    [Fact]
    public Task TestAssign2()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x = 0;
                    {
                        x = 5;
                        Console.WriteLine(x);
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    {
                        int x = 5;
                        Console.WriteLine(x);
                    }
                }
            }
            """);

    [Fact]
    public Task TestAssign3()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    var [||]x = (short)0;
                    {
                        x = 5;
                        Console.WriteLine(x);
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    {
                        var x = (short)0;
                        x = 5;
                        Console.WriteLine(x);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMissing1()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x;
                    Console.WriteLine(x);
                }
            }
            """);

    [Fact]
    public Task TestMissing1_TopLevelStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            int [||]x;
            Console.WriteLine(x);
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538424")]
    public Task TestMissingWhenReferencedInDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    object[] [||]x = {
                        x = null
                    };
                    x.ToString();
                }
            }
            """);

    [Fact]
    public Task TestMissingWhenInDeclarationGroup()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main()
                {
                    int [||]i = 5;
                    int j = 10;
                    Console.WriteLine(i);
                }
            }
            """);

    [Fact]
    public Task TestMissingWhenInDeclarationGroup_TopLevelStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            int [||]i = 5;
            int j = 10;
            Console.WriteLine(i);
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541475")]
    public Task Regression8190()
        => TestMissingInRegularAndScriptAsync(
            """
            class Program
            {
                void M()
                {
                    {
                        object x;
                        [|object|] }
                }
            }
            """);

    [Fact]
    public Task TestFormatting()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    int [||]i = 5; Console.WriteLine();
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();
                    int i = 5; Console.Write(i);
                }
            }
            """);

    [Fact]
    public Task TestFormatting_TopLevelStatement()
        => TestAsync(
            """
            int [||]i = 5; Console.WriteLine();
            Console.Write(i);
            """,
            """
            Console.WriteLine();
            int i = 5; Console.Write(i);
            """,
            new(TestOptions.Regular));

    [Fact]
    public Task TestMissingInHiddenBlock1()
        => TestMissingInRegularAndScriptAsync(
            """
            #line default
            class Program
            {
                void Main()
                {
                    int [|x|] = 0;
                    Goo();
            #line hidden
                    Bar(x);
                }
            #line default
            }
            """);

    [Fact]
    public Task TestMissingInHiddenBlock2()
        => TestMissingInRegularAndScriptAsync(
            """
            #line default
            class Program
            {
                void Main()
                {
                    int [|x|] = 0;
                    Goo();
            #line hidden
                    Goo();
            #line default
                    Bar(x);
                }
            }
            """);

    [Fact]
    public Task TestMissingInHiddenBlock2_TopLevelStatement()
        => TestMissingInRegularAndScriptAsync(
            """
            #line default

            int [|x|] = 0;
            Goo();
            #line hidden
            Goo();
            #line default
            Bar(x);
            """);

    [Fact]
    public Task TestAvailableInNonHiddenBlock1()
        => TestInRegularAndScriptAsync(
            """
            #line default
            class Program
            {
                void Main()
                {
                    int [||]x = 0;
                    Goo();
                    Bar(x);
            #line hidden
                }
            #line default
            }
            """,
            """
            #line default
            class Program
            {
                void Main()
                {
                    Goo();
                    int x = 0;
                    Bar(x);
            #line hidden
                }
            #line default
            }
            """);

    [Fact]
    public Task TestAvailableInNonHiddenBlock2()
        => TestInRegularAndScriptAsync(
            """
            #line default
            class Program
            {
                void Main()
                {
                    int [||]x = 0;
                    Goo();
            #line hidden
                    Goo();
            #line default
                    Goo();
                    Bar(x);
                }
            }
            """,
            """
            #line default
            class Program
            {
                void Main()
                {
                    Goo();
            #line hidden
                    Goo();
            #line default
                    Goo();
                    int x = 0;
                    Bar(x);
                }
            }
            """);

    [Fact]
    public Task TestAvailableInNonHiddenBlock2_TopLevelStatement()
        => TestAsync(
            """
            #line default

            int [||]x = 0;
            Goo();
            #line hidden
            Goo();
            #line default
            Goo();
            Bar(x);
            """,
            """
            #line default

            Goo();
            #line hidden
            Goo();
            #line default
            Goo();
            #line default

            int x = 0;
            Bar(x);
            """,
            new(TestOptions.Regular));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545435")]
    public Task TestWarnOnChangingScopes1()
        => TestInRegularAndScriptAsync(
            """
            using System.Linq;

            class Program
            {
                void Main()
                {
                    var [||]@lock = new object();
                    new[] { 1 }.AsParallel().ForAll((i) => {
                        lock (@lock)
                        {
                        }
                    });
                }
            }
            """,
            """
            using System.Linq;

            class Program
            {
                void Main()
                {
                    new[] { 1 }.AsParallel().ForAll((i) =>
                    {
                        {|Warning:var @lock = new object();|}
                        lock (@lock)
                        {
                        }
                    });
                }
            }
            """,
            new(title: FeaturesResources.Move_declaration_near_reference_may_change_semantics));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545435")]
    public Task TestWarnOnChangingScopes1_TopLevelStatement()
        => TestAsync(
            """
            using System.Linq;

            var [||]@lock = new object();
            new[] { 1 }.AsParallel().ForAll((i) => {
                lock (@lock)
                {
                }
            });
            """,
            """
            using System.Linq;

            new[] { 1 }.AsParallel().ForAll((i) =>
            {

                {|Warning:var @lock = new object();|}
                lock (@lock)
                {
                }
            });
            """,
            new(TestOptions.Regular));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545435")]
    public Task TestWarnOnChangingScopes2()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Linq;

            class Program
            {
                void Main()
                {
                    var [||]i = 0;
                    foreach (var v in new[] { 1 })
                    {
                        Console.Write(i);
                        i++;
                    }
                }
            }
            """,
            """
            using System;
            using System.Linq;

            class Program
            {
                void Main()
                {
                    foreach (var v in new[] { 1 })
                    {
                        {|Warning:var i = 0;|}
                        Console.Write(i);
                        i++;
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545435")]
    public Task TestWarnOnChangingScopes2_TopLevelStatement()
        => TestAsync(
            """
            using System;
            using System.Linq;

            var [||]i = 0;
            foreach (var v in new[] { 1 })
            {
                Console.Write(i);
                i++;
            }
            """,
            """
            using System;
            using System.Linq;

            foreach (var v in new[] { 1 })
            {

                {|Warning:var i = 0;|}
                Console.Write(i);
                i++;
            }
            """,
            new(TestOptions.Regular));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/44664")]
    public Task TestWarnOnChangingScopes3()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Linq;

            class Program
            {
                void Main()
                {
                    var [||]i = 0;
                    void LocalFunction()
                    {
                        Console.Write(i);
                        i++;
                    }
                }
            }
            """,
            """
            using System;
            using System.Linq;

            class Program
            {
                void Main()
                {
                    void LocalFunction()
                    {
                        {|Warning:var i = 0;|}
                        Console.Write(i);
                        i++;
                    }
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/44664")]
    public Task TestWarnOnChangingScopes3_TopLevelStatement()
        => TestAsync(
            """
            using System;
            using System.Linq;

            var [||]i = 0;
            void LocalFunction()
            {
                Console.Write(i);
                i++;
            }
            """,
            """
            using System;
            using System.Linq;

            void LocalFunction()
            {

                {|Warning:var i = 0;|}
                Console.Write(i);
                i++;
            }
            """,
            new(TestOptions.Regular));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545840")]
    public Task InsertCastIfNecessary1()
        => TestInRegularAndScriptAsync(
            """
            using System;

            static class C
            {
                static int Outer(Action<int> x, object y) { return 1; }
                static int Outer(Action<string> x, string y) { return 2; }

                static void Inner(int x, int[] y) { }
                unsafe static void Inner(string x, int*[] y) { }

                static void Main()
                {
                    var [||]a = Outer(x => Inner(x, null), null);
                    unsafe
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """,

            """
            using System;

            static class C
            {
                static int Outer(Action<int> x, object y) { return 1; }
                static int Outer(Action<string> x, string y) { return 2; }

                static void Inner(int x, int[] y) { }
                unsafe static void Inner(string x, int*[] y) { }

                static void Main()
                {
                    unsafe
                    {
                        var a = Outer(x => Inner(x, null), (object)null);
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545835")]
    public Task InsertCastIfNecessary2()
        => TestInRegularAndScriptAsync(
            """
            using System;

            class X
            {
                static int Goo(Func<int?, byte> x, object y) { return 1; }
                static int Goo(Func<X, byte> x, string y) { return 2; }

                const int Value = 1000;
                static void Main()
                {
                    var [||]a = Goo(X => (byte)X.Value, null);
                    unchecked
                    {
                        Console.WriteLine(a);
                    }
                }
            }
            """,

            """
            using System;

            class X
            {
                static int Goo(Func<int?, byte> x, object y) { return 1; }
                static int Goo(Func<X, byte> x, string y) { return 2; }

                const int Value = 1000;
                static void Main()
                {
                    unchecked
                    {
                        {|Warning:var a = Goo(X => (byte)X.Value, (object)null);|}
                        Console.WriteLine(a);
                    }
                }
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546267")]
    public Task MissingIfNotInDeclarationSpan()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    // Comment [||]about goo!
                    // Comment about goo!
                    // Comment about goo!
                    // Comment about goo!
                    // Comment about goo!
                    // Comment about goo!
                    // Comment about goo!
                    int goo;
                    Console.WriteLine();
                    Console.WriteLine(goo);
                }
            }
            """);

    [Fact]
    public Task Tuple()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    (int, string) [||]x;
                    {
                        Console.WriteLine(x);
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    {
                        (int, string) x;
                        Console.WriteLine(x);
                    }
                }
            }
            """);

    [Fact]
    public Task TupleWithNames()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    (int a, string b) [||]x;
                    {
                        Console.WriteLine(x);
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    {
                        (int a, string b) x;
                        Console.WriteLine(x);
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments01()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();

                    // leading trivia
                    int i = 5;
                    Console.Write(i);
                }
            }
            """);

    [Fact]
    public Task TestComments02()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    {
                        Console.Write(i);
                    }
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();

                    {
                        // leading trivia
                        int i = 5;
                        Console.Write(i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments03()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    // Existing trivia
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();

                    // leading trivia
                    int i = 5;
                    // Existing trivia
                    Console.Write(i);
                }
            }
            """);

    [Fact]
    public Task TestComments04()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    {
                        // Existing trivia
                        Console.Write(i);
                    }
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();

                    {
                        // leading trivia
                        int i = 5;
                        // Existing trivia
                        Console.Write(i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments05()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (false)
                    {
                    }

                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    i = 0;
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (false)
                    {
                    }

                    Console.WriteLine();

                    // leading trivia
                    int i = 0;
                    Console.Write(i);
                }
            }
            """);

    [Fact]
    public Task TestComments06()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (false)
                    {
                    }

                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    {
                        i = 0;
                        Console.Write(i);
                    }
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (false)
                    {
                    }

                    Console.WriteLine();

                    {
                        // leading trivia
                        int i = 0;
                        Console.Write(i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestComments07()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (false)
                    {
                    }

                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    // Existing trivia
                    i = 0;
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (false)
                    {
                    }

                    Console.WriteLine();

                    // leading trivia
                    // Existing trivia
                    int i = 0;
                    Console.Write(i);
                }
            }
            """);

    [Fact]
    public Task TestComments08()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (false)
                    {
                    }

                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    {
                        // Existing trivia
                        i = 0;
                        Console.Write(i);
                    }
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (false)
                    {
                    }

                    Console.WriteLine();

                    {
                        // leading trivia
                        // Existing trivia
                        int i = 0;
                        Console.Write(i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMergeComments01()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    i = 0;
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();

                    // leading trivia
                    int i = 0;
                    Console.Write(i);
                }
            }
            """);

    [Fact]
    public Task TestMergeComments02()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    {
                        i = 0;
                        Console.Write(i);
                    }
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();

                    {
                        // leading trivia
                        int i = 0;
                        Console.Write(i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMergeComments03()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    // Existing trivia
                    i = 0;
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();

                    // leading trivia
                    // Existing trivia
                    int i = 0;
                    Console.Write(i);
                }
            }
            """);

    [Fact]
    public Task TestMergeComments04()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    {
                        // Existing trivia
                        i = 0;
                        Console.Write(i);
                    }
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    Console.WriteLine();

                    {
                        // leading trivia
                        // Existing trivia
                        int i = 0;
                        Console.Write(i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMergeComments05()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                    {
                    }

                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    i = 0;
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                    {
                    }

                    Console.WriteLine();

                    // leading trivia
                    int i = 0;
                    Console.Write(i);
                }
            }
            """);

    [Fact]
    public Task TestMergeComments06()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                    {
                    }

                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    {
                        i = 0;
                        Console.Write(i);
                    }
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                    {
                    }

                    Console.WriteLine();

                    {
                        // leading trivia
                        int i = 0;
                        Console.Write(i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMergeComments07()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                    {
                    }

                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    // Existing trivia
                    i = 0;
                    Console.Write(i);
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                    {
                    }

                    Console.WriteLine();

                    // leading trivia
                    // Existing trivia
                    int i = 0;
                    Console.Write(i);
                }
            }
            """);

    [Fact]
    public Task TestMergeComments07_TopLevelStatement()
        => TestAsync(
            """
            if (true)
            {
            }

            // leading trivia
            int [||]i = 5;
            Console.WriteLine();

            // Existing trivia
            i = 0;
            Console.Write(i);
            """,
            """
            if (true)
            {
            }

            Console.WriteLine();

            // leading trivia
            // Existing trivia
            int i = 0;
            Console.Write(i);
            """,
            new(TestOptions.Regular));

    [Fact]
    public Task TestMergeComments08()
        => TestInRegularAndScriptAsync(
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                    {
                    }

                    // leading trivia
                    int [||]i = 5;
                    Console.WriteLine();

                    {
                        // Existing trivia
                        i = 0;
                        Console.Write(i);
                    }
                }
            }
            """,
            """
            class Program
            {
                static void Main(string[] args)
                {
                    if (true)
                    {
                    }

                    Console.WriteLine();

                    {
                        // leading trivia
                        // Existing trivia
                        int i = 0;
                        Console.Write(i);
                    }
                }
            }
            """);

    [Fact]
    public Task TestMergeComments08_TopLevelStatement()
        => TestAsync(
            """
            if (true)
            {
            }

            // leading trivia
            int [||]i = 5;
            Console.WriteLine();

            {
                // Existing trivia
                i = 0;
                Console.Write(i);
            }
            """,
            """
            if (true)
            {
            }

            Console.WriteLine();

            {
                // leading trivia
                // Existing trivia
                int i = 0;
                Console.Write(i);
            }
            """,
            new(TestOptions.Regular));

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public Task TestMissingOnCrossFunction1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
              static void Main(string[] args)
              {
                Method<string>();
              }

              public static void Method<T>()
              { 
                [|T t|];
                void Local<T>()
                {
                  Out(out t);
                  Console.WriteLine(t);
                }
                Local<int>();
              }

              public static void Out<T>(out T t) => t = default;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public Task TestMissingOnCrossFunction2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
              static void Main(string[] args)
              {
                Method<string>();
              }

              public static void Method<T>()
              { 
                void Local<T>()
                {
                    [|T t|];
                    void InnerLocal<T>()
                    {
                      Out(out t);
                      Console.WriteLine(t);
                    }
                }
                Local<int>();
              }

              public static void Out<T>(out T t) => t = default;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public Task TestMissingOnCrossFunction3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Method<string>();
                }

                public static void Method<T>()
                { 
                    [|T t|];
                    void Local<T>()
                    {
                        { // <-- note this set of added braces
                            Out(out t);
                            Console.WriteLine(t);
                        }
                    }
                    Local<int>();
                }

                public static void Out<T>(out T t) => t = default;
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
    public Task TestMissingOnCrossFunction4()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;

            class Program
            {
                static void Main(string[] args)
                {
                    Method<string>();
                }

                public static void Method<T>()
                {
                    { // <-- note this set of added braces
                        [|T t|];
                        void Local<T>()
                        {
                            { // <-- and my axe
                                Out(out t);
                                Console.WriteLine(t);
                            }
                        }
                        Local<int>();
                    }
                }

                public static void Out<T>(out T t) => t = default;
            }
            """);

    [Fact]
    public Task TestMoveInsideSwitchSection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case true:
                            int [||]x = 0;
                            System.Console.WriteLine();
                            System.Console.WriteLine(x);
                            break;
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case true:
                            System.Console.WriteLine();
                            int x = 0;
                            System.Console.WriteLine(x);
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestMoveIntoSwitchSection()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x;
                    switch (true)
                    {
                        case true:
                            x = 0;
                            break;
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    switch (true)
                    {
                        case true:
                            int x = 0;
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestMoveIntoSwitchSection_TopLevelStatement()
        => TestAsync(
            """
            int [||]x;
            switch (true)
            {
                case true:
                    x = 0;
                    break;
            }
            """,
            """
            switch (true)
            {
                case true:
                    int x = 0;
                    break;
            }
            """,
            new(TestOptions.Regular));

    [Fact]
    public Task TestUsedInMultipleSwitchSections_MoveToSwitchStatement()
        => TestInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x;
                    System.Console.WriteLine();
                    switch (true)
                    {
                        case true:
                            x = 0;
                            break;
                        case false:
                            x = 0;
                            break;
                    }
                }
            }
            """,
            """
            class C
            {
                void M()
                {
                    System.Console.WriteLine();
                    int x;
                    switch (true)
                    {
                        case true:
                            x = 0;
                            break;
                        case false:
                            x = 0;
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestUsedInMultipleSwitchSections_TopLevelStatement_MoveToSwitchStatement()
        => TestAsync(
            """
            int [||]x;
            System.Console.WriteLine();
            switch (true)
            {
                case true:
                    x = 0;
                    break;
                case false:
                    x = 0;
                    break;
            }
            """,
            """
            System.Console.WriteLine();
            int x;
            switch (true)
            {
                case true:
                    x = 0;
                    break;
                case false:
                    x = 0;
                    break;
            }
            """,
            new(TestOptions.Regular));

    [Fact]
    public Task TestUsedInMultipleSwitchSections_CannotMove()
        => TestMissingInRegularAndScriptAsync(
            """
            class C
            {
                void M()
                {
                    int [||]x;
                    switch (true)
                    {
                        case true:
                            x = 0;
                            break;
                        case false:
                            x = 0;
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestUsedInMultipleSwitchSections_TopLevelStatement_CannotMove()
        => TestMissingInRegularAndScriptAsync(
            """
            int [||]x;
            switch (true)
            {
                case true:
                    x = 0;
                    break;
                case false:
                    x = 0;
                    break;
            }
            """);
}
