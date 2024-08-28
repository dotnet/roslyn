// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    public async Task TestNotWithIncompatibleExplicitArrays_Strict()
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
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
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
                    object[] i = [|[|new|] string[]|] { "" };
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
                    object[] i = [|[|new|][]|] { "" };
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
    public async Task TestNotWithIncompatibleImplicitArrays_Strict()
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
            EditorConfig = """
                [*]
                dotnet_style_prefer_collection_expression=when_types_exactly_match
                """
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
         [CombinatorialValues(["var", "object", "dynamic"])] string type)
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
        [CombinatorialValues(["var", "object", "dynamic"])] string type)
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
        [CombinatorialValues(["var", "object", "dynamic"])] string type)
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
        [CombinatorialValues(["var", "object", "dynamic"])] string type)
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
    public async Task TestNotWithNaturalType1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        var c = (object)new int[] { 1 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNaturalType2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    void M()
                    {
                        object c = new int[] { 1 };
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72337")]
    public async Task TestTargetTypedArgumentPrimaryConstructor1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                       class C(int[] x);
                       class C2() : C([|[|new|] int[]|] { 1, 2, 3 });
                       """,
            FixedCode = """
                        class C(int[] x);
                        class C2() : C([1, 2, 3]);
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

    public static readonly IEnumerable<object[]> EmptyOrConstantsOnly = new object[][]
    {
        new [] { "[|[|new|] int[0]|]", "[]" },
        new [] { "[|[|new|] int[]|] { }", "[]" },
        new [] { "[|[|new|] int[]|] { 1, 2, 3 }", "[1, 2, 3]" },
        new [] { "[|[|new|][]|] { 1, 2, 3 }", "[1, 2, 3]" },
    };

    [Theory, MemberData(nameof(EmptyOrConstantsOnly))]
    public async Task TestForSpanField(string expression, string expected)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                ref struct C
                {
                    private ReadOnlySpan<int> span = {{expression}};

                    public C() { }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                ref struct C
                {
                    private ReadOnlySpan<int> span = {{expected}};
                
                    public C() { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForSpanField2()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                ref struct C
                {
                    private static readonly int i = 0;
                    private ReadOnlySpan<int> span = new int[] { i };

                    public C() { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Theory, MemberData(nameof(EmptyOrConstantsOnly))]
    public async Task TestForSpanProperty1(string expression, string expected)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span => {{expression}};
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span => {{expected}};
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Theory, MemberData(nameof(EmptyOrConstantsOnly))]
    public async Task TestForSpanProperty2(string expression, string expected)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span { get => {{expression}}; }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span { get => {{expected}}; }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Theory, MemberData(nameof(EmptyOrConstantsOnly))]
    public async Task TestForSpanProperty3(string expression, string expected)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span { get { return {{expression}}; } }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span { get { return {{expected}}; } }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForSpanProperty4()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private static readonly int i = 1;
                    private ReadOnlySpan<int> Span => new int[] { i };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Theory, MemberData(nameof(EmptyOrConstantsOnly))]
    public async Task TestForMethodReturn(string expression, string expected)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span() => {{expression}};
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private ReadOnlySpan<int> Span() => {{expected}};
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestForSpanMethodReturn2()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    private static readonly int i = 1;
                    private ReadOnlySpan<int> Span() => new int[] { i };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Theory, MemberData(nameof(EmptyOrConstantsOnly))]
    public async Task TestForMethodLocal1(string expression, string expected)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> span = {{expression}};
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        ReadOnlySpan<int> span = {{expected}};
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Theory, MemberData(nameof(EmptyOrConstantsOnly))]
    public async Task TestForArgument(string expression, string expected)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        X({{expression}});
                    }

                    void X(ReadOnlySpan<int> span) { }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M()
                    {
                        X({{expected}});
                    }
                
                    void X(ReadOnlySpan<int> span) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan1()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan2()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        ReadOnlySpan<int> t;
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        ReadOnlySpan<int> t;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan3()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    int[] globalArray;

                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        ReadOnlySpan<int> t = globalArray;
                        return t;
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    int[] globalArray;

                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        ReadOnlySpan<int> t = globalArray;
                        return t;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan4()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        ReadOnlySpan<int> t = s;
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        ReadOnlySpan<int> t = s;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan5()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        X(s);
                    }

                    void X(ReadOnlySpan<int> span) { }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        X(s);
                    }
                
                    void X(ReadOnlySpan<int> span) { }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan6()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        return X(s);
                    }

                    ReadOnlySpan<int> X(scoped ReadOnlySpan<int> span) => default;
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        return X(s);
                    }
                
                    ReadOnlySpan<int> X(scoped ReadOnlySpan<int> span) => default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan7()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        s.Slice(0, 1);
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        s.Slice(0, 1);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan7_A()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    string M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        return s.ToString();
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    string M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        return s.ToString();
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan8()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        return X(s.Slice(0, 1));
                    }
                
                    ReadOnlySpan<int> X(scoped ReadOnlySpan<int> span) => default;
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        return X(s.Slice(0, 1));
                    }
                
                    ReadOnlySpan<int> X(scoped ReadOnlySpan<int> span) => default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan9()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        return X(s[0..1]);
                    }
                
                    ReadOnlySpan<int> X(scoped ReadOnlySpan<int> span) => default;
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        return X(s[0..1]);
                    }
                
                    ReadOnlySpan<int> X(scoped ReadOnlySpan<int> span) => default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan9_A()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    int M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        return s[0];
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    int M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        return s[0];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan10()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    int M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        return s.Length;
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    int M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        return s.Length;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan11()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    int M(int i)
                    {
                        ReadOnlySpan<int> s = [|[|new|][]|] { i };
                        return nameof(s).Length;
                    }
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    int M(int i)
                    {
                        ReadOnlySpan<int> s = [i];
                        return nameof(s).Length;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan12()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> s = new[] { i };
                        return s;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan13()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        return new[] { i };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan13_A()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                        => new[] { i };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan14()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        return X(new[] { i });
                    }

                    ReadOnlySpan<int> X(ReadOnlySpan<int> y) => y;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan14_A()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                        => X(new[] { i });

                    ReadOnlySpan<int> X(ReadOnlySpan<int> y) => y;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan15()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        return X(new[] { i });
                    }

                    ReadOnlySpan<int> X(ReadOnlySpan<int> y) => default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan15_A()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                        => X(new[] { i });

                    ReadOnlySpan<int> X(ReadOnlySpan<int> y) => default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan16()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = new[] { i };
                        return span.Slice(0, 1);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan17()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = new[] { i };
                        return span[0..1];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan18()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = new[] { i };
                        ReadOnlySpan<int> t = span;
                        return t;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan19()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = new[] { i };
                        ReadOnlySpan<int> t = span;
                        return t.Slice(0, 1);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan20()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = new[] { i };
                        X(span, out var v);
                        return v;
                    }

                    void X(ReadOnlySpan<int> s, out ReadOnlySpan<int> t) => t = s;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan21()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = new[] { i };
                        X(span, out var v);
                        return v;
                    }

                    void X(ReadOnlySpan<int> s, out ReadOnlySpan<int> t) => t = default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan21_A()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = new[] { i };
                        X(span, out var v);
                        return v.Slice(0, 1);
                    }

                    void X(ReadOnlySpan<int> s, out ReadOnlySpan<int> t) => t = default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan22()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = [|[|new|][]|] { i };
                        X(span, out var v);
                        return v;
                    }

                    void X(scoped ReadOnlySpan<int> s, out ReadOnlySpan<int> t) => t = default;
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = [i];
                        X(span, out var v);
                        return v;
                    }

                    void X(scoped ReadOnlySpan<int> s, out ReadOnlySpan<int> t) => t = default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan23()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> span = [|[|new|][]|] { i };
                        X(span, out var v);
                    }

                    void X(ReadOnlySpan<int> s, out ReadOnlySpan<int> t) => t = default;
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    void M(int i)
                    {
                        ReadOnlySpan<int> span = [i];
                        X(span, out var v);
                    }

                    void X(ReadOnlySpan<int> s, out ReadOnlySpan<int> t) => t = default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestLocalSpan24()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = [|[|new|][]|] { i };
                        X(span, out var v);
                        return v.Slice(0, 1);
                    }

                    void X(scoped ReadOnlySpan<int> s, out ReadOnlySpan<int> t) => t = default;
                }
                """,
            FixedCode = $$"""
                using System;
                using System.Linq;
                using System.Collections.Generic;
                
                class C
                {
                    ReadOnlySpan<int> M(int i)
                    {
                        ReadOnlySpan<int> span = [i];
                        X(span, out var v);
                        return v.Slice(0, 1);
                    }

                    void X(scoped ReadOnlySpan<int> s, out ReadOnlySpan<int> t) => t = default;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixAllConditionalExpression1()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class C
                {
                    void M(bool b)
                    {
                        var v = b ? [|[|new|][]|] { "a" } : [|[|new|][]|] { "b" };
                    }
                }
                """,
            FixedCode = $$"""
                class C
                {
                    void M(bool b)
                    {
                        var v = b ? ["a"] : new[] { "b" };
                    }
                }
                """,
            BatchFixedCode = $$"""
                class C
                {
                    void M(bool b)
                    {
                        var v = b ? new[] { "a" } : ["b"];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixAllConditionalExpression2()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class C
                {
                    void M(bool b)
                    {
                        var v = b ? [|[|new|] string[]|] { "a" } : [|[|new|] string[]|] { "b" };
                    }
                }
                """,
            FixedCode = $$"""
                class C
                {
                    void M(bool b)
                    {
                        var v = b ? ["a"] : new string[] { "b" };
                    }
                }
                """,
            BatchFixedCode = $$"""
                class C
                {
                    void M(bool b)
                    {
                        var v = b ? new string[] { "a" } : ["b"];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestSelfReference()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(int i, int j)
                    {
                        object[] r = new object[1];
                        r[0] = r;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public async Task TestInLambda()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Func<int[]> f = () => [|[|new|] int[]|] { 1, 2, 3 };
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Func<int[]> f = () => [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public async Task TestNotInLambda1()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        var f = () => new int[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/71012")]
    public async Task TestNotInExpressionTree()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    void M()
                    {
                        Expression<Func<int[]>> f = () => new int[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70998")]
    public async Task ForMismatchedTupleNames1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    (int A, int B)[] M()
                    {
                        return [|[|new|][]|] { (A: 1, 2) };
                    }
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    (int A, int B)[] M()
                    {
                        return [(A: 1, 2)];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70998")]
    public async Task ForMismatchedTupleNames2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    (int A, int B)[] M = [|{|] (A: 1, 2) };
                }
                """,
            FixedCode = """
                using System;

                class C
                {
                    (int A, int B)[] M = [(A: 1, 2)];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70816")]
    public async Task NotWithInvalidNumericInference()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M()
                    {
                        byte[] a = [1, 2];
                        a.AsSpan().SequenceEqual(new byte[] { 0, 1 });
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact]
    public async Task TestFixAllImplicitArray1()
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                class C
                {
                    void M(bool b)
                    {
                        object falsePositive = new[] { [|[|new|][]|] { 1 }, [|[|new|][]|] { 1 } };
                    }
                }
                """,
            // Fixing just validates each fix in order, iteratively.  After the first item is fixed, the second can't be.
            FixedCode = $$"""
                class C
                {
                    void M(bool b)
                    {
                        object falsePositive = new[] { [1], new[] { 1 } };
                    }
                }
                """,
            // Batch fixing runs the fixer against all diagnostics at once.  That fixer goes from innermost (lowest) to
            // highest. So we end up fixing the second.  After that one is fixed, the first can't be.
            BatchFixedCode = $$"""
                class C
                {
                    void M(bool b)
                    {
                        object falsePositive = new[] { new[] { 1 }, [1] };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public async Task TestInterfaceOn()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    IEnumerable<int> M()
                    {
                        return [|[|new|] int[]|] { 1, 2, 3 };
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    IEnumerable<int> M()
                    {
                        return [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public async Task TestInterfaceOn_ReadWriteDestination()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    IList<int> M()
                    {
                        return [|[|new|] int[]|] { 1, 2, 3 };
                    }
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    IList<int> M()
                    {
                        return [1, 2, 3];
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/70996")]
    public async Task TestInterfaceOff()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    IEnumerable<int> M()
                    {
                        return new int[] { 1, 2, 3 };
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

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/71522")]
    public async Task TestTargetTypedConditional1(
        [CombinatorialValues("", "#nullable enable")] string nullable)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                {{nullable}}

                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    void M(string[] args)
                    {
                        int value = 3;
                        M(value is 1 ? ["1"] : [|[|new|][]|] { "4" });
                    }
                }
                """,
            FixedCode = $$"""
                {{nullable}}

                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    void M(string[] args)
                    {
                        int value = 3;
                        M(value is 1 ? ["1"] : ["4"]);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/71522")]
    public async Task TestTargetTypedConditional2(
        [CombinatorialValues("", "#nullable enable")] string nullable)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                {{nullable}}

                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                int value = 3;
                M(value is 1 ? ["1"] : [|[|new|][]|] { "4" });

                static void M(string[] args) { }
                """,
            FixedCode = $$"""
                {{nullable}}

                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                int value = 3;
                M(value is 1 ? ["1"] : ["4"]);

                static void M(string[] args) { }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState = { OutputKind = OutputKind.ConsoleApplication }
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/71522")]
    public async Task TestTargetTypedConditional3(
        [CombinatorialValues("", "#nullable enable")] string nullable)
    {
        await new VerifyCS.Test
        {
            TestCode = $$"""
                {{nullable}}

                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                int value = 3;
                M(value is 1 ? [|[|new|][]|] { "1" } :
                  value is 2 ? [|[|new|][]|] { "2" } :
                  value is 3 ? [|[|new|][]|] { "3" } :
                               [|[|new|][]|] { "4" });

                static void M(string[] args) { }
                """,
            FixedCode = $$"""
                {{nullable}}

                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                int value = 3;
                M(value is 1 ? ["1"] :
                  value is 2 ? ["2"] :
                  value is 3 ? ["3"] :
                               ["4"]);

                static void M(string[] args) { }
                """,
            BatchFixedCode = $$"""
                {{nullable}}

                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                int value = 3;
                M(value is 1 ? ["1"] :
                  value is 2 ? ["2"] :
                  value is 3 ? ["3"] :
                               ["4"]);

                static void M(string[] args) { }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            TestState = { OutputKind = OutputKind.ConsoleApplication }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNullableArrays1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                #nullable disable

                using System;

                class C
                {
                    void M()
                    {
                        GetActiveStatementDebugInfosCSharp(
                            [|[|new|][]|] { GetGeneratedCodeFromMarkedSource() },
                            filePaths: [|[|new|][]|] { this.filePath });
                    }

                #nullable enable

                    public static void GetActiveStatementDebugInfosCSharp(
                        string[] markedSources,
                        string[]? filePaths = null,
                        int[]? methodRowIds = null,
                        Guid[]? modules = null,
                        int[]? methodVersions = null,
                        int[]? ilOffsets = null)
                    {
                    }

                    private static string GetGeneratedCodeFromMarkedSource() => "";

                    private string? filePath;
                }
                """,
            FixedCode = """
                #nullable disable
                
                using System;
                
                class C
                {
                    void M()
                    {
                        GetActiveStatementDebugInfosCSharp(
                            [GetGeneratedCodeFromMarkedSource()],
                            filePaths: [this.filePath]);
                    }
                
                #nullable enable
                
                    public static void GetActiveStatementDebugInfosCSharp(
                        string[] markedSources,
                        string[]? filePaths = null,
                        int[]? methodRowIds = null,
                        Guid[]? modules = null,
                        int[]? methodVersions = null,
                        int[]? ilOffsets = null)
                    {
                    }
                
                    private static string GetGeneratedCodeFromMarkedSource() => "";
                
                    private string? filePath;
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultiDimensionalArray1()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly IEnumerable<object[]> EmptyOrConstantsOnly = [|[|new|] object[][]|]
                    {
                        [|[|new|][]|] { "[]", "[]" },
                        [|[|new|][]|] { "[]", "[]" },
                    };
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly IEnumerable<object[]> EmptyOrConstantsOnly =
                    [
                        ["[]", "[]"],
                        ["[]", "[]"],
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultiDimensionalArray1_Strict()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly IEnumerable<object[]> EmptyOrConstantsOnly = new object[][]
                    {
                        new[] { "[]", "[]" },
                        new[] { "[]", "[]" },
                    };
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
    public async Task TestMultiDimensionalArray2()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly IEnumerable<object[]> EmptyOrConstantsOnly =
                    [
                        [|[|new|][]|] { "[]", "[]" },
                        [|[|new|][]|] { "[]", "[]" },
                    ];
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly IEnumerable<object[]> EmptyOrConstantsOnly =
                    [
                        ["[]", "[]"],
                        ["[]", "[]"],
                    ];
                }
                """,
            BatchFixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly IEnumerable<object[]> EmptyOrConstantsOnly =
                    [
                        ["[]", "[]"],
                        ["[]", "[]"],
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultiDimensionalArray2_Strict()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly IEnumerable<object[]> EmptyOrConstantsOnly =
                    [
                        new[] { "[]", "[]" },
                        new[] { "[]", "[]" },
                    ];
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
    public async Task TestMultiDimensionalArray3()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly IEnumerable<IEnumerable<object>> EmptyOrConstantsOnly = [|[|new|] object[][]|]
                    {
                        [|[|new|][]|] { "[]", "[]" },
                        [|[|new|][]|] { "[]", "[]" },
                    };
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly IEnumerable<IEnumerable<object>> EmptyOrConstantsOnly =
                    [
                        ["[]", "[]"],
                        ["[]", "[]"],
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultiDimensionalArray3_Strict()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly IEnumerable<IEnumerable<object>> EmptyOrConstantsOnly = new object[][]
                    {
                        new[] { "[]", "[]" },
                        new[] { "[]", "[]" },
                    };
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
    public async Task TestMultiDimensionalArray4()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly IEnumerable<IEnumerable<object>> EmptyOrConstantsOnly = [|[|new|] object[][]|]
                    {
                        ["[]", "[]"],
                        ["[]", "[]"],
                    };
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly IEnumerable<IEnumerable<object>> EmptyOrConstantsOnly =
                    [
                        ["[]", "[]"],
                        ["[]", "[]"],
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultiDimensionalArray4_Strict()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly IEnumerable<IEnumerable<object>> EmptyOrConstantsOnly = new object[][]
                    {
                        ["[]", "[]"],
                        ["[]", "[]"],
                    };
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
    public async Task TestMultiDimensionalArray5()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly IEnumerable<object> EmptyOrConstantsOnly = [|[|new|] object[][]|]
                    {
                        [|[|new|][]|] { "[]", "[]" },
                        [|[|new|][]|] { "[]", "[]" },
                    };
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly IEnumerable<object> EmptyOrConstantsOnly =
                    [
                        new[] { "[]", "[]" },
                        new[] { "[]", "[]" },
                    ];
                }
                """,
            BatchFixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly IEnumerable<object> EmptyOrConstantsOnly = new object[][]
                    {
                        ["[]", "[]"],
                        ["[]", "[]"],
                    };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestMultiDimensionalArray6()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly object[] EmptyOrConstantsOnly = [|[|new|] object[][]|]
                    {
                        [|[|new|][]|] { "[]", "[]" },
                        [|[|new|][]|] { "[]", "[]" },
                    };
                }
                """,
            FixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly object[] EmptyOrConstantsOnly =
                    [
                        new[] { "[]", "[]" },
                        new[] { "[]", "[]" },
                    ];
                }
                """,
            BatchFixedCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;
                
                class C
                {
                    public static readonly object[] EmptyOrConstantsOnly = new object[][]
                    {
                        ["[]", "[]"],
                        ["[]", "[]"],
                    };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestArray1()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly object EmptyOrConstantsOnly = new object[]
                    {
                        ""
                    };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact]
    public async Task TestArray2()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public static readonly object EmptyOrConstantsOnly = new[]
                    {
                        ""
                    };
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72640")]
    public async Task TestDynamic1()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public void Test(dynamic obj)
                    {
                        obj.arr = new byte[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72640")]
    public async Task TestDynamic2()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public void Test(dynamic obj)
                    {
                        obj.arr = (new byte[] { 1, 2, 3 })!;
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72640")]
    public async Task TestDynamic3()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public void Test(dynamic obj)
                    {
                        obj = new byte[] { 1, 2, 3 };
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72640")]
    public async Task TestDynamic4()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public void Test(dynamic obj)
                    {
                        Test(new byte[] { 1, 2, 3 });
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72640")]
    public async Task TestDynamic5()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public void Test(dynamic obj)
                    {
                        Test((new byte[] { 1, 2, 3 })!);
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72640")]
    public async Task TestDynamic6()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public void Test(dynamic obj)
                    {
                        Test1(obj, [|[|new|] int?[]|] { 3 });
                    }

                    private void Test1(dynamic obj, params int?[][] args)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72640")]
    public async Task TestDynamic7()
    {
        await new VerifyCS.Test
        {
            TestCode =
                """
                using System;
                using System.Collections.Generic;
                using System.Linq.Expressions;

                class C
                {
                    public void Test(dynamic obj)
                    {
                        Test1(obj, [|[|new|] int?[]|] { 3 });
                    }

                    private void Test1(dynamic obj, int?[] args)
                    {
                    }
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer1()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|][]|] //Test
                    { 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    //Test
                    [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer2()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = //Test
                    [|{|] 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = //Test
                    [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer3()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[][][] a = //Other comment
                    [|{|]
                        [|[|new|] int[][]|] //my comment
                        {
                            [|[|new|] int[]|] { 123, 456 }
                        },
                        [|[|new|] int[][]|] //my comment 2
                        {
                            [|[|new|] int[]|] { 789, 101 }
                        }
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[][][] a = //Other comment
                    [
                        //my comment
                        [
                            [123, 456]
                        ],
                        //my comment 2
                        [
                            [789, 101]
                        ]
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer4()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|] int[]|] //Test
                    { };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = //Test
                    [];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer5()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = //Test
                    [|{|] };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = //Test
                    [];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer6()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i =
                    [|{|] };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    [];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer7()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|] int[]|]
                    { };
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer8()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|][]|] /* Test */
                    { 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i =
                    /* Test */
                    [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer9()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = /* Test */
                    [|{|] 1, 2, 3 };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = /* Test */
                    [1, 2, 3];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer10()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[][][] a = /* Other comment */
                    [|{|]
                        [|[|new|] int[][]|] /* my comment */
                        {
                            [|[|new|] int[]|] { 123, 456 }
                        },
                        [|[|new|] int[][]|] /* my comment 2 */
                        {
                            [|[|new|] int[]|] { 789, 101 }
                        }
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[][][] a = /* Other comment */
                    [
                        /* my comment */
                        [
                            [123, 456]
                        ],
                        /* my comment 2 */
                        [
                            [789, 101]
                        ]
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer11()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = [|[|new|] int[]|] /* Test */
                    { };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = /* Test */
                    [];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer12()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[] i = /* Test */
                    [|{|] };
                }
                """,
            FixedCode = """
                class C
                {
                    int[] i = /* Test */
                    [];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/73740")]
    public async Task PreservesTrailingTriviaAfterInitializer14()
    {
        await new VerifyCS.Test
        {
            TestCode = """
                class C
                {
                    int[][][] a = /* Other 
                    comment */
                    //other comment
                    [|{|]
                        [|[|new|] int[][]|] /* mixed comments */ // here
                        {
                            [|[|new|] int[]|] { 123, 456 } //with some trailing comments!
                        },
                    };
                }
                """,
            FixedCode = """
                class C
                {
                    int[][][] a = /* Other 
                    comment */
                    //other comment
                    [
                        /* mixed comments */ // here
                        [
                            [123, 456] //with some trailing comments!
                        ],
                    ];
                }
                """,
            LanguageVersion = LanguageVersion.CSharp12,
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/74931")]
    public async Task AllowSwitchToReadOnlySpanCSharp12(bool whenTypesLooselyMatch)
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(char c)
                    {
                        Split([|[|new|][]|] { c });
                    }

                    void Split(char[] p) { }
                    void Split(ReadOnlySpan<char> p) { }
                }
                """,
            FixedCode = """
                using System;
                
                class C
                {
                    void M(char c)
                    {
                        Split([c]);
                    }
                
                    void Split(char[] p) { }
                    void Split(ReadOnlySpan<char> p) { }
                }
                """,
            EditorConfig = $$"""
                [*]
                dotnet_style_prefer_collection_expression={{(whenTypesLooselyMatch ? "when_types_loosely_match" : "when_types_exactly_match")}}
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/74931")]
    public async Task AllowSwitchToReadOnlySpanCSharp13(bool whenTypesLooselyMatch)
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(char c)
                    {
                        Split([|[|new|][]|] { c });
                    }

                    void Split(char[] p) { }
                    void Split(ReadOnlySpan<char> p) { }
                }
                """,
            FixedCode = """
                using System;
                
                class C
                {
                    void M(char c)
                    {
                        Split([c]);
                    }
                
                    void Split(char[] p) { }
                    void Split(ReadOnlySpan<char> p) { }
                }
                """,
            EditorConfig = $$"""
                [*]
                dotnet_style_prefer_collection_expression={{(whenTypesLooselyMatch ? "when_types_loosely_match" : "when_types_exactly_match")}}
                """,
            LanguageVersion = LanguageVersion.CSharp13,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/74931")]
    public async Task AllowSwitchToReadOnlySpanGeneric(bool whenTypesLooselyMatch)
    {
        await new VerifyCS.Test
        {
            TestCode = """
                using System;

                class C
                {
                    void M(char c)
                    {
                        Split([|[|new|][]|] { c });
                    }

                    void Split<T>(T[] p) { }
                    void Split<T>(ReadOnlySpan<T> p) { }
                }
                """,
            FixedCode = """
                using System;
                
                class C
                {
                    void M(char c)
                    {
                        Split([c]);
                    }
                
                    void Split<T>(T[] p) { }
                    void Split<T>(ReadOnlySpan<T> p) { }
                }
                """,
            EditorConfig = $$"""
                [*]
                dotnet_style_prefer_collection_expression={{(whenTypesLooselyMatch ? "when_types_loosely_match" : "when_types_exactly_match")}}
                """,
            LanguageVersion = LanguageVersion.CSharp12,
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
        }.RunAsync();
    }
}
