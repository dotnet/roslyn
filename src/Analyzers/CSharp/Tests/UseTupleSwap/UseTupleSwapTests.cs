// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.UseTupleSwap;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseTupleSwap;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseTupleSwapDiagnosticAnalyzer,
    CSharpUseTupleSwapCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseTupleSwap)]
public sealed class UseTupleSwapTests
{
    [Fact]
    public Task TestMissingBeforeCSharp7()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args)
                {
                    var temp = args[0];
                    args[0] = args[1];
                    args[1] = temp;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp6,
        }.RunAsync();

    [Fact]
    public Task TestMissingWithFeatureOff()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args)
                {
                    var temp = args[0];
                    args[0] = args[1];
                    args[1] = temp;
                }
            }
            """,
            Options =
            {
                { CSharpCodeStyleOptions.PreferTupleSwap, false, CodeStyle.NotificationOption2.Silent }
            }
        }.RunAsync();

    [Fact]
    public Task TestBasicCase()
        => VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(string[] args)
                {
                    [|var|] temp = args[0];
                    args[0] = args[1];
                    args[1] = temp;
                }
            }
            """,
            """
            class C
            {
                void M(string[] args)
                {
                    (args[1], args[0]) = (args[0], args[1]);
                }
            }
            """);

    [Fact]
    public Task TestNotWithRef()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(ref int a, ref int b)
                {
                    ref int temp = ref a;
                    a = ref b;
                    b = ref temp;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestArbitraryParens()
        => VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(string[] args)
                {
                    [|var|] temp = (args[0]);
                    ((args[0])) = (((args[1])));
                    ((((args[1])))) = (((((temp)))));
                }
            }
            """,
            """
            class C
            {
                void M(string[] args)
                {
                    (args[1], args[0]) = (args[0], args[1]);
                }
            }
            """);

    [Fact]
    public Task TestTrivia1()
        => VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(string[] args)
                {
                    // Comment
                    [|var|] temp = args[0];
                    args[0] = args[1];
                    args[1] = temp;
                }
            }
            """,
            """
            class C
            {
                void M(string[] args)
                {
                    // Comment
                    (args[1], args[0]) = (args[0], args[1]);
                }
            }
            """);

    [Fact]
    public Task TestTrivia2()
        => VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(string[] args)
                {
                    [|var|] temp = args [ 0 ] ;
                    args  [  0  ] = args   [   1   ];
                    args    [    1    ] = temp;
                }
            }
            """,
            """
            class C
            {
                void M(string[] args)
                {
                    (args   [   1   ], args  [  0  ]) = (args  [  0  ], args   [   1   ]);
                }
            }
            """);

    [Fact]
    public Task TestSimpleAssignment1()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args)
                {
                    var temp = args[0];
                    args[0] += args[1];
                    args[1] = temp;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestSimpleAssignment2()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args)
                {
                    var temp = args[0];
                    args[0] = args[1];
                    args[1] += temp;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotSwap1()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args, string temp1)
                {
                    var temp = args[0];
                    args[0] = args[1];
                    args[1] = temp1;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotSwap2()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args)
                {
                    var temp = args[0];
                    args[1] = temp;
                    args[0] = args[1];
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotSwap3()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args)
                {
                    var temp = args[0];
                    args[0] = args[1];
                    args[0] = temp;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotSwap4()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args)
                {
                    var temp = args[0];
                    args[1] = args[0];
                    args[0] = temp;
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestNotSwap5()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args)
                {
                    string temp;
                    args[0] = args[1];
                    args[1] = {|CS0165:temp|};
                }
            }
            """,
        }.RunAsync();

    [Fact]
    public Task TestInSwitch()
        => VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(string[] args, int x)
                {
                    switch (x)
                    {
                        default:
                            [|var|] temp = args[0];
                            args[0] = args[1];
                            args[1] = temp;
                            break;
                    }
                }
            }
            """,
            """
            class C
            {
                void M(string[] args, int x)
                {
                    switch (x)
                    {
                        default:
                            (args[1], args[0]) = (args[0], args[1]);
                            break;
                    }
                }
            }
            """);

    [Fact]
    public Task TestFixAll1()
        => VerifyCS.VerifyCodeFixAsync(
            """
            class C
            {
                void M(string[] args)
                {
                    // comment 1
                    [|var|] temp1 = args[0];
                    args[0] = args[1];
                    args[1] = temp1;

                    // comment 2
                    [|var|] temp2 = args[2];
                    args[2] = args[3];
                    args[3] = temp2;
                }
            }
            """,
            """
            class C
            {
                void M(string[] args)
                {
                    // comment 1
                    (args[1], args[0]) = (args[0], args[1]);

                    // comment 2
                    (args[3], args[2]) = (args[2], args[3]);
                }
            }
            """);

    [Fact]
    public Task TestNotWithMultipleVariables()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M(string[] args)
                {
                    string temp = args[0], temp2 = "";
                    args[0] = args[1];
                    args[1] = temp;
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/58759")]
    public Task TestTopLevelStatements()
        => new VerifyCS.Test
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState =
            {
                Sources =
                {
                    """
                    [|var|] temp = args[0];
                    args[0] = args[1];
                    args[1] = temp;
                    """,
                },
                OutputKind = OutputKind.ConsoleApplication,
            },
            FixedCode = """
                (args[1], args[0]) = (args[0], args[1]);

                """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66427")]
    public Task NotOnRefStruct()
        => new VerifyCS.Test
        {
            TestCode = """
            ref struct S { }

            class C
            {
                void M()
                {
                    S v0 = default;
                    S v1 = default;

                    var vTmp = v0;
                    v0 = v1;
                    v1 = vTmp;
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66427")]
    public Task OnNormalStruct()
        => new VerifyCS.Test
        {
            TestCode = """
            struct S { }

            class C
            {
                void M()
                {
                    S v0 = default;
                    S v1 = default;

                    [|var|] vTmp = v0;
                    v0 = v1;
                    v1 = vTmp;
                }
            }
            """,
            FixedCode = """
            struct S { }

            class C
            {
                void M()
                {
                    S v0 = default;
                    S v1 = default;

                    (v1, v0) = (v0, v1);
                }
            }
            """,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66427")]
    public Task NotOnPointer()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                unsafe void M(int* v0, int* v1)
                {
                    var vTmp = v0;
                    v0 = v1;
                    v1 = vTmp;
                }
            }
            """,
        }.RunAsync();
}
