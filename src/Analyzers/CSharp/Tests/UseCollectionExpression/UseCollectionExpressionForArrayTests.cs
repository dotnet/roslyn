// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Analyzers.UseCoalesceExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UseCoalesceExpression;
using Xunit;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Testing;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForArrayCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionExpression)]
public class UseCollectionExpressionForArray
{
    [Fact]
    public async Task TestNotInCSharp11()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = { 1, 2, 3 };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp11,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInCSharp12()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|{|] 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSingleLine_TrailingComma()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|{|] 1, 2, 3, };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = [1, 2, 3,];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSingleLine_Trivia()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = /*x*/ [|{|] /*y*/ 1, 2, 3 /*z*/ } /*w*/;
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = /*x*/ [/*y*/ 1, 2, 3 /*z*/] /*w*/;
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultiLine()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|{|]
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultiLine_TrailingComma()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|{|]
                        1, 2, 3,
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = [
                        1, 2, 3,
                    ];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestEmpty1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|{|]};
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = [];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestEmpty2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|{|] };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = [];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithIncompatibleExplicitArrays()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    object[] i = new string[] { "" };
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithCompatibleExplicitArrays1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    object[] i = [|[|new|] object[]|] { "" };
                }
                """,
            FixedCode = """
                class C
                {
                    object[] i = [""];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithCompatibleExplicitArrays2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    object[] i = [|[|new|] object[]|]
                    {
                        ""
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    object[] i = [
                        ""
                    ];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithCompatibleExplicitArrays_Empty()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    object[] i = [|[|new|] object[]|] { };
                }
                """,
            FixedCode = """
                class C
                {
                    object[] i = [];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithCompatibleExplicitArrays_TrailingComma()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    object[] i = [|[|new|] object[]|] { "", };
                }
                """,
            FixedCode = """
                class C
                {
                    object[] i = ["",];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithIncompatibleImplicitArrays()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    object[] i = new[] { "" };
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithCompatibleImplicitArrays1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string[] i = [|[|new|][]|] { "" };
                }
                """,
            FixedCode = """
                class C
                {
                    string[] i = [""];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithCompatibleImplicitArrays2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string[] i = [|[|new|][]|]
                    {
                        ""
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    string[] i = [
                        ""
                    ];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMissingOnEmptyImplicitArray()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string[] i = {|CS0826:{|CS0029:new[] { }|}|};
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithCompatibleImplicitArrays_TrailingComma()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string[] i = [|[|new|][]|] { "", };
                }
                """,
            FixedCode = """
                class C
                {
                    string[] i = ["",];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }
}
