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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MoveDeclarationNearReference
{
    [Trait(Traits.Feature, Traits.Features.CodeActionsMoveDeclarationNearReference)]
    public class MoveDeclarationNearReferenceTests : AbstractCSharpCodeActionTest_NoEditor
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
            => new CSharpMoveDeclarationNearReferenceCodeRefactoringProvider();

        [Fact]
        public async Task TestMove1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMove1_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact]
        public async Task TestMove2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMove2_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact]
        public async Task TestMove3()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMove3_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact]
        public async Task TestMove4()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMove4_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact]
        public async Task TestAssign1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestAssign2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestAssign3()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissing1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissing1_TopLevelStatement()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                int [||]x;
                Console.WriteLine(x);
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538424")]
        public async Task TestMissingWhenReferencedInDeclaration()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingWhenInDeclarationGroup()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingWhenInDeclarationGroup_TopLevelStatement()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                int [||]i = 5;
                int j = 10;
                Console.WriteLine(i);
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/541475")]
        public async Task Regression8190()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestFormatting()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestFormatting_TopLevelStatement()
        {
            await TestAsync(
                """
                int [||]i = 5; Console.WriteLine();
                Console.Write(i);
                """,
                """
                Console.WriteLine();
                int i = 5; Console.Write(i);
                """,
                TestOptions.Regular);
        }

        [Fact]
        public async Task TestMissingInHiddenBlock1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingInHiddenBlock2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMissingInHiddenBlock2_TopLevelStatement()
        {
            await TestMissingInRegularAndScriptAsync(
                """
                #line default

                int [|x|] = 0;
                Goo();
                #line hidden
                Goo();
                #line default
                Bar(x);
                """);
        }

        [Fact]
        public async Task TestAvailableInNonHiddenBlock1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestAvailableInNonHiddenBlock2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestAvailableInNonHiddenBlock2_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545435")]
        public async Task TestWarnOnChangingScopes1()
        {
            await TestInRegularAndScriptAsync(
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
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545435")]
        public async Task TestWarnOnChangingScopes1_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545435")]
        public async Task TestWarnOnChangingScopes2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545435")]
        public async Task TestWarnOnChangingScopes2_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/44664")]
        public async Task TestWarnOnChangingScopes3()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/pull/44664")]
        public async Task TestWarnOnChangingScopes3_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545840")]
        public async Task InsertCastIfNecessary1()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545835")]
        public async Task InsertCastIfNecessary2()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546267")]
        public async Task MissingIfNotInDeclarationSpan()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task Tuple()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TupleWithNames()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestComments01()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestComments02()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestComments03()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestComments04()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestComments05()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestComments06()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestComments07()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestComments08()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMergeComments01()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMergeComments02()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMergeComments03()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMergeComments04()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMergeComments05()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMergeComments06()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMergeComments07()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMergeComments07_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact]
        public async Task TestMergeComments08()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMergeComments08_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
        public async Task TestMissingOnCrossFunction1()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
        public async Task TestMissingOnCrossFunction2()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
        public async Task TestMissingOnCrossFunction3()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/21907")]
        public async Task TestMissingOnCrossFunction4()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMoveInsideSwitchSection()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMoveIntoSwitchSection()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestMoveIntoSwitchSection_TopLevelStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact]
        public async Task TestUsedInMultipleSwitchSections_MoveToSwitchStatement()
        {
            await TestInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestUsedInMultipleSwitchSections_TopLevelStatement_MoveToSwitchStatement()
        {
            await TestAsync(
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
                TestOptions.Regular);
        }

        [Fact]
        public async Task TestUsedInMultipleSwitchSections_CannotMove()
        {
            await TestMissingInRegularAndScriptAsync(
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
        }

        [Fact]
        public async Task TestUsedInMultipleSwitchSections_TopLevelStatement_CannotMove()
        {
            await TestMissingInRegularAndScriptAsync(
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
    }
}
