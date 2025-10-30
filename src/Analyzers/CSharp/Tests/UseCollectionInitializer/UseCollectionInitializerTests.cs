// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseCollectionInitializer;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseCollectionInitializer;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionInitializerDiagnosticAnalyzer,
    CSharpUseCollectionInitializerCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionInitializer)]
public sealed partial class UseCollectionInitializerTests
{
    private static async Task TestInRegularAndScriptAsync(
        string testCode,
        string fixedCode,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        LanguageVersion languageVersion = LanguageVersion.CSharp11)
    {
        var test = new VerifyCS.Test
        {
            ReferenceAssemblies = Testing.ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = languageVersion,
            TestState = { OutputKind = outputKind },
        };

        await test.RunAsync();
    }

    private static async Task TestMissingInRegularAndScriptAsync(string testCode, LanguageVersion? languageVersion = null)
    {
        var test = new VerifyCS.Test
        {
            TestCode = testCode,
        };

        if (languageVersion != null)
            test.LanguageVersion = languageVersion.Value;

        await test.RunAsync();
    }

    [Fact]
    public Task TestOnVariableDeclarator()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = [|new|] List<int>();
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = new List<int>
                    {
                        1
                    };
                }
            }
            """);

    [Fact]
    public Task TestNotInField1()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                List<int> v = new List<int>();
            }
            """);

    [Fact]
    public Task TestNotInField2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                List<int> v = new List<int>() { 1, 2, 3 };
            }
            """);

    [Fact]
    public Task TestOnVariableDeclarator_AddRange()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    var c = [|new|] List<int>();
                    [|c.Add(|]1);
                    c.AddRange(x);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    var c = new List<int>
                    {
                        1
                    };
                    c.AddRange(x);
                }
            }
            """);

    [Fact]
    public Task TestOnVariableDeclarator_Foreach()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    var c = [|new|] List<int>();
                    [|c.Add(|]1);
                    foreach (var v in x)
                        c.Add(v);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    var c = new List<int>
                    {
                        1
                    };
                    foreach (var v in x)
                        c.Add(v);
                }
            }
            """);

    [Fact]
    public Task TestOnVariableDeclarator_If1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    var c = [|new|] List<int>();
                    [|c.Add(|]1);
                    if (b)
                        c.Add(2);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    var c = new List<int>
                    {
                        1
                    };
                    if (b)
                        c.Add(2);
                }
            }
            """);

    [Fact]
    public Task TestOnVariableDeclarator_If2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    var c = [|new|] List<int>();
                    [|c.Add(|]1);
                    if (b)
                        c.Add(2);
                    else
                        c.Add(3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    var c = new List<int>
                    {
                        1
                    };
                    if (b)
                        c.Add(2);
                    else
                        c.Add(3);
                }
            }
            """);

    [Fact]
    public Task TestOnVariableDeclarator_If3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    var c = [|new|] List<int>();
                    [|c.Add(|]1);
                    if (b)
                    {
                        c.Add(2);
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    var c = new List<int>
                    {
                        1
                    };
                    if (b)
                    {
                        c.Add(2);
                    }
                }
            }
            """);

    [Fact]
    public Task TestIndexAccess1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = [|new|] List<int>();
                    c[1] = 2;
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = new List<int>
                    {
                        [1] = 2
                    };
                }
            }
            """);

    [Fact]
    public Task TestIndexAccess1_NotInCSharp5()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = new List<int>();
                    c[1] = 2;
                }
            }
            """, LanguageVersion.CSharp5);

    [Fact]
    public Task TestComplexIndexAccess1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class A
            {
                public B b;
            }

            class B
            {
                public List<int> c;
            }

            class C
            {
                void M(A a)
                {
                    a.b.c = [|new|] List<int>();
                    a.b.c[1] = 2;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class A
            {
                public B b;
            }

            class B
            {
                public List<int> c;
            }

            class C
            {
                void M(A a)
                {
                    a.b.c = new List<int>
                    {
                        [1] = 2
                    };
                }
            }
            """);

    [Fact]
    public Task TestIndexAccess2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = [|new|] List<object>();
                    c[1] = 2;
                    c[2] = "";
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = new List<object>
                    {
                        [1] = 2,
                        [2] = ""
                    };
                }
            }
            """);

    [Fact]
    public Task TestIndexAccess3()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections;

            class C
            {
                void M()
                {
                    var c = [|new|] X();
                    c[1] = 2;
                    c[2] = "";
                    c[3, 4] = 5;
                }
            }

            class X : IEnumerable
            {
                public object this[int i] { get => null; set { } }
                public object this[int i, int j] { get => null; set { } }

                public IEnumerator GetEnumerator() => null;
                public void Add(int i) { }
            }
            """,
            """
            using System.Collections;

            class C
            {
                void M()
                {
                    var c = new X
                    {
                        [1] = 2,
                        [2] = "",
                        [3, 4] = 5
                    };
                }
            }

            class X : IEnumerable
            {
                public object this[int i] { get => null; set { } }
                public object this[int i, int j] { get => null; set { } }

                public IEnumerator GetEnumerator() => null;
                public void Add(int i) { }
            }
            """);

    [Fact]
    public Task TestIndexFollowedByInvocation()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = [|new|] List<int>();
                    c[1] = 2;
                    c.Add(0);
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = new List<int>
                    {
                        [1] = 2
                    };
                    c.Add(0);
                }
            }
            """);

    [Fact]
    public Task TestInvocationFollowedByIndex()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = [|new|] List<int>();
                    [|c.Add(|]0);
                    c[1] = 2;
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = new List<int>
                    {
                        0
                    };
                    c[1] = 2;
                }
            }
            """);

    [Fact]
    public Task TestWithInterimStatement()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = [|new|] List<int>();
                    [|c.Add(|]1);
                    [|c.Add(|]2);
                    throw new System.Exception();
                    c.Add(3);
                    c.Add(4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = new List<int>
                    {
                        1,
                        2
                    };
                    throw new System.Exception();
                    c.Add(3);
                    c.Add(4);
                }
            }
            """);

    [Fact]
    public Task TestMissingBeforeCSharp3()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = new List<int>();
                    c.Add(1);
                }
            }
            """, LanguageVersion.CSharp2);

    [Fact]
    public Task TestMissingOnNonIEnumerable()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = new C();
                    c.Add(1);
                }

                void Add(int i) { }
            }
            """);

    [Fact]
    public Task TestMissingOnNonIEnumerableEvenWithAdd()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = new C();
                    c.Add(1);
                }

                public void Add(int i)
                {
                }
            }
            """);

    [Fact]
    public Task TestWithCreationArguments()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = [|new|] List<int>(1);
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = new List<int>(1)
                    {
                        1
                    };
                }
            }
            """);

    [Fact]
    public Task TestOnAssignmentExpression()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = null;
                    c = [|new|] List<int>();
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = null;
                    c = new List<int>
                    {
                        1
                    };
                }
            }
            """);

    [Fact]
    public Task TestMissingOnRefAdd()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int i)
                {
                    var c = new List();
                    c.Add(ref i);
                }
            }


            class List
            {
                public void Add(ref int i)
                {
                }
            }
            """);

    [Fact]
    public Task TestComplexInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(List<int>[] array)
                {
                    array[0] = [|new|] List<int>();
                    [|array[0].Add(|]1);
                    [|array[0].Add(|]2);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(List<int>[] array)
                {
                    array[0] = new List<int>
                    {
                        1,
                        2
                    };
                }
            }
            """);

    [Fact]
    public Task TestNotOnNamedArg()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = new List<int>();
                    c.Add(item: 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializer()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = [|new|] List<int>()
                    {
                        1
                    };
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = [|new|] List<int>
                    {
                        1,
                        1
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializerWithComma()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = [|new|] List<int>()
                    {
                        1,
                    };
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = [|new|] List<int>
                    {
                        1,
                        1
                    };
                }
            }
            """);

    [Fact]
    public Task TestFixAllInDocument1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(List<int>[] array)
                {
                    array[0] = [|new|] List<int>();
                    [|array[0].Add(|]1);
                    [|array[0].Add(|]2);
                    array[1] = [|new|] List<int>();
                    [|array[1].Add(|]3);
                    [|array[1].Add(|]4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(List<int>[] array)
                {
                    array[0] = new List<int>
                    {
                        1,
                        2
                    };
                    array[1] = new List<int>
                    {
                        3,
                        4
                    };
                }
            }
            """);

    [Fact]
    public Task TestFixAllInDocument2()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list1 = [|new|] Bar(() => {
                        var list2 = [|new|] List<int>();
                        [|list2.Add(|]2);
                    });
                    [|list1.Add(|]1);
                }
            }

            class Bar : IEnumerable
            {
                public Bar(Action action) { }

                public IEnumerator GetEnumerator() => null;
                public void Add(int i) { }
            }
            """,
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list1 = new Bar(() =>
                    {
                        var list2 = new List<int>
                        {
                            2
                        };
                    })
                    {
                        1
                    };
                }
            }

            class Bar : IEnumerable
            {
                public Bar(Action action) { }

                public IEnumerator GetEnumerator() => null;
                public void Add(int i) { }
            }
            """);

    [Fact]
    public Task TestFixAllInDocument3()
        => new VerifyCS.Test
        {
            TestCode =
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list1 = [|new|] List<Action>();
                    [|list1.Add(|]() => {
                        var list2 = [|new|] List<int>();
                        [|list2.Add(|]2);
                    });
                }
            }
            """,
            FixedCode =
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list1 = new List<Action>
                    {
                        () =>
                        {
                            var list2 = new List<int> { 2 };
                        }
                    };
                }
            }
            """,
            BatchFixedCode =
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list1 = new List<Action>
                    {
                        () =>
                        {
                            var list2 = new List<int>
                            {
                                2
                            };
                        }
                    };
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = [|new|] List<int>();
                    [|c.Add(|]1); // Goo
                    [|c.Add(|]2); // Bar
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = new List<int>
                    {
                        1, // Goo
                        2 // Bar
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46670")]
    public Task TestTriviaRemoveLeadingBlankLinesForFirstElement()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = [|new|] List<int>();

                    // Goo
                    [|c.Add(|]1);

                    // Bar
                    [|c.Add(|]2);
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    var c = new List<int>
                    {
                        // Goo
                        1,

                        // Bar
                        2
                    };
                }
            }
            """);

    [Fact]
    public Task TestComplexInitializer2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = [|new|] Dictionary<int, string>();
                    [|c.Add(|]1, "x");
                    [|c.Add(|]2, "y");
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = new Dictionary<int, string>
                    {
                        { 1, "x" },
                        { 2, "y" }
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16158")]
    public Task TestIncorrectAddName()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public static void Bar()
                {
                    string item = null;
                    var items = new List<string>();

                    var values = [|new|] List<string>(); // Collection initialization can be simplified
                    [|values.Add(|]item);
                    values.AddRange(items);
                }
            }
            """,
            """
            using System.Collections.Generic;

            public class Goo
            {
                public static void Bar()
                {
                    string item = null;
                    var items = new List<string>();

                    var values = new List<string>
                    {
                        item
                    }; // Collection initialization can be simplified
                    values.AddRange(items);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16241")]
    public Task TestNestedCollectionInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
                    using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    var myStringArray = new string[] { "Test", "123", "ABC" };
                    var myStringList = myStringArray?.ToList() ?? new List<string>();
                    myStringList.Add("Done");
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17823")]
    public Task TestMissingWhenReferencedInInitializer()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var items = new List<object>();
                    items[0] = items[0];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17823")]
    public Task TestWhenReferencedInInitializer_LocalVar()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var items = [|new|] List<object>();
                    items[0] = 1;
                    items[1] = items[0];
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    var items = [|new|] List<object>
                    {
                        [0] = 1
                    };
                    items[1] = items[0];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17823")]
    public Task TestWhenReferencedInInitializer_LocalVar2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    var t = new List<int>(new int[] { 1, 2, 3 });
                    t.Add(t.Min() - 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18260")]
    public Task TestWhenReferencedInInitializer_Assignment()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = null;
                    items = [|new|] List<object>();
                    items[0] = 1;
                    items[1] = items[0];
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = null;
                    items = [|new|] List<object>
                    {
                        [0] = 1
                    };
                    items[1] = items[0];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18260")]
    public Task TestWhenReferencedInInitializer_Assignment2()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    List<int> t = null;
                    t = new List<int>(new int[] { 1, 2, 3 });
                    t.Add(t.Min() - 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18260")]
    public Task TestFieldReference()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                private List<int> myField;
                void M()
                {
                    myField = new List<int>();
                    myField.Add(this.myField.Count);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17853")]
    public Task TestMissingForDynamic()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Dynamic;

            class C
            {
                void Goo()
                {
                    dynamic body = new ExpandoObject();
                    body[0] = new ExpandoObject();
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17953")]
    public Task TestMissingAcrossPreprocessorDirective()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
                    var items = new List<object>();
            #if true
                    items.Add(1);
            #endif
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17953")]
    public Task TestAvailableInsidePreprocessorDirective()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
            #if true
                    var items = [|new|] List<object>();
                    [|items.Add(|]1);
            #endif
                }
            }
            """,
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
            #if true
                    var items = new List<object>
                    {
                        1
                    };
            #endif
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18242")]
    public Task TestObjectInitializerAssignmentAmbiguity()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
                    int lastItem;
                    var list = [|new|] List<int>();
                    [|list.Add(|]lastItem = 5);
                }
            }
            """,
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
                    int lastItem;
                    var list = new List<int>
                    {
                        (lastItem = 5)
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18242")]
    public Task TestObjectInitializerCompoundAssignment()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
                    int lastItem = 0;
                    var list = [|new|] List<int>();
                    [|list.Add(|]lastItem += 5);
                }
            }
            """,
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
                    int lastItem = 0;
                    var list = new List<int>
                    {
                        (lastItem += 5)
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19253")]
    public Task TestKeepBlankLinesAfter()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class MyClass
            {
                public void Main()
                {
                    var list = [|new|] List<int>();
                    [|list.Add(|]1);

                    int horse = 1;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class MyClass
            {
                public void Main()
                {
                    var list = new List<int>
                    {
                        1
                    };

                    int horse = 1;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23672")]
    public Task TestMissingWithExplicitImplementedAddMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Dynamic;

            public class Goo
            {
                public void M()
                {
                    IDictionary<string, object> obj = new ExpandoObject();
                    obj.Add("string", "v");
                    obj.Add("int", 1);
                    obj.Add(" object", new { X = 1, Y = 2 });
                    }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47632")]
    public Task TestWhenReferencedInInitializerLeft()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = [|new|] List<object>();
                    items[0] = 1;
                    items[items.Count - 1] = 2;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = [|new|] List<object>
                    {
                        [0] = 1
                    };
                    items[items.Count - 1] = 2;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47632")]
    public Task TestWithIndexerInInitializerLeft()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = [|new|] List<object>();
                    items[0] = 1;
                    items[^1] = 2;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = new List<object>
                    {
                        [0] = 1
                    };
                    items[^1] = 2;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47632")]
    public Task TestWithImplicitObjectCreation()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = [|new|]();
                    items[0] = 1;
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = new()
                    {
                        [0] = 1
                    };
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61066")]
    public Task TestInTopLevelStatements()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            var list = [|new|] List<int>();
            [|list.Add(|]1);
            """,
            """
            using System.Collections.Generic;

            var list = new List<int>
            {
                1
            };

            """, OutputKind.ConsoleApplication);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71245")]
    public Task TestCollectionExpressionArgument1()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            var list = [|new|] List<object[]>();
            [|list.Add(|][]);
            """,
            """
            using System.Collections.Generic;

            var list = new List<object[]>
            {
                ([])
            };

            """, OutputKind.ConsoleApplication, LanguageVersion.CSharp12);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71245")]
    public Task TestCollectionExpressionArgument2()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = [|new|] List<object[]>();
                    [|list.Add(|][]);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var list = new List<object[]>
                    {
                        ([])
                    };
                }
            }
            """, languageVersion: LanguageVersion.CSharp12);

    [Fact]
    public Task TestDictionaryInitializerAmbiguity1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        var v = [|new|] List<int[]>();
                        [|v.Add(|][1, 2, 3]);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        var v = new List<int[]>
                        {
                            ([1, 2, 3])
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact]
    public Task TestDictionaryInitializerAmbiguity2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        var v = [|new|] List<int[]>();
                        // Leading
                        [|v.Add(|][1, 2, 3]);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        var v = new List<int[]>
                        {
                            // Leading
                            ([1, 2, 3])
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75214")]
    public Task TestComplexForeach()
        => new VerifyCS.Test
        {
            TestCode = """
                #nullable enable

                using System.Collections.Generic;
                using System.Linq;

                class C
                {
                    void M(List<int>? list1)
                    {
                        foreach (var (value, sort) in (list1 ?? [|new|] List<int>()).Select((val, i) => (val, i)))
                        {
                        }
                    }
                }
                """,
            FixedCode = """
                #nullable enable
                
                using System.Collections.Generic;
                using System.Linq;
                
                class C
                {
                    void M(List<int>? list1)
                    {
                        foreach (var (value, sort) in (list1 ?? []).Select((val, i) => (val, i)))
                        {
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77416")]
    public Task TestNoCollectionExpressionForBlockingCollection()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Concurrent;

                class A
                {
                    public void Main(ConcurrentQueue<int> queue)
                    {
                        BlockingCollection<int> bc = [|new|](queue);
                        [|bc.Add(|]42);
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Concurrent;

                class A
                {
                    public void Main(ConcurrentQueue<int> queue)
                    {
                        BlockingCollection<int> bc = new(queue)
                        {
                            42
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80862")]
    public Task TestDoNotOfferForUsingDeclaration()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            public class DisposableCollection : IEnumerable, IDisposable
            {
                private readonly List<object> _items = new List<object>();
                
                public void Add(object item) => _items.Add(item);
                
                public IEnumerator GetEnumerator() => _items.GetEnumerator();
                
                public void Dispose() { }
            }

            class C
            {
                void M()
                {
                    using var collection = new DisposableCollection();
                    collection.Add(1);
                    collection.Add(2);
                }
            }
            """);
}
