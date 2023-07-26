// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

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

    [Theory, CombinatorialData]
    public async Task TestNotWithVar_ExplicitArrayType(
         [CombinatorialValues(new object[] { "var", "object", "dynamic" })] string type)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class C
                {
                    void M()
                    {
                        {{type}} i = new string[] { "" };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Theory, CombinatorialData]
    public async Task TestNotWithVar_ExplicitArrayType2(
        [CombinatorialValues(new object[] { "var", "object", "dynamic" })] string type)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class C
                {
                    void M()
                    {
                        {{type}} i = (new string[] { "" });
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Theory, CombinatorialData]
    public async Task TestNotWithVar_ImplicitArrayType(
        [CombinatorialValues(new object[] { "var", "object", "dynamic" })] string type)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class C
                {
                    void M()
                    {
                        {{type}} i = new[] { "" };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Theory, CombinatorialData]
    public async Task TestNotWithVar_ImplicitArrayType2(
        [CombinatorialValues(new object[] { "var", "object", "dynamic" })] string type)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class C
                {
                    void M()
                    {
                        {{type}} i = (new[] { "" });
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithExtension()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        var i = new int[] { 1 }.AsSpan();
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net70,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedToField()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int[] X = [|[|new|] int[]|] { 1 };
                }
                """,
            FixedCode = """
                class C
                {
                    private int[] X = [1];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedToProperty()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    private int[] X { get; } = [|[|new|] int[]|] { 1 };
                }
                """,
            FixedCode = """
                class C
                {
                    private int[] X { get; } = [1];
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedToComplexCast()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        var c = (int[])[|[|new|] int[]|] { 1 };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        var c = (int[])[1];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotTargetTypedWithIdentifierCast()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using IntArray = int[];

                class C
                {
                    void M()
                    {
                        var c = (IntArray)new int[] { 1 };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInConditional1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x)
                    {
                        var c = true ? [|[|new|] int[]|] { 1 } : x;
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x)
                    {
                        var c = true ? [1] : x;
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInConditional2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x)
                    {
                        var c = true ? x : [|[|new|] int[]|] { 1 };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x)
                    {
                        var c = true ? x : [1];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInConditional3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x)
                    {
                        int[] c = true ? null : [|[|new|] int[]|] { 1 };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x)
                    {
                        int[] c = true ? null : [1];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotTargetTypedInConditional4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x)
                    {
                        var c = true ? null : new int[] { 1 };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInSwitchExpressionArm1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = b switch { true => x, false => [|[|new|] int[]|] { 1 } };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = b switch { true => x, false => [1] };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInSwitchExpressionArm2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = b switch { false => [|[|new|] int[]|] { 1 }, true => x };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = b switch { false => [1], true => x };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInSwitchExpressionArm3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        int[] c = b switch { false => [|[|new|] int[]|] { 1 }, true => null };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        int[] c = b switch { false => [1], true => null };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotTargetTypedInSwitchExpressionArm4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = b switch { false => new int[] { 1 }, true => null };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotTargetTypedInitializer1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = new int[,] { { 1, 2, 3 } };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInitializer2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = new[] { [|[|new|][]|] { 1, 2, 3 }, x };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = new[] { [1, 2, 3], x };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInitializer3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = new[] { x, [|[|new|][]|] { 1, 2, 3 } };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = new[] { x, [1, 2, 3] };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInitializer4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        var c = new[] { new[] { 1, 2, 3 } };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedInitializer5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        int[][] c = [|[|new|][]|] { new[] { 1, 2, 3 } };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x, bool b)
                    {
                        int[][] c = [new[] { 1, 2, 3 }];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedArgument1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        X([|[|new|] int[]|] { 1, 2, 3 });
                    }

                    void X(int[] x) { }
                }
                """,
            FixedCode = """
                class C
                {
                    void M()
                    {
                        X([1, 2, 3]);
                    }

                    void X(int[] x) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotTargetTypedArgument2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System.Collections.Generic;
                class C
                {
                    void M()
                    {
                        X(new int[] { 1, 2, 3 });
                    }

                    void X(int[] x) { }
                    void X(List<int> x) { }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedAttributeArgument1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                [X([|[|new|] int[]|] { 1, 2, 3 })]
                class C
                {
                }

                public class XAttribute : System.Attribute
                {
                    public XAttribute(int[] values) { }
                }
                """,
            FixedCode = """
                [X([1, 2, 3])]
                class C
                {
                }
                
                public class XAttribute : System.Attribute
                {
                    public XAttribute(int[] values) { }
                }
                """,
            FixedState =
                {
                    // THis will tart working once https://github.com/dotnet/roslyn/issues/69133 is fixed.
                    ExpectedDiagnostics =
                    {
                        // /0/Test0.cs(1,4): error CS0182: An attribute argument must be a constant expression, typeof expression or array creation expression of an attribute parameter type
                        DiagnosticResult.CompilerError("CS0182").WithSpan(1, 4, 1, 13),
                    }
                },
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedReturn1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] M()
                    {
                        return [|[|new|] int[]|] { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    int[] M()
                    {
                        return [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAssignment1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x)
                    {
                        x = [|[|new|] int[]|] { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x)
                    {
                        x = [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestAssignment2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    public int[] X;

                    void M()
                    {
                        var v = new C
                        {
                            X = [|[|new|] int[]|] { 1, 2, 3 },
                        };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    public int[] X;
                
                    void M()
                    {
                        var v = new C
                        {
                            X = [1, 2, 3],
                        };
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestCoalesce1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M(int[] x)
                    {
                        var y = x ?? [|[|new|] int[]|] { 1, 2, 3 };
                    }
                }
                """,
            FixedCode = """
                class C
                {
                    void M(int[] x)
                    {
                        var y = x ?? [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithLinqLet()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;
                using System.Linq;

                class C
                {
                    void M(int[] x)
                    {
                        var y = from a in x
                                let b = new int[] { 1, 2, 3 }
                                select b;
                    }
                }
                """,
            LanguageVersion = LanguageVersionExtensions.CSharpNext,
        }.RunAsync();
    }
}
