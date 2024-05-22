// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq.Expressions;
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
public partial class UseCollectionInitializerTests_CollectionExpression
{
    private static async Task TestInRegularAndScriptAsync(string testCode, string fixedCode, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        await new VerifyCS.Test
        {
            ReferenceAssemblies = Testing.ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState = { OutputKind = outputKind }
        }.RunAsync();
    }

    private static Task TestMissingInRegularAndScriptAsync(string testCode)
        => TestInRegularAndScriptAsync(testCode, testCode);

    [Fact]
    public async Task TestNotOnVarVariableDeclarator()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestNotWithConstructorArguments1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = new List<int>(new[] { 1, 2, 3 });
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithConstructorArguments2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    var c = [|new|] List<int>(new[] { 1, 2, 3 });
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
                    var c = new List<int>(new[] { 1, 2, 3 })
                    {
                        1
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task TestInField1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [|new|] List<int>();
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [];
            }
            """);
    }

    [Fact]
    public async Task TestInField2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [|new|] List<int>() { 1, 2, 3 };
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [1, 2, 3];
            }
            """);
    }

    [Fact]
    public async Task TestInField3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [|new|] List<int>()
                {
                    1,
                    2,
                    3
                };
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c =
                [
                    1,
                    2,
                    3
                ];
            }
            """);
    }

    [Fact]
    public async Task TestInField4()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = new List<int>(new[] { 1, 2, 3 });
            }
            """);
    }

    [Fact]
    public async Task TestInField5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [|new|] List<int> { };
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [];
            }
            """);
    }

    [Fact]
    public async Task TestInField6()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [|new|] List<int> { 1, 2, 3 };
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [1, 2, 3];
            }
            """);
    }

    [Fact]
    public async Task TestInField7()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [|new|] List<int>
                {
                    1,
                    2,
                    3
                };
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                List<int> c =
                [
                    1,
                    2,
                    3
                ];
            }
            """);
    }

    [Fact]
    public async Task TestInArgument1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    X([|new|] List<int>());
                }

                void X(List<int> list) { }
            }
            """,
            """
            using System.Collections.Generic;
            
            class C
            {
                void M()
                {
                    X([]);
                }
            
                void X(List<int> list) { }
            }
            """);
    }

    [Fact]
    public async Task TestInArgument2_InterfacesOn()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    X([|new|] List<int>());
                }

                void X(IEnumerable<int> list) { }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    X([]);
                }

                void X(IEnumerable<int> list) { }
            }
            """);
    }

    [Fact]
    public async Task TestInArgument2_InterfacesOff()
    {
        await new VerifyCS.Test
        {
            ReferenceAssemblies = Testing.ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        X(new List<int>());
                    }

                    void X(IEnumerable<int> list) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestOnVariableDeclarator()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
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
                    List<int> c = [1];
                }
            }
            """);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/70172"), WorkItem("https://github.com/dotnet/roslyn/issues/69277")]
    public async Task TestOnVariableDeclarator_If1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    List<int> c = [|new|] List<int>();
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
                    List<int> c = [1, .. {|CS0173:b ? [2] : []|}];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_If2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    List<int> c = [|new|] List<int>();
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
                    List<int> c = [1, b ? 2 : 3];
                }
            }
            """);
    }

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/70172"), WorkItem("https://github.com/dotnet/roslyn/issues/69277")]
    public async Task TestOnVariableDeclarator_If3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    List<int> c = [|new|] List<int>();
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
                    List<int> c = [1, .. {|CS0173:b ? [2] : []|}];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_If4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    if (b)
                    {
                        c.Add(2);
                    }
                    else
                    {
                        c.Add(3);
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
                    List<int> c = [1, b ? 2 : 3];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_If5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    if (b)
                    {
                        c.Add(2);
                        c.Add(3);
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
                    List<int> c = [1];
                    if (b)
                    {
                        c.Add(2);
                        c.Add(3);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_If6()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    if (b)
                    {
                        c.Add(2);
                    }
                    else
                    {
                        c.Add(3);
                        c.Add(4);
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
                    List<int> c = [1];
                    if (b)
                    {
                        c.Add(2);
                    }
                    else
                    {
                        c.Add(3);
                        c.Add(4);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_If7()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    if (b)
                    {
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
                    List<int> c = [1];
                    if (b)
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_If8()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(bool b)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    if (b)
                    {
                        c.Add(2);
                    }
                    else
                    {
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
                    List<int> c = [1];
                    if (b)
                    {
                        c.Add(2);
                    }
                    else
                    {
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclaratorDifferentType_InterfaceOn()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    IList<int> c = [|new|] List<int>();
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
                    IList<int> c = [1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclaratorDifferentType_InterfaceOff()
    {
        await new VerifyCS.Test
        {
            ReferenceAssemblies = Testing.ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        IList<int> c = [|new|] List<int>();
                        [|c.Add(|]1);
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        IList<int> c = new List<int>
                        {
                            1
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();
    }

    [Fact]
    public async Task TestOnVariableDeclarator_Foreach1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    [|foreach (var v in |]x)
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
                    List<int> c = [1, .. x];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_Foreach1_A()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    [|foreach (var v in |]x)
                    {
                        c.Add(v);
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [1, .. x];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_Foreach1_B()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    foreach (var v in x)
                    {
                        c.Add(0);
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [1];
                    foreach (var v in x)
                    {
                        c.Add(0);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_Foreach1_C()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int z)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    foreach (var v in x)
                    {
                        c.Add(z);
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int z)
                {
                    List<int> c = [1];
                    foreach (var v in x)
                    {
                        c.Add(z);
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_Foreach2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    [|foreach (var v in |]x)
                        c.Add(v);
                    [|foreach (var v in |]y)
                        c.Add(v);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [1, .. x, .. y];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_Foreach3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>();
                    [|foreach (var v in |]x)
                        c.Add(v);
                    [|c.Add(|]1);
                    [|foreach (var v in |]y)
                        c.Add(v);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [.. x, 1, .. y];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_Foreach4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>();
                    [|foreach (var v in |]x)
                        c.Add(v);
                    [|foreach (var v in |]y)
                        c.Add(v);
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [.. x, .. y, 1];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70388")]
    public async Task TestOnVariableDeclarator_AwaitForeach1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                async void M(IAsyncEnumerable<int> x)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    await foreach (var v in x)
                        c.Add(v);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                async void M(IAsyncEnumerable<int> x)
                {
                    List<int> c = [1];
                    await foreach (var v in x)
                        c.Add(v);
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_AddRange1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    [|c.AddRange(|]x);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [1, .. x];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnVariableDeclarator_AddRangeAndForeach1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                    [|foreach (var v in |]x)
                        c.Add(v);
                    [|c.AddRange(|]y);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [1, .. x, .. y];
                }
            }
            """);
    }

    [Fact]
    public async Task TestIndexAccess1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
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
                    List<int> c = new List<int>
                    {
                        [1] = 2
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task TestIndexAccess1_Foreach()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>();
                    c[1] = 2;
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
                    List<int> c = new List<int>
                    {
                        [1] = 2
                    };
                    foreach (var v in x)
                        c.Add(v);
                }
            }
            """);
    }

    [Fact]
    public async Task TestComplexIndexAccess1()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact]
    public async Task TestIndexAccess2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    List<object> c = [|new|] List<object>();
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
                    List<object> c = new List<object>
                    {
                        [1] = 2,
                        [2] = ""
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task TestIndexAccess3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections;

            class C
            {
                void M()
                {
                    X c = [|new|] X();
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
                    X c = new X
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
    }

    [Fact]
    public async Task TestIndexFollowedByInvocation()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
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
                    List<int> c = new List<int>
                    {
                        [1] = 2
                    };
                    c.Add(0);
                }
            }
            """);
    }

    [Fact]
    public async Task TestInvocationFollowedByIndex()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
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
                    List<int> c = [0];
                    c[1] = 2;
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithInterimStatement()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
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
                    List<int> c = [1, 2];
                    throw new System.Exception();
                    c.Add(3);
                    c.Add(4);
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnNonIEnumerable()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    C c = new C();
                    c.Add(1);
                }

                void Add(int i) { }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnNonIEnumerableEvenWithAdd()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    C c = new C();
                    c.Add(1);
                }

                public void Add(int i)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestWithCreationArguments()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>(1);
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
                    List<int> c = [1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnAssignmentExpression()
    {
        await TestInRegularAndScriptAsync(
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
                    c = [1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestMissingOnRefAdd()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int i)
                {
                    List c = new List();
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
    }

    [Fact]
    public async Task TestComplexInitializer()
    {
        await TestInRegularAndScriptAsync(
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
                    array[0] = [1, 2];
                }
            }
            """);
    }

    [Fact]
    public async Task TestOnNamedArg()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
                    c.Add(item: 1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [];
                    c.Add(item: 1);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public async Task TestWithExistingInitializer()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    {
                        1
                    };
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
                    List<int> c =
                    [
                        1, 2
                    ];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public async Task TestWithExistingInitializer_NoParens()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>
                    {
                        1
                    };
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
                    List<int> c =
                    [
                        1, 2
                    ];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public async Task TestWithExistingInitializerWithComma()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    {
                        1,
                    };
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
                    List<int> c =
                    [
                        1, 2,
                    ];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public async Task TestWithExistingInitializer2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>()
                    {
                        1
                    };
                    [|foreach (var y in |]x)
                        c.Add(y);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c =
                    [
                        1, .. x
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestFixAllInDocument1()
    {
        await TestInRegularAndScriptAsync(
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
                    array[0] = [1, 2];
                    array[1] = [3, 4];
                }
            }
            """);
    }

    [Fact]
    public async Task TestFixAllInDocument2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    Bar list1 = [|new|] Bar(() => {
                        List<int> list2 = [|new|] List<int>();
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
                    Bar list1 = new Bar(() =>
                    {
                        List<int> list2 = [2];
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
    }

    [Fact]
    public async Task TestFixAllInDocument3()
    {
        await new VerifyCS.Test
        {
            TestCode =
            """
            using System;
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<Action> list1 = [|new|] List<Action>();
                    [|list1.Add(|]() => {
                        List<int> list2 = [|new|] List<int>();
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
                    List<Action> list1 =
                    [
                        () => {
                            List<int> list2 = [2];
                        },
                    ];
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
                    List<Action> list1 =
                    [
                        () => {
                            List<int> list2 = [2];
                        },
                    ];
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTrivia1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
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
                    List<int> c =
                    [
                        1, // Goo
                        2, // Bar
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestTrivia2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>();
                    // Goo
                    [|foreach (var v in |]x)
                        c.Add(v);

                    // Bar
                    [|foreach (var v in |]y)
                        c.Add(v);
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c =
                    [
                        // Goo
                        .. x,
                        // Bar
                        .. y,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestTrivia3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>();
                    // Goo
                    // Bar
                    [|foreach (var v in |]x)
                        c.Add(v);

                    // Baz
                    // Quux
                    [|foreach (var v in |]y)
                        c.Add(v);
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c =
                    [
                        // Goo
                        // Bar
                        .. x,
                        // Baz
                        // Quux
                        .. y,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestTrivia4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M(bool b1, bool b2)
                {
                    List<int> c = [|new|] List<int>();
                    // Goo
                    if (b1)
                        c.Add(0);
                    else
                        c.Add(1);

                    // Bar
                    if (b2)
                    {
                        c.Add(2);
                    }
                    else
                    {
                        c.Add(3);
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M(bool b1, bool b2)
                {
                    List<int> c =
                    [
                        // Goo
                        b1 ? 0 : 1,
                        // Bar
                        b2 ? 2 : 3,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestTrivia5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M(bool b1, bool b2)
                {
                    List<int> c = [|new|] List<int>();
                    // Goo
                    // Bar
                    if (b1)
                        c.Add(0);
                    else
                        c.Add(1);
            
                    // Baz
                    // Quux
                    if (b2)
                    {
                        c.Add(2);
                    }
                    else
                    {
                        c.Add(3);
                    }
                }
            }
            """,
            """
            using System.Collections.Generic;
            class C
            {
                void M(bool b1, bool b2)
                {
                    List<int> c =
                    [
                        // Goo
                        // Bar
                        b1 ? 0 : 1,
                        // Baz
                        // Quux
                        b2 ? 2 : 3,
                    ];
                }
            }
            """);
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/46670")]
    public async Task TestTriviaRemoveLeadingBlankLinesForFirstElement()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();

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
                    List<int> c =
                    [
                        // Goo
                        1,
                        // Bar
                        2,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestComplexInitializer2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    Dictionary<int, string> c = [|new|] Dictionary<int, string>();
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
                    Dictionary<int, string> c = new Dictionary<int, string>
                    {
                        { 1, "x" },
                        { 2, "y" }
                    };
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16158")]
    public async Task TestIncorrectAddName()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public static void Bar()
                {
                    string item = null;
                    var items = new List<string>();

                    List<string> values = [|new|] List<string>(); // Collection initialization can be simplified
                    [|values.Add(|]item);
                    values.Remove(item);
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

                    List<string> values = [item]; // Collection initialization can be simplified
                    values.Remove(item);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16241")]
    public async Task TestNestedCollectionInitializer()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    string[] myStringArray = new string[] { "Test", "123", "ABC" };
                    List<string> myStringList = myStringArray?.ToList() ?? [|new|] List<string>();
                    myStringList.Add("Done");
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class Program
            {
                static void Main(string[] args)
                {
                    string[] myStringArray = new string[] { "Test", "123", "ABC" };
                    List<string> myStringList = myStringArray?.ToList() ?? [];
                    myStringList.Add("Done");
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17823")]
    public async Task TestWhenReferencedInInitializer()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = [|new|] List<object>();
                    items[0] = items[0];
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = [];
                    items[0] = items[0];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17823")]
    public async Task TestWhenReferencedInInitializer_LocalVar()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                static void M()
                {
                    List<object> items = [|new|] List<object>();
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
                    List<object> items = [|new|] List<object>
                    {
                        [0] = 1
                    };
                    items[1] = items[0];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17823")]
    public async Task TestWhenReferencedInInitializer_LocalVar2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    List<int> t = new List<int>(new int[] { 1, 2, 3 });
                    t.Add(t.Min() - 1);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18260")]
    public async Task TestWhenReferencedInInitializer_Assignment()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18260")]
    public async Task TestWhenReferencedInInitializer_Assignment2()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18260")]
    public async Task TestFieldReference()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                private List<int> myField;
                void M()
                {
                    myField = [|new|] List<int>();
                    myField.Add(this.myField.Count);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                private List<int> myField;
                void M()
                {
                    myField = [];
                    myField.Add(this.myField.Count);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17853")]
    public async Task TestMissingForDynamic()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17953")]
    public async Task TestMissingAcrossPreprocessorDirective()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
                    List<object> items = new List<object>();
            #if true
                    items.Add(1);
            #endif
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17953")]
    public async Task TestAvailableInsidePreprocessorDirective()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
            #if true
                    List<object> items = [|new|] List<object>();
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
                    List<object> items = [1];
            #endif
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18242")]
    public async Task TestObjectInitializerAssignmentAmbiguity()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
                    int lastItem;
                    List<int> list = [|new|] List<int>();
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
                    List<int> list = [lastItem = 5];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18242")]
    public async Task TestObjectInitializerCompoundAssignment()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            public class Goo
            {
                public void M()
                {
                    int lastItem = 0;
                    List<int> list = [|new|] List<int>();
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
                    List<int> list = [lastItem += 5];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19253")]
    public async Task TestKeepBlankLinesAfter()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class MyClass
            {
                public void Main()
                {
                    List<int> list = [|new|] List<int>();
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
                    List<int> list = [1];

                    int horse = 1;
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/23672")]
    public async Task TestMissingWithExplicitImplementedAddMethod()
    {
        await TestMissingInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47632")]
    public async Task TestWhenReferencedInInitializerLeft()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47632")]
    public async Task TestWithIndexerInInitializerLeft()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/47632")]
    public async Task TestWithImplicitObjectCreation()
    {
        await TestInRegularAndScriptAsync(
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
    }

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61066")]
    public async Task TestInTopLevelStatements()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            List<int> list = [|new|] List<int>();
            [|list.Add(|]1);
            """,
            """
            using System.Collections.Generic;

            List<int> list = [1];

            """, OutputKind.ConsoleApplication);
    }

    [Fact]
    public async Task TestUpdateExistingCollectionInitializerToExpression1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestUpdateExistingCollectionInitializerToExpression2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    {
                        1
                    };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                    [
                        1
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestUpdateExistingCollectionInitializerToExpression3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    {
                        1,
                    };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                    [
                        1,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestUpdateExistingCollectionInitializerToExpression4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    {
                        1,
                        2
                    };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                    [
                        1,
                        2
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestUpdateExistingCollectionInitializerToExpression5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    {
                        1,
                        2,
                    };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                    [
                        1,
                        2,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NoElements_ExistingInitializer1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() { };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NoElements_ExistingInitializer2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    { };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NoElements_ExistingInitializer3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                    };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NoElements_ExistingInitializer4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>() { };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NoElements_ExistingInitializer5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        { };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NoElements_ExistingInitializer6()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        {
                        };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NoElements_ExistingInitializer7()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                        {
                        };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NoElements_ExistingInitializer8()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                        };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() { 1, 2 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [1, 2];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    { 1, 2 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [1, 2];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                        1,
                        2
                    };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [
                        1,
                        2
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>() { 1, 2 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [1, 2];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        { 1, 2 };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [1, 2];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer6()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        {
                            1,
                            2,
                        };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1,
                            2,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer7()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                        {
                            1,
                            2,
                        };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1,
                            2,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer8()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                            1,
                            2
                        };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [
                            1,
                            2
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer9()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        {
                            1, 2
                        };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1, 2
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_ExistingElements_ExistingInitializer10()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        {
                            1, 2,
                        };
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1, 2,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() { 1, 2 };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [1, 2, 3];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    { 1, 2 };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [1, 2, 3];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                        1,
                        2
                    };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [
                        1,
                        2,
                        3,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>() { 1, 2 };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [1, 2, 3];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        { 1, 2 };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [1, 2, 3];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer6()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        {
                            1,
                            2,
                        };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1,
                            2,
                            3,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer7()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                        {
                            1,
                            2,
                        };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1,
                            2,
                            3,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer8()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                            1,
                            2
                        };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [
                            1,
                            2,
                            3,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer9()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                        {
                            1, 2
                        };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1, 2, 3
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer10()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                        {
                            1, 2,
                        };
                    [|c.Add(|]3);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1, 2, 3,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() { 1, 2 };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                    [
                        1, 2,
                        3 +
                            4,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                    { 1, 2 };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                    [
                        1, 2,
                        3 +
                            4,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                        1,
                        2
                    };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [
                        1,
                        2,
                        3 +
                            4,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>() { 1, 2 };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1, 2,
                            3 +
                                4,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        { 1, 2 };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1, 2,
                            3 +
                                4,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer6()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [|new|] List<int>()
                        {
                            1,
                            2,
                        };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1,
                            2,
                            3 +
                                4,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer7()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>()
                        {
                            1,
                            2,
                        };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                        [
                            1,
                            2,
                            3 +
                                4,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer8()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                            1,
                            2
                        };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [
                            1,
                            2,
                            3 +
                                4,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer9()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                            1, 2
                        };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [
                            1, 2,
                            3 +
                                4,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer10()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>() {
                            1, 2,
                        };
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [
                            1, 2,
                            3 +
                                4,
                        ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_NoInitializer1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                    [
                        3 +
                            4,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestReplacementLocation_NewMultiLineElements_NoInitializer2()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>();
                    [|c.Add(|]1 +
                        2);
                    [|c.Add(|]3 +
                        4);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c =
                    [
                        1 +
                            2,
                        3 +
                            4,
                    ];
                }
            }
            """);
    }

    [Fact]
    public async Task TestNoMultiLineEvenWhenLongIfAllElementsAlreadyPresent()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections.Generic;

                namespace N
                {
                    class WellKnownDiagnosticTags
                    {
                        public static string Telemetry, EditAndContinue, Unnecessary, NotConfigurable;
                    }

                    class C
                    {
                        private static readonly string s_enforceOnBuildNeverTag;
                        class D
                        {
                            void M()
                            {
                                List<string> s_microsoftCustomTags = [|new|] List<string> { WellKnownDiagnosticTags.Telemetry };
                                List<string> s_editAndContinueCustomTags = [|new|] List<string> { WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag };
                                List<string> s_unnecessaryCustomTags = [|new|] List<string> { WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry };
                                List<string> s_notConfigurableCustomTags = [|new|] List<string> { WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry };
                                List<string> s_unnecessaryAndNotConfigurableCustomTags = [|new|] List<string> { WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry };
                            }
                        }
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections.Generic;
                
                namespace N
                {
                    class WellKnownDiagnosticTags
                    {
                        public static string Telemetry, EditAndContinue, Unnecessary, NotConfigurable;
                    }
                
                    class C
                    {
                        private static readonly string s_enforceOnBuildNeverTag;
                        class D
                        {
                            void M()
                            {
                                List<string> s_microsoftCustomTags = [WellKnownDiagnosticTags.Telemetry];
                                List<string> s_editAndContinueCustomTags = [WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag];
                                List<string> s_unnecessaryCustomTags = [WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry];
                                List<string> s_notConfigurableCustomTags = [WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry];
                                List<string> s_unnecessaryAndNotConfigurableCustomTags = [WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry];
                            }
                        }
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCapacity1()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>(0);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity2()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = new List<int>(1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity3()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>(1);
                    [|c.Add(|]0);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [0];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity4()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    List<int> c = [|new|] List<int>(0);
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
                    List<int> c = new List<int>(0)
                    {
                        1
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity5()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>(1 + x.Length);
                    [|c.Add(|]0);
                    [|c.AddRange(|]x);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [0, .. x];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity6()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>(x.Length + 1);
                    [|c.Add(|]0);
                    [|c.AddRange(|]x);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [0, .. x];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity7()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>(2 + x.Length);
                    [|c.Add(|]0);
                    [|c.AddRange(|]x);
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [0, .. x, 1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity8()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>(2 + x.Length + y.Length);
                    [|c.Add(|]0);
                    [|c.AddRange(|]x);
                    [|c.AddRange(|]y);
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [0, .. x, .. y, 1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity9()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>(x.Length + y.Length + 2);
                    [|c.Add(|]0);
                    [|c.AddRange(|]x);
                    [|c.AddRange(|]y);
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [0, .. x, .. y, 1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity10()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IList<int> y)
                {
                    List<int> c = [|new|] List<int>(x.Length + y.Count + 2);
                    [|c.Add(|]0);
                    [|c.AddRange(|]x);
                    [|c.AddRange(|]y);
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IList<int> y)
                {
                    List<int> c = [0, .. x, .. y, 1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity11()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(x.Length + y.Count() + 2);
                    [|c.Add(|]0);
                    [|c.AddRange(|]x);
                    [|c.AddRange(|]y);
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [0, .. x, .. y, 1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity12()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(x.Length + y.Count() + 2) { 0, 1 };
                    [|c.AddRange(|]x);
                    [|c.AddRange(|]y);
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [0, 1, .. x, .. y];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity13()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x)
                {
                    List<int> c = [|new|] List<int>(1 + x.Length);
                    [|c.Add(|]1);
                    [|foreach (var v in |]x)
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
                    List<int> c = [1, .. x];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity14()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(1 + x.Length + y.Count() + 1);
                    [|c.Add(|]0);
                    [|c.AddRange(|]x);
                    [|c.AddRange(|]y);
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [0, .. x, .. y, 1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity15()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(1 + x.Length + y.Count());
                    [|c.Add(|]0);
                    c.AddRange(x);
                    c.AddRange(y);
                    c.Add(1);
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(1 + x.Length + y.Count())
                    {
                        0
                    };
                    c.AddRange(x);
                    c.AddRange(y);
                    c.Add(1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity16()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(1 - x.Length);
                    [|c.Add(|]0);
                    c.AddRange(x);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(1 - x.Length)
                    {
                        0
                    };
                    c.AddRange(x);
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity17()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(1);
                    [|c.Add(|]0);
                    c.AddRange(x);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(1)
                    {
                        0
                    };
                    c.AddRange(x);
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity18()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>(1 + x.Length + y.Length);
                    [|c.Add(|]0);
                    c.AddRange(x);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = new List<int>(1 + x.Length + y.Length)
                    {
                        0
                    };
                    c.AddRange(x);
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity19()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>(x);
                    [|c.Add(|]0);
                    c.AddRange(y);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = new List<int>(x)
                    {
                        0
                    };
                    c.AddRange(y);
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity20()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(x.Length + y.Count() + 2) { 0 };
                    [|c.Add(|]1);
                    [|c.AddRange(|]x);
                    [|c.AddRange(|]y);
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [0, 1, .. x, .. y];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity21()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = [|new|] List<int>(1 + y.Count());
                    [|c.Add(|]0);
                    c.AddRange(x);
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M(int[] x, IEnumerable<int> y)
                {
                    List<int> c = new List<int>(1 + y.Count())
                    {
                        0
                    };
                    c.AddRange(x);
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity22()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IList<int> y)
                {
                    List<int> c = [|new|] List<int>(x.Length + x.Length + 2);
                    [|c.Add(|]0);
                    [|c.AddRange(|]x);
                    [|c.AddRange(|]x);
                    [|c.Add(|]1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IList<int> y)
                {
                    List<int> c = [0, .. x, .. x, 1];
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity23()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IList<int> y)
                {
                    List<int> c = [|new|] List<int>(x.Length + 2);
                    [|c.Add(|]0);
                    c.AddRange(x);
                    c.AddRange(x);
                    c.Add(1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IList<int> y)
                {
                    List<int> c = new List<int>(x.Length + 2)
                    {
                        0
                    };
                    c.AddRange(x);
                    c.AddRange(x);
                    c.Add(1);
                }
            }
            """);
    }

    [Fact]
    public async Task TestCapacity24()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IList<int> y)
                {
                    List<int> c = [|new|] List<int>(x.Length + x.Length + x.Length + 2);
                    [|c.Add(|]0);
                    c.AddRange(x);
                    c.AddRange(x);
                    c.Add(1);
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, IList<int> y)
                {
                    List<int> c = new List<int>(x.Length + x.Length + x.Length + 2)
                    {
                        0
                    };
                    c.AddRange(x);
                    c.AddRange(x);
                    c.Add(1);
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public async Task TestInLambda()
    {
        await TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq.Expressions;

            class C
            {
                void M()
                {
                    Func<List<int>> f = () => [|new|] List<int>();
                }
            }
            """,
            """
            using System;
            using System.Collections.Generic;
            using System.Linq.Expressions;

            class C
            {
                void M()
                {
                    Func<List<int>> f = () => [];
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public async Task TestNotInLambda1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq.Expressions;

            class C
            {
                void M()
                {
                    var e = () => new List<int>();
                }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public async Task TestNotInExpressionTree()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections.Generic;
            using System.Linq.Expressions;

            class C
            {
                void M()
                {
                    Expression<Func<List<int>>> e = () => new List<int>();
                }
            }
            """);
    }

    [Fact]
    public async Task TestInDictionary_Empty()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class Program
            {
                static void Main()
                {
                    Dictionary<string, object> d = [|new|] Dictionary<string, object>() { };
                }
            }
            """,
            """
            using System.Collections.Generic;
            class Program
            {
                static void Main()
                {
                    Dictionary<string, object> d = [];
                }
            }
            """);
    }

    [Fact]
    public async Task TestInDictionary_NotEmpty()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            class Program
            {
                static void Main()
                {
                    Dictionary<string, object> d = new Dictionary<string, object>() { { string.Empty, null } };
                }
            }
            """);
    }

    [Fact]
    public async Task TestInIEnumerableAndIncompatibleAdd_Empty()
    {
        await TestInRegularAndScriptAsync(
            """
            using System.Collections;
            class Program
            {
                static void Main()
                {
                    MyCollection c = [|new|] MyCollection() { };
                }
            }
            class MyCollection : IEnumerable
            {
                public void Add(string s) { }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }
            """,
            """
            using System.Collections;
            class Program
            {
                static void Main()
                {
                    MyCollection c = [];
                }
            }
            class MyCollection : IEnumerable
            {
                public void Add(string s) { }
                IEnumerator IEnumerable.GetEnumerator() => null;
            }
            """);
    }

    [Fact]
    public async Task TestInIEnumerableAndIncompatibleAdd_NotEmpty()
    {
        await new VerifyCS.Test
        {
            ReferenceAssemblies = Testing.ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                using System.Collections;
                class Program
                {
                    static void Main()
                    {
                        MyCollection c = [|new|] MyCollection() { "a", "b" };
                    }
                }
                class MyCollection : IEnumerable
                {
                    public void Add(string s) { }
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                """,
            FixedCode = """
                using System.Collections;
                class Program
                {
                    static void Main()
                    {
                        MyCollection c = ["a", "b"];
                    }
                }
                class MyCollection : IEnumerable
                {
                    public void Add(string s) { }
                    IEnumerator IEnumerable.GetEnumerator() => null;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState =
            {
                OutputKind = OutputKind.DynamicallyLinkedLibrary,
            },
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71607")]
    public async Task TestAddRangeOfCollectionExpression1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> numbers = [|new|]() { 1, 2 };
                        [|numbers.AddRange(|][4, 5]);
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M()
                    {
                        List<int> numbers = [1, 2, 4, 5];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71607")]
    public async Task TestAddRangeOfCollectionExpression2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> numbers = [|new|]() { 1, 2 };
                        [|numbers.AddRange(|][4, .. x]);
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    void M(int[] x)
                    {
                        List<int> numbers = [1, 2, 4, .. x];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }
}
