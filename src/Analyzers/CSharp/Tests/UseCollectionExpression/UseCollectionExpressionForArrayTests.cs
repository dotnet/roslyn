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
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.UnitTests.UseCollectionExpression;

using VerifyCS = CSharpCodeFixVerifier<
    CSharpUseCollectionExpressionForArrayDiagnosticAnalyzer,
    CSharpUseCollectionExpressionForArrayCodeFixProvider>;

[Trait(Traits.Feature, Traits.Features.CodeActionsUseCollectionExpression)]
public class UseCollectionExpressionForArrayTests
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
                    object[] i =
                    [
                        ""
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithExplicitArray_ExplicitSize()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string[] i = [|[|new|] string[1]|] { "" };
                }
                """,
            FixedCode = """
                class C
                {
                    string[] i = [""];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithExplicitArray_MultiDimensionalArray_ExplicitSizes1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string[,] i = new string[1, 1];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithExplicitArray_MultiDimensionalArray_ExplicitSizes2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string[,] i = new string[1, 1] { { "" } };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestWithExplicitArray_MultiDimensionalArray_ImplicitSizes1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    string[,] i = new string[,] { { "" } };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
                    string[] i =
                    [
                        ""
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
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
                    string[] i = {|CS0826:new[] { }|};
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestTargetTypedToComplexCast2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        var c = {|CS0030:(int)new int[] { 1 }|};
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            FixedState =
            {
                ExpectedDiagnostics =
                {
                    // /0/Test0.cs(5,22): info IDE0300: Collection initialization can be simplified
                    VerifyCS.Diagnostic().WithSpan(5, 22, 5, 25).WithSpan(5, 22, 5, 39).WithSeverity(DiagnosticSeverity.Info),
                    // /0/Test0.cs(5,22): hidden IDE0300: Collection initialization can be simplified
                    VerifyCS.Diagnostic().WithSpan(5, 22, 5, 27).WithSpan(5, 22, 5, 39).WithSpan(5, 22, 5, 27).WithSeverity(DiagnosticSeverity.Hidden),
                }
            },
            LanguageVersion = LanguageVersion.CSharp12,
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting1()
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                    [|{|] 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting3()
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                    [|{|]
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                        [|{|] 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                        [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting6()
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting7()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                        [|{|]
                            1, 2, 3
                        };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                        [
                            1, 2, 3
                        ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting8()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =

                        [|{|]
                            1, 2, 3
                        };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =

                        [
                            1, 2, 3
                        ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting1_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|] int[]|] { 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting2_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                    [|[|new|] int[]|] { 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting3_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|] int[]|] {
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting4_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|] int[]|]
                    {
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting5_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                    [|[|new|] int[]|]
                    {
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting6_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                    [|[|new|] int[]|] {
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting7_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                        [|[|new|] int[]|]
                        {
                            1, 2, 3
                        };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                        [
                            1, 2, 3
                        ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting8_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                        [|[|new|] int[]|] {
                            1, 2, 3
                        };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                        [
                            1, 2, 3
                        ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting9_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|] int[]|] {
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting10_Explicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =

                    [|[|new|] int[]|]
                    {
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =

                    [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting1_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|][]|] { 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting2_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                    [|[|new|][]|] { 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting3_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|][]|] {
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting4_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|][]|]
                    {
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting5_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                    [|[|new|][]|]
                    {
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting6_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                    [|[|new|][]|] {
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting7_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                        [|[|new|][]|]
                        {
                            1, 2, 3
                        };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                        [
                            1, 2, 3
                        ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting8_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                        [|[|new|][]|] {
                            1, 2, 3
                        };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                        [
                            1, 2, 3
                        ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting9_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|][]|] {
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
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestInitializerFormatting10_Implicit()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =

                    [|[|new|][]|]
                    {
                        1, 2, 3
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =

                    [
                        1, 2, 3
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoMultiLineEvenWhenLongIfAllElementsAlreadyPresent()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                namespace N
                {
                    class WellKnownDiagnosticTags
                    {
                        public static string Telemetry, EditAndContinue, Unnecessary, NotConfigurable;
                    }

                    class C
                    {
                        private static readonly string s_enforceOnBuildNeverTag;
                        private static readonly string[] s_microsoftCustomTags = [|[|new|] string[]|] { WellKnownDiagnosticTags.Telemetry };
                        private static readonly string[] s_editAndContinueCustomTags = [|[|new|] string[]|] { WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag };
                        private static readonly string[] s_unnecessaryCustomTags = [|[|new|] string[]|] { WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry };
                        private static readonly string[] s_notConfigurableCustomTags = [|[|new|] string[]|] { WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry };
                        private static readonly string[] s_unnecessaryAndNotConfigurableCustomTags = [|[|new|] string[]|] { WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry };
                    }
                }
                """,
            FixedCode = """
                using System;
                
                namespace N
                {
                    class WellKnownDiagnosticTags
                    {
                        public static string Telemetry, EditAndContinue, Unnecessary, NotConfigurable;
                    }
                
                    class C
                    {
                        private static readonly string s_enforceOnBuildNeverTag;
                        private static readonly string[] s_microsoftCustomTags = [WellKnownDiagnosticTags.Telemetry];
                        private static readonly string[] s_editAndContinueCustomTags = [WellKnownDiagnosticTags.EditAndContinue, WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag];
                        private static readonly string[] s_unnecessaryCustomTags = [WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.Telemetry];
                        private static readonly string[] s_notConfigurableCustomTags = [WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry];
                        private static readonly string[] s_unnecessaryAndNotConfigurableCustomTags = [WellKnownDiagnosticTags.Unnecessary, WellKnownDiagnosticTags.NotConfigurable, s_enforceOnBuildNeverTag, WellKnownDiagnosticTags.Telemetry];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_MultiLine1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = [|[|new|] int[1]|];
                        r[0] = 1 +
                            2;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r =
                        [
                            1 +
                                2,
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_MultiLine2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = [|[|new|] int[2]|];
                        r[0] = 1 +
                            2;
                        r[1] = 3 +
                            4;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r =
                        [
                            1 +
                                2,
                            3 +
                                4,
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ZeroSize()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        int[] r = [|[|new|] int[0]|];
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M()
                    {
                        int[] r = [];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_NotEnoughFollowingStatements()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        int[] r = new int[1];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_WrongFollowingStatement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        int[] r = new int[1];
                        return;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_NotLocalStatementInitializer()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        int[] r = Goo(new int[1]);
                        r[0] = 1;
                    }

                    int[] Goo(int[] input) => default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ExpressionStatementNotAssignment()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i)
                    {
                        int[] r = new int[1];
                        i++;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_AssignmentNotElementAccess()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = new int[1];
                        i = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ElementAccessNotToIdentifier()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    static int[] array;

                    void M(int i, int j)
                    {
                        int[] r = new int[1];
                        C.array[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_IdentifierNotEqualToVariableName()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    static int[] array;

                    void M(int i, int j)
                    {
                        int[] r = new int[1];
                        array[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ArgumentNotConstant()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = new int[1];
                        r[i] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ConstantArgumentNotCorrect1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = new int[1];
                        r[1] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_ConstantArgumentNotCorrect2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = new int[2];
                        r[0] = i;
                        r[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_OneElement()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = [|[|new|] int[1]|];
                        r[0] = i;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = [i];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_OneElement_MultipleFollowingStatements()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = [|[|new|] int[1]|];
                        r[0] = i;
                        r[0] = j;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = [i];
                        r[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = [|[|new|] int[2]|];
                        r[0] = i;
                        r[1] = j;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = [i, j];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2_Constant()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        const int v = 1;
                        int[] r = [|[|new|] int[2]|];
                        r[0] = i;
                        r[v] = j;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        const int v = 1;
                        int[] r = [i, j];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2_SecondWrongIndex()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[] r = new int[2];
                        r[0] = i;
                        r[0] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2_SecondNonConstant()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        var v = 1;
                        int[] r = new int[2];
                        r[0] = i;
                        r[v] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement2_SecondWrongDestination()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    static int[] array;

                    void M(int i, int j)
                    {
                        var v = 1;
                        int[] r = new int[2];
                        r[0] = i;
                        array[1] = j;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement_TwoDimensional1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[][] r = [|[|new|] int[2][]|];
                        r[0] = null;
                        r[1] = null;
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[][] r = [null, null];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement_TwoDimensional2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[][] r = [|[|new|] int[2][]|];
                        r[0] = [|[|new|][]|] { 1 };
                        r[1] = [|[|new|] int[]|] { 2 };
                    }
                }
                """,
            FixedCode = """
                using System;
                
                class C
                {
                    void M(int i, int j)
                    {
                        int[][] r = [[1], [2]];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement_TwoDimensional2_Trivia1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[][] r = [|[|new|] int[2][]|];

                        // Leading
                        r[0] = [|[|new|][]|] { 1 }; // Trailing
                        r[1] = [|[|new|] int[]|] { 2 };
                    }
                }
                """,
            FixedCode = """
                using System;
                
                class C
                {
                    void M(int i, int j)
                    {
                        int[][] r =
                        [
                            // Leading
                            [1], // Trailing
                            [2],
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoInitializer_TwoElement_TwoDimensional2_Trivia2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        int[][] r = [|[|new|] int[2][]|];

                        r[0] = [|[|new|][]|]
                        {
                            // Leading
                            1 // Trailing
                        };
                        r[1] = [|[|new|] int[]|] { 2 };
                    }
                }
                """,
            FixedCode = """
                using System;
                
                class C
                {
                    void M(int i, int j)
                    {
                        int[][] r =
                        [
                            [
                                // Leading
                                1 // Trailing
                            ],
                            [2],
                        ];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestGlobalStatement1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                int[] i = [|{|] 1, 2, 3 };
                """,
            FixedCode = """
                int[] i = [1, 2, 3];
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();
    }

    [Fact]
    public async Task TestGlobalStatement2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                int[] i =
                [|{|]
                    1,
                    2,
                    3,
                };
                """,
            FixedCode = """
                int[] i =
                [
                    1,
                    2,
                    3,
                ];
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public async Task TestWithDifferentNewLines(string endOfLine)
    {
        await new VerifyCS.Test
        {
            TestCode = """
                int[] i =
                [|{|]
                    1,
                    2,
                    3,
                };
                """.ReplaceLineEndings(endOfLine),
            FixedCode = """
                int[] i =
                [
                    1,
                    2,
                    3,
                ];
                """.ReplaceLineEndings(endOfLine),
            LanguageVersion = LanguageVersion.CSharp12,
            TestState =
            {
                OutputKind = OutputKind.ConsoleApplication,
            },
        }.RunAsync();
    }
}
