// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
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
public sealed partial class UseCollectionInitializerTests_CollectionExpression
{
    private static Task TestInRegularAndScriptAsync([StringSyntax("C#-test")] string testCode, [StringSyntax("C#-test")] string fixedCode, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = testCode,
            FixedCode = fixedCode,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState = { OutputKind = outputKind }
        }.RunAsync();

    private static Task TestMissingInRegularAndScriptAsync([StringSyntax("C#-test")] string testCode)
        => TestInRegularAndScriptAsync(testCode, testCode);

    [Fact]
    public Task TestNotOnVarVariableDeclarator()
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
    public Task TestNotWithConstructorArguments1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWithConstructorArguments2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInField1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInField2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInField3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInField4()
        => TestInRegularAndScriptAsync("""
            using System.Collections.Generic;

            class C
            {
                List<int> c = [|new|] List<int>(new[] { 1, 2, 3 });
            }
            """, """
            using System.Collections.Generic;

            class C
            {
                List<int> c = [.. new[] { 1, 2, 3 }];
            }
            """);

    [Fact]
    public Task TestInField5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInField6()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInField7()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInArgument1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInArgument2_InterfacesOn()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInArgument2_InterfacesOff()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestOnVariableDeclarator()
        => TestInRegularAndScriptAsync(
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

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/70172"), WorkItem("https://github.com/dotnet/roslyn/issues/69277")]
    public Task TestOnVariableDeclarator_If1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_If2()
        => TestInRegularAndScriptAsync(
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

    [Fact(Skip = "https://github.com/dotnet/roslyn/issues/70172"), WorkItem("https://github.com/dotnet/roslyn/issues/69277")]
    public Task TestOnVariableDeclarator_If3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_If4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_If5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_If6()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_If7()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_If8()
        => TestInRegularAndScriptAsync(
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

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/73879")]
    [InlineData("IList")]
    [InlineData("ICollection")]
    public Task TestOnVariableDeclaratorDifferentType_Interface_LooseMatch_MutableInterface(string collectionType)
        => TestInRegularAndScriptAsync(
            $$"""
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    {{collectionType}}<int> c = [|new|] List<int>();
                    [|c.Add(|]1);
                }
            }
            """,
            $$"""
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    {{collectionType}}<int> c = [1];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73879")]
    public Task TestOnVariableDeclaratorDifferentType_Interface_LooseMatch_ReadOnlyInterface()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    IEnumerable<int> c = [|new|] List<int>();
                }
            }
            """,
            """
            using System.Collections.Generic;

            class C
            {
                void M()
                {
                    IEnumerable<int> c = [];
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73879")]
    public Task TestOnVariableDeclaratorDifferentType_Interface_ExactlyMatch_MutableInterface()
        => new VerifyCS.Test
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
                        IList<int> c = [1];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73879")]
    public Task TestOnVariableDeclaratorDifferentType_Interface_ExactlyMatch_ReadOnlyInterface()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = Testing.ReferenceAssemblies.NetCore.NetCoreApp31,
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void M()
                    {
                        IReadOnlyList<int> c = new List<int>();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
        }.RunAsync();

    [Fact]
    public Task TestOnVariableDeclarator_Foreach1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_Foreach1_A()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_Foreach1_B()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_Foreach1_C()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_Foreach2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_Foreach3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_Foreach4()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70388")]
    public Task TestOnVariableDeclarator_AwaitForeach1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_AddRange1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestOnVariableDeclarator_AddRangeAndForeach1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestIndexAccess1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestIndexAccess1_Foreach()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestIndexAccess3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestIndexFollowedByInvocation()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInvocationFollowedByIndex()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWithInterimStatement()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingOnNonIEnumerable()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMissingOnNonIEnumerableEvenWithAdd()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestWithCreationArguments()
        => TestInRegularAndScriptAsync(
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
                    c = [1];
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
                    array[0] = [1, 2];
                }
            }
            """);

    [Fact]
    public Task TestOnNamedArg()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializer_NoParens()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializerWithComma()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/39146")]
    public Task TestWithExistingInitializer2()
        => TestInRegularAndScriptAsync(
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
                    array[0] = [1, 2];
                    array[1] = [3, 4];
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

    [Fact]
    public Task TestTrivia1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestTrivia2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestTrivia3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestTrivia4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestTrivia5()
        => TestInRegularAndScriptAsync(
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

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/46670")]
    public Task TestTriviaRemoveLeadingBlankLinesForFirstElement()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestComplexInitializer2()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/16241")]
    public Task TestNestedCollectionInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17823")]
    public Task TestWhenReferencedInInitializer()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17823")]
    public Task TestWhenReferencedInInitializer_LocalVar()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/17823")]
    public Task TestWhenReferencedInInitializer_LocalVar2()
        => TestInRegularAndScriptAsync("""
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    List<int> t = [|new|] List<int>(new int[] { 1, 2, 3 });
                    t.Add(t.Min() - 1);
                }
            }
            """, """
            using System.Collections.Generic;
            using System.Linq;
            
            class C
            {
                void M()
                {
                    List<int> t = [.. new int[] { 1, 2, 3 }];
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
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;
            using System.Linq;

            class C
            {
                void M()
                {
                    List<int> t = null;
                    t = [|new|] List<int>(new int[] { 1, 2, 3 });
                    t.Add(t.Min() - 1);
                }
            }
            """,
            """
            using System.Collections.Generic;
            using System.Linq;
            
            class C
            {
                void M()
                {
                    List<int> t = null;
                    t = [.. new int[] { 1, 2, 3 }];
                    t.Add(t.Min() - 1);
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/18260")]
    public Task TestFieldReference()
        => TestInRegularAndScriptAsync(
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
                    List<object> items = new List<object>();
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/19253")]
    public Task TestKeepBlankLinesAfter()
        => TestInRegularAndScriptAsync(
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

    [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseObjectInitializer)]
    [WorkItem("https://github.com/dotnet/roslyn/issues/61066")]
    public Task TestInTopLevelStatements()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            List<int> list = [|new|] List<int>();
            [|list.Add(|]1);
            """,
            """
            using System.Collections.Generic;

            List<int> list = [1];

            """, OutputKind.ConsoleApplication);

    [Fact]
    public Task TestUpdateExistingCollectionInitializerToExpression1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUpdateExistingCollectionInitializerToExpression2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUpdateExistingCollectionInitializerToExpression3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUpdateExistingCollectionInitializerToExpression4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestUpdateExistingCollectionInitializerToExpression5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NoElements_ExistingInitializer1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NoElements_ExistingInitializer2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NoElements_ExistingInitializer3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NoElements_ExistingInitializer4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NoElements_ExistingInitializer5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NoElements_ExistingInitializer6()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NoElements_ExistingInitializer7()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NoElements_ExistingInitializer8()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer6()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer7()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer8()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer9()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_ExistingElements_ExistingInitializer10()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer6()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer7()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer8()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer9()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewSingleLineElements_ExistingElements_ExistingInitializer10()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer6()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer7()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer8()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer9()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_ExistingElements_ExistingInitializer10()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_NoInitializer1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestReplacementLocation_NewMultiLineElements_NoInitializer2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNoMultiLineEvenWhenLongIfAllElementsAlreadyPresent()
        => new VerifyCS.Test
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

    [Fact]
    public Task TestCapacity1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity2()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity3()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity4()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity5()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity6()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity7()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity8()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity9()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity10()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity11()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity12()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity13()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity14()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity15()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity16()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity17()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity18()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity19()
        => TestInRegularAndScriptAsync(
            """
            using System.Collections.Generic;

            class C
            {
                void M(int[] x, int[] y)
                {
                    List<int> c = [|new|] List<int>(x);
                    [|c.Add(|]0);
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
                    List<int> c = [.. x, 0, .. y];
                }
            }
            """);

    [Fact]
    public Task TestCapacity20()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity21()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity22()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity23()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestCapacity24()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public Task TestInLambda()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public Task TestNotInLambda1()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public Task TestNotInExpressionTree()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInDictionary_Empty()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInDictionary_NotEmpty()
        => TestMissingInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInIEnumerableAndIncompatibleAdd_Empty()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInIEnumerableAndIncompatibleAdd_NotEmpty()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71607")]
    public Task TestAddRangeOfCollectionExpression1()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71607")]
    public Task TestAddRangeOfCollectionExpression2()
        => new VerifyCS.Test
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72169")]
    public Task TestInValueTuple1()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, List<int>) M() {
                        return (42, [|new|] List<int>());
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, List<int>) M() {
                        return (42, []);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72169")]
    public Task TestInValueTuple2()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, List<int>) M() {
                        return (42, [|new|] List<int>() { });
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, List<int>) M() {
                        return (42, []);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72169")]
    public Task TestInValueTuple3()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, List<int>) M() {
                        return (42, [|new|] List<int> { 1, 2, 3 });
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, List<int>) M() {
                        return (42, [1, 2, 3]);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72169")]
    public Task TestInValueTuple4()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, List<int>) M() {
                        return (42, [|new|] List<int>
                        {
                            1,
                            2,
                            3
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
                    public (int, List<int>) M() {
                        return (42,
                        [
                            1,
                            2,
                            3
                        ]);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72169")]
    public Task TestInValueTuple5()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, Func<List<int>>) M() {
                        return (42, () => [|new|] List<int>());
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, Func<List<int>>) M() {
                        return (42, () => []);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72169")]
    public Task TestInValueTuple6()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, Func<List<int>>) M() {
                        return (42, () => [|new|] List<int>() { 1, 2, 3 });
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, Func<List<int>>) M() {
                        return (42, () => [1, 2, 3]);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72169")]
    public Task TestInValueTuple7()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;

                class C
                {
                    public (int, Func<List<int>>) M() {
                        return (42, () => [|new|] List<int>
                        {
                            1,
                            2,
                            3
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
                    public (int, Func<List<int>>) M() {
                        return (42, () =>
                        [
                            1,
                            2,
                            3
                        ]);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72701")]
    public Task TestNotWithObservableCollection1()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Collections.ObjectModel;

                class C
                {
                    void M()
                    {
                        IList<string> strings = new ObservableCollection<string>();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72701")]
    public Task TestNotWithObservableCollection2()
        => new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Collections.ObjectModel;

                class C
                {
                    void M()
                    {
                        ObservableCollection<string> strings = [|new|] ObservableCollection<string>();
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Collections.ObjectModel;

                class C
                {
                    void M()
                    {
                        ObservableCollection<string> strings = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [|new|] List<int>(values);
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [.. values];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [|new|] List<int>(values) { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [.. values, 1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73362")]
    public Task TestWithOverloadResolution1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    public void Test(Class1 param1, Class1 param2)
                    {
                        MethodTakingEnumerable([|new|] List<Class1> { param1, param2 });
                    }

                    public void MethodTakingEnumerable(IEnumerable<Class1> param)
                    {
                    }

                    public void MethodTakingEnumerable(IEnumerable<Class2> param)
                    {
                    }

                    public class Class1 { }
                    public class Class2 { }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    public void Test(Class1 param1, Class1 param2)
                    {
                        MethodTakingEnumerable([param1, param2]);
                    }
                
                    public void MethodTakingEnumerable(IEnumerable<Class1> param)
                    {
                    }
                
                    public void MethodTakingEnumerable(IEnumerable<Class2> param)
                    {
                    }
                
                    public class Class1 { }
                    public class Class2 { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/75894")]
    public Task TestNotOnDictionaryConstructor()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void Main()
                    {
                        Dictionary<string, string> a = null;
                        Dictionary<string, string> d = new(a);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76092")]
    public Task TestNotOnSetAssignedToNonSet()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void Main()
                    {
                        ICollection<int> a = new HashSet<int>();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76092")]
    public Task TestOnSetAssignedToSet()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;

                class C
                {
                    void Main()
                    {
                        HashSet<int> a = [|new|] HashSet<int>();
                    }
                }
                """,
            FixedCode = """
                using System.Collections.Generic;

                class C
                {
                    void Main()
                    {
                        HashSet<int> a = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76683")]
    public Task TestNotOnStack()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                using System.Collections.Immutable;
                
                class C
                {
                    Stack<T> GetNumbers<T>(T[] values)
                    {
                        return new Stack<T>(values);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/77962")]
    public Task TestNotOnBindingList()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.ComponentModel;
                
                class C
                {
                    BindingList<T> GetNumbers<T>(T[] values)
                    {
                        return new BindingList<T>(values);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79156")]
    public Task TestNotWithConstructorArgNotValidAsAddArgument()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;

                Goo _ = new("goobar");

                public class Goo(string s = "") : IEnumerable
                {
                    public void Add(Bar b) { }

                    IEnumerator IEnumerable.GetEnumerator()
                        => throw new NotImplementedException();
                }

                public class Bar { }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/79156")]
    public Task TestWithConstructorArgValidAsAddArgument()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                IEnumerable<Bar> bars = null;
                Goo _ = [|new|](bars);

                public class Goo : IEnumerable
                {
                    public Goo() { }

                    public Goo(IEnumerable<Bar> bars)
                    {
                        foreach (var bar in bars)
                            Add(bar);
                    }

                    public void Add(Bar b) { }

                    IEnumerator IEnumerable.GetEnumerator()
                        => throw new NotImplementedException();
                }

                public class Bar { }
                """,
            FixedCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                IEnumerable<Bar> bars = null;
                Goo _ = [.. bars];

                public class Goo : IEnumerable
                {
                    public Goo() { }

                    public Goo(IEnumerable<Bar> bars)
                    {
                        foreach (var bar in bars)
                            Add(bar);
                    }

                    public void Add(Bar b) { }

                    IEnumerator IEnumerable.GetEnumerator()
                        => throw new NotImplementedException();
                }

                public class Bar { }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80471")]
    public Task TestNeedForCast1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;

                class C
                {
                    private void CodeFixErrorRepro_OldEnumerables()
                    {
                        ArrayList arrayList = [1, 2, 3];

                        List<int> stronglyTypedList = [|new|]();
                        [|foreach (int i in |]arrayList)
                        {
                            stronglyTypedList.Add(i);
                        }
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Collections;
                using System.Collections.Generic;
                using System.Linq;
                
                class C
                {
                    private void CodeFixErrorRepro_OldEnumerables()
                    {
                        ArrayList arrayList = [1, 2, 3];
                
                        List<int> stronglyTypedList = [.. arrayList.Cast<int>()];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70099")]
    public Task TestNotInCollectionBuilderMethod()
        => TestMissingInRegularAndScriptAsync(
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyCustomCollection), nameof(MyCustomCollection.Create))]
            internal class MyCustomCollection<T> : Collection<T>
            {
            }

            internal static class MyCustomCollection
            {
                public static MyCustomCollection<T> Create<T>(ReadOnlySpan<T> items)
                {
                    MyCustomCollection<T> collection = new();
                    foreach (T item in items)
                    {
                        collection.Add(item);
                    }

                    return collection;
                }
            }

            """ + UseCollectionExpressionForEmptyTests.CollectionBuilderAttributeDefinition);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70099")]
    public Task TestCollectionBuilderOutsideMethod()
        => TestInRegularAndScriptAsync(
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyCustomCollection), nameof(MyCustomCollection.Create))]
            internal class MyCustomCollection<T> : Collection<T>
            {
            }

            internal static class MyCustomCollection
            {
                public static MyCustomCollection<T> Create<T>(ReadOnlySpan<T> items)
                {
                    MyCustomCollection<T> collection = new();
                    foreach (T item in items)
                    {
                        collection.Add(item);
                    }

                    return collection;
                }
            }

            class C
            {
                void M()
                {
                    MyCustomCollection<int> c = [|new|]();
                    [|c.Add(|]1);
                }
            }

            """ + UseCollectionExpressionForEmptyTests.CollectionBuilderAttributeDefinition,
            """
            using System;
            using System.Collections;
            using System.Collections.Generic;
            using System.Collections.ObjectModel;
            using System.Runtime.CompilerServices;

            [CollectionBuilder(typeof(MyCustomCollection), nameof(MyCustomCollection.Create))]
            internal class MyCustomCollection<T> : Collection<T>
            {
            }

            internal static class MyCustomCollection
            {
                public static MyCustomCollection<T> Create<T>(ReadOnlySpan<T> items)
                {
                    MyCustomCollection<T> collection = new();
                    foreach (T item in items)
                    {
                        collection.Add(item);
                    }

                    return collection;
                }
            }

            class C
            {
                void M()
                {
                    MyCustomCollection<int> c = [1];
                }
            }

            """ + UseCollectionExpressionForEmptyTests.CollectionBuilderAttributeDefinition);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument1_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [|new|] List<int>(values);
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [with(values)];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument2_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [|new|] List<int>(values) { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [with(values), 1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument3_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [|new|] List<int>(0) { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [with(0), 1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestObjectCreationArgument4_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [|new|] List<int>(capacity: 0) { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        List<int> list = [with(capacity: 0), 1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestKeyValuePair1_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [|new|] Dictionary<int, string>() { { 1, "x" } };
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [1: "x"];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestKeyValuePair2_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [|new|] Dictionary<int, string>()
                        {
                            { 1, "x" },
                            { 2, "y" },
                        };
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map =
                        [
                            1: "x",
                            2: "y",
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestKeyValuePair3_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [|new|] Dictionary<int, string>() { [1] = "x" };
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [1: "x"];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestKeyValuePair4_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [|new|] Dictionary<int, string>()
                        {
                            [1] = "x",
                            [2] = "y",
                        };
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map =
                        [
                            1: "x",
                            2: "y",
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestKeyValuePair5_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [|new|] Dictionary<int, string>();
                        [|map.Add(|]1, "x");
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [1: "x"];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestKeyValuePair6_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [|new|] Dictionary<int, string>();
                        map[1] = "x";
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<int, string> map = [1: "x"];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task TestKeyValuePair7_CSharp14()
        => new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<string, string> map = [|new|] Dictionary<string, string>(StringComparer.Ordinal);
                        map["x"] = "y";
                    }
                }
                """,
            FixedCode = """
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        Dictionary<string, string> map = [with(StringComparer.Ordinal), "x": "y"];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task DictionaryType1()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        X([|new|] Dictionary<int, string>()
                        {
                            [1] = "x",
                            [2] = "y",
                        });
                    }

                    void X(Dictionary<int, string> map)
                    {
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        X(
                        [
                            1: "x",
                            2: "y",
                        ]);
                    }
                
                    void X(Dictionary<int, string> map)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task DictionaryType2()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        X([|new|] Dictionary<int, string>()
                        {
                            [1] = "x",
                            [2] = "y",
                        });
                    }

                    void X(IDictionary<int, string> map)
                    {
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        X(
                        [
                            1: "x",
                            2: "y",
                        ]);
                    }
                
                    void X(IDictionary<int, string> map)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task DictionaryType3_ExactMatch()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        X(new Dictionary<int, string>()
                        {
                            [1] = "x",
                            [2] = "y",
                        });
                    }

                    void X(IReadOnlyDictionary<int, string> map)
                    {
                    }
                }
                """,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72699")]
    public Task DictionaryType3_LooseMatch()
        => new VerifyCS.Test
        {
            TestCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        X([|new|] Dictionary<int, string>()
                        {
                            [1] = "x",
                            [2] = "y",
                        });
                    }

                    void X(IReadOnlyDictionary<int, string> map)
                    {
                    }
                }
                """,
            FixedCode = """
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int[] values)
                    {
                        X(
                        [
                            1: "x",
                            2: "y",
                        ]);
                    }
                
                    void X(IReadOnlyDictionary<int, string> map)
                    {
                    }
                }
                """,
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_loosely_match
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
}

