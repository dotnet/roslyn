// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.IntroduceParameter;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.IntroduceParameter;

[Trait(Traits.Feature, Traits.Features.CodeActionsIntroduceParameter)]
public sealed class IntroduceParameterTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpIntroduceParameterCodeRefactoringProvider();

    protected override ImmutableArray<CodeAction> MassageActions(ImmutableArray<CodeAction> actions)
        => FlattenActions(actions);

    private OptionsCollection UseExpressionBody
        => Option(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CSharpCodeStyleOptions.WhenPossibleWithSuggestionEnforcement);

    [Fact]
    public async Task TestExpressionWithNoMethodCallsCase()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    int m = [|x * y * z;|]
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z, int m) 
                {
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestExpressionCaseWithLocal()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    int l = 5;
                    int m = [|l * y * z;|]
                }
            }
            """);
    }

    [Fact]
    public async Task TestBasicComplexExpressionCase()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(string x, int y, int z) 
                {
                    int m = [|x.Length * y * z|];
                }

                void M1(string y)
                {
                    M(y, 5, 2);
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(string x, int y, int z, int m) 
                {
                }

                void M1(string y)
                {
                    M(y, 5, 2, y.Length * 5 * 2);
                }
            }
            """, 0);
    }

    [Fact]
    public async Task TestExpressionCaseWithSingleMethodCall()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = [|x * z * z|];
                }

                void M1(int x, int y, int z) 
                {
                    M(z, x, z);
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z, int m)
                {
                }

                void M1(int x, int y, int z) 
                {
                    M(z, x, z, z * z * z);
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestLocalDeclarationMultipleDeclarators()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = [|x * z * z|], y = 0;
                }

                void M1(int x, int y, int z) 
                {
                    M(z, x, z);
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z, int v)
                {
                    int m = {|Rename:v|}, y = 0;
                }

                void M1(int x, int y, int z) 
                {
                    M(z, x, z, z * z * z);
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestDeclarationInForLoop()
    {
        await TestMissingAsync("""
            using System;
            class TestClass
            {
                void M(int a, int b)
                {
                    var v = () =>
                    {
                        for (var y = [|a * b|]; ;) { }
                    };
                }
            }
            """);
    }

    [Fact]
    public async Task TestExpressionCaseWithSingleMethodCallInLocalFunction()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = M2(x, z);

                    int M2(int x, int y)
                    {
                        int val = [|x * y|];
                        return val;
                    }
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = M2(x, z, x * z);

                    int M2(int x, int y, int val)
                    {
                        return val;
                    }
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestExpressionCaseWithSingleMethodCallInStaticLocalFunction()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = M2(x, z);

                    static int M2(int x, int y)
                    {
                        int val = [|x * y|];
                        return val;
                    }
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = M2(x, z, x * z);

                    static int M2(int x, int y, int val)
                    {
                        return val;
                    }
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestHighlightIncompleteExpressionCaseWithSingleMethodCall()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = 5 * [|x * y * z|];
                }

                void M1(int x, int y, int z) 
                {
                    M(z, y, x);
                }
            }
            """);
    }

    [Fact]
    public async Task TestExpressionCaseWithMultipleMethodCall()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = [|x * y * z|];
                }

                void M1(int x, int y, int z) 
                {
                    M(a + b, 5, x);
                    M(z, y, x);
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z, int m)
                {
                }

                void M1(int x, int y, int z) 
                {
                    M(a + b, 5, x, (a + b) * 5 * x);
                    M(z, y, x, z * y * x);
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestExpressionAllOccurrences()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = x * y * z;
                    int f = [|x * y * z|];
                }

                void M1(int x, int y, int z) 
                {
                    M(a + b, 5, x);
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z, int f)
                {
                    int m = f;
                }

                void M1(int x, int y, int z) 
                {
                    M(a + b, 5, x, (a + b) * 5 * x);
                }
            }
            """, index: 3);
    }

    [Fact]
    public async Task TestxpressionWithNoMethodCallTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    int m = [|x * y * z;|]
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(int x, int y, int z)
                {
                    return x * y * z;
                }

                void M(int x, int y, int z, int m) 
                {
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionWithSingleMethodCallTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = [|y * x|];
                }

                void M1(int x, int y, int z) 
                {
                    M(z, y, x);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(int x, int y)
                {
                    return y * x;
                }

                void M(int x, int y, int z, int m)
                {
                }

                void M1(int x, int y, int z) 
                {
                    M(z, y, x, GetM(z, y));
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionWithSingleMethodCallAndAccessorsTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = [|y * x|];
                }

                void M1(int x, int y, int z) 
                {
                    this.M(z, y, x);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(int x, int y)
                {
                    return y * x;
                }

                void M(int x, int y, int z, int m)
                {
                }

                void M1(int x, int y, int z) 
                {
                    this.M(z, y, x, GetM(z, y));
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionWithSingleMethodCallAndAccessorsConditionalTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = [|y * x|];
                }

                void M1(int x, int y, int z) 
                {
                    this?.M(z, y, x);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(int x, int y)
                {
                    return y * x;
                }

                void M(int x, int y, int z, int m)
                {
                }

                void M1(int x, int y, int z) 
                {
                    this?.M(z, y, x, this?.GetM(z, y));
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionWithSingleMethodCallMultipleAccessorsTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    A a = new A();
                    var age = a.Prop.ComputeAge(x, y);
                }
            }

            class A
            {
                public B Prop { get; set; }
            }
            class B
            {
                public int ComputeAge(int x, int y)
                {
                    var age = [|x + y|];
                    return age;
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    A a = new A();
                    var age = a.Prop.ComputeAge(x, y, a.Prop.GetAge(x, y));
                }
            }

            class A
            {
                public B Prop { get; set; }
            }
            class B
            {
                public int GetAge(int x, int y)
                {
                    return x + y;
                }

                public int ComputeAge(int x, int y, int age)
                {
                    return age;
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionWithSingleMethodCallMultipleAccessorsConditionalTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    A a = new A();
                    var age = a?.Prop?.ComputeAge(x, y);
                }
            }

            class A
            {
                public B Prop { get; set; }
            }
            class B
            {
                public int ComputeAge(int x, int y)
                {
                    var age = [|x + y|];
                    return age;
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    A a = new A();
                    var age = a?.Prop?.ComputeAge(x, y, a?.Prop?.GetAge(x, y));
                }
            }

            class A
            {
                public B Prop { get; set; }
            }
            class B
            {
                public int GetAge(int x, int y)
                {
                    return x + y;
                }

                public int ComputeAge(int x, int y, int age)
                {
                    return age;
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionWithSingleMethodCallAccessorsMixedConditionalTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    A a = new A();
                    var age = a.Prop?.ComputeAge(x, y);
                }
            }

            class A
            {
                public B Prop { get; set; }
            }
            class B
            {
                public int ComputeAge(int x, int y)
                {
                    var age = [|x + y|];
                    return age;
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    A a = new A();
                    var age = a.Prop?.ComputeAge(x, y, a.Prop?.GetAge(x, y));
                }
            }

            class A
            {
                public B Prop { get; set; }
            }
            class B
            {
                public int GetAge(int x, int y)
                {
                    return x + y;
                }

                public int ComputeAge(int x, int y, int age)
                {
                    return age;
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionWithSingleMethodCallTrampolineAllOccurrences()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = [|x * y * z|];
                    int l = x * y * z;
                }

                void M1(int x, int y, int z) 
                {
                    M(z, y, x);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(int x, int y, int z)
                {
                    return x * y * z;
                }

                void M(int x, int y, int z, int m)
                {
                    int l = m;
                }

                void M1(int x, int y, int z) 
                {
                    M(z, y, x, GetM(z, y, x));
                }
            }
            """, index: 4, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionWithNoMethodCallOverload()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z) 
                {
                    int m = [|x * y * z;|]
                }
            }
            """, """
            using System;
            class TestClass
            {
                private void M(int x, int y, int z)
                {
                    M(x, y, z, x * y * z);
                }

                void M(int x, int y, int z, int m) 
                {
                }
            }
            """, index: 2, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionWithSingleMethodCallOverload()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(int x, int y, int z)
                {
                    int m = [|x * y * z|];
                }

                void M1(int x, int y, int z) 
                {
                    M(z, y, x);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private void M(int x, int y, int z)
                {
                    M(x, y, z, x * y * z);
                }

                void M(int x, int y, int z, int m)
                {
                }

                void M1(int x, int y, int z) 
                {
                    M(z, y, x);
                }
            }
            """, index: 2, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionBodiedMemberOverload()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y, int z) => [|x * y * z|];

                void M1(int x, int y, int z)
                {
                    int prod = M(z, y, x);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int M(int x, int y, int z) => M(x, y, z, x * y * z);
                int M(int x, int y, int z, int v) => {|Rename:v|};

                void M1(int x, int y, int z)
                {
                    int prod = M(z, y, x);
                }
            }
            """, index: 2, options: UseExpressionBody, parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionBodiedMemberTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y, int z) => [|x * y * z|];

                void M1(int x, int y, int z)
                {
                    int prod = M(z, y, x);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetV(int x, int y, int z)
                {
                    return x * y * z;
                }

                int M(int x, int y, int z, int v) => {|Rename:v|};

                void M1(int x, int y, int z)
                {
                    int prod = M(z, y, x, GetV(z, y, x));
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionCaseWithRecursiveCall()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y, int z)
                {
                    int m = [|x * y * z|];
                    return M(x, x, z);
                }
            }
            """, """
            using System;
            class TestClass
            {
                int M(int x, int y, int z, int m)
                {
                    return M(x, x, z, x * x * z);
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestExpressionCaseWithNestedRecursiveCall()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y, int z)
                {
                    int m = [|x * y * z|];
                    return M(x, x, M(x, y, z));
                }
            }
            """, """
            using System;
            class TestClass
            {
                int M(int x, int y, int z, int m)
                {
                    return M(x, x, M(x, y, z, x * y * z), x * x * M(x, y, z, x * y * z));
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestExpressionCaseWithParamsArg()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(params int[] args)
                {
                    int m = [|args[0] + args[1]|];
                    return m;
                }

                void M1()
                {
                    M(5, 6, 7);
                }
            }
            """);
    }

    [Fact]
    public async Task TestExpressionCaseWithOptionalParameters()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y = 5)
                {
                    int m = [|x * y|];
                    return m;
                }

                void M1()
                {
                    M(5, 3);
                }
            }
            """, """
            using System;
            class TestClass
            {
                int M(int x, int m, int y = 5)
                {
                    return m;
                }

                void M1()
                {
                    M(5, 5 * 3, 3);
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestExpressionCaseWithOptionalParametersUsed()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y = 5)
                {
                    int m = [|x * y|];
                    return m;
                }

                void M1()
                {
                    M(7);
                }
            }
            """, """
            using System;
            class TestClass
            {
                int M(int x, int m, int y = 5)
                {
                    return m;
                }

                void M1()
                {
                    M(7, 7 * 5);
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestExpressionCaseWithOptionalParametersUsedOverload()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y = 5)
                {
                    int m = [|x * y|];
                    return m;
                }

                void M1()
                {
                    M(7);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int M(int x, int y = 5)
                {
                    return M(x, x * y, y);
                }

                int M(int x, int m, int y = 5)
                {
                    return m;
                }

                void M1()
                {
                    M(7);
                }
            }
            """, index: 2);
    }

    [Fact]
    public async Task TestExpressionCaseWithOptionalParametersUsedTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y = 5)
                {
                    int m = [|x * y|];
                    return m;
                }

                void M1()
                {
                    M(7);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(int x, int y = 5)
                {
                    return x * y;
                }

                int M(int x, int m, int y = 5)
                {
                    return m;
                }

                void M1()
                {
                    M(7, GetM(7));
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task TestExpressionCaseWithOptionalParametersUnusedTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y = 5)
                {
                    int m = [|x * y|];
                    return m;
                }

                void M1()
                {
                    M(7, 2);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(int x, int y = 5)
                {
                    return x * y;
                }

                int M(int x, int m, int y = 5)
                {
                    return m;
                }

                void M1()
                {
                    M(7, GetM(7, 2), 2);
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task TestExpressionCaseWithCancellationToken()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            using System.Threading;
            class TestClass
            {
                int M(int x, CancellationToken cancellationToken)
                {
                    int m = [|x * x|];
                    return m;
                }

                void M1(CancellationToken cancellationToken)
                {
                    M(7, cancellationToken);
                }
            }
            """, """
            using System;
            using System.Threading;
            class TestClass
            {
                int M(int x, int m, CancellationToken cancellationToken)
                {
                    return m;
                }

                void M1(CancellationToken cancellationToken)
                {
                    M(7, 7 * 7, cancellationToken);
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestExpressionCaseWithRecursiveCallTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y, int z)
                {
                    int m = [|x * y * z|];
                    return M(x, x, z);
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(int x, int y, int z)
                {
                    return x * y * z;
                }

                int M(int x, int y, int z, int m)
                {
                    return M(x, x, z, GetM(x, x, z));
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionCaseWithNestedRecursiveCallTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y, int z)
                {
                    int m = [|x * y * z|];
                    return M(x, x, M(x, y, x));
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(int x, int y, int z)
                {
                    return x * y * z;
                }

                int M(int x, int y, int z, int m)
                {
                    return M(x, x, M(x, y, x, GetM(x, y, x)), GetM(x, x, M(x, y, x, GetM(x, y, x))));
                }
            }
            """, index: 1, options: new OptionsCollection(GetLanguage()), parseOptions: CSharpParseOptions.Default);
    }

    [Fact]
    public async Task TestExpressionCaseInConstructor()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                public TestClass(int x, int y)
                {
                    Math.Max([|x + y|], x * y);
                }

                void TestMethod()
                {
                    var test = new TestClass(5, 6);
                }
            }
            """, """
            using System;
            class TestClass
            {
                public TestClass(int x, int y, int val1)
                {
                    Math.Max({|Rename:val1|}, x * y);
                }

                void TestMethod()
                {
                    var test = new TestClass(5, 6, 5 + 6);
                }
            }
            """, 0);
    }

    [Fact]
    public async Task TestLambdaCaseNormal()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                Func<int, int, int> mult = (x, y) => [|x * y|];
            }
            """);
    }

    [Fact]
    public async Task TestLambdaCaseTrampoline()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                Func<int, int, int> mult = (x, y) => [|x * y|];
            }
            """);
    }

    [Fact]
    public async Task TestLambdaCaseOverload()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                Func<int, int, int> mult = (x, y) => [|x * y|];
            }
            """);
    }

    [Fact]
    public async Task TestTopLevelStatements()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            Math.Max(5 + 5, [|6 + 7|]);
            """);
    }

    [Fact]
    public async Task TestFieldInitializer()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int a = [|5 + 3|];
            }
            """);
    }

    [Fact]
    public async Task TestIndexer()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class SampleCollection<T>
            {
                private T[] arr = new T[100];

                public T this[int i] => arr[[|i + 5|]];
            }
            """);
    }

    [Fact]
    public async Task TestPropertyGetter()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;

            class TimePeriod
            {
               private double _seconds;

               public double Hours
               {
                   get { return [|_seconds / 3600|]; }
               }
            }
            """);
    }

    [Fact]
    public async Task TestPropertySetter()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;

            class TimePeriod
            {
               private double _seconds;

               public double Hours
               {
                   set {
                      _seconds = [|value * 3600|];
                    }
                }
            }
            """);
    }

    [Fact]
    public async Task TestDestructor()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                public ~TestClass()
                {
                    Math.Max([|5 + 5|], 5 * 5);
                }
            }
            """);
    }

    [Fact]
    public async Task TestExpressionInParameter()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                public void M(int x = [|5 * 5|])
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestCrossLanguageInvocations()
    {
        await TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language= "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public class Program
            {
                public Program() {}

                public int M(int x, int y)
                {
                    int m = [|x * y|];
                    return m;
                }

                void M1()
                {
                    M(7, 2);
                }
            }
                    </Document>
                </Project>

            <Project Language= "Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                    <ProjectReference>Assembly1</ProjectReference>
                    <Document>
            Class ProgramVB
                Sub M2()
                    Dim programC = New Program()
                    programC.M(7, 2)
                End Sub
            End Class
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language= "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            public class Program
            {
                public Program() {}

                public int M(int x, int y, int m)
                {
                    return m;
                }

                void M1()
                {
                    M(7, 2, 7 * 2);
                }
            }
                    </Document>
                </Project>

            <Project Language= "Visual Basic" AssemblyName="Assembly2" CommonReferences="true">
                    <ProjectReference>Assembly1</ProjectReference>
                    <Document>
            Class ProgramVB
                Sub M2()
                    Dim programC = New Program()
                    programC.M(7, 2)
                End Sub
            End Class
                    </Document>
                </Project>
            </Workspace>
            """, 0);
    }

    [Fact]
    public async Task TestConvertedTypeInExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(double x, double y) 
                {
                    int m = [|(int)(x * y);|]
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(double x, double y, int m) 
                {
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestConvertedTypeInExpressionTrampoline()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M(double x, double y) 
                {
                    int m = [|(int)(x * y);|]
                }
            }
            """, """
            using System;
            class TestClass
            {
                private int GetM(double x, double y)
                {
                    return (int)(x * y);
                }

                void M(double x, double y, int m) 
                {
                }
            }
            """, index: 1);
    }

    [Fact]
    public async Task TestThisKeywordInExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                public int M1()
                {
                    return 5;
                }

                public int M(int x, int y)
                {
                    int m = [|x * this.M1();|]
                    return m;
                }
            }
            """, """
            using System;
            class TestClass
            {
                public int M1()
                {
                    return 5;
                }

                public int GetM(int x)
                {
                    return x * this.M1();
                }

                public int M(int x, int y, int m)
                {
                    return m;
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestThisImplicitInExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                public int M1()
                {
                    return 5;
                }

                public int M(int x, int y)
                {
                    int m = [|x * M1();|]
                    return m;
                }
            }
            """, """
            using System;
            class TestClass
            {
                public int M1()
                {
                    return 5;
                }

                public int GetM(int x)
                {
                    return x * M1();
                }

                public int M(int x, int y, int m)
                {
                    return m;
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestStaticMethodCallInExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                public static int M1()
                {
                    return 5;
                }

                public int M(int x, int y)
                {
                    int m = [|x * M1();|]
                    return m;
                }
            }
            """, """
            using System;
            class TestClass
            {
                public static int M1()
                {
                    return 5;
                }

                public int M(int x, int y, int m)
                {
                    return m;
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestBaseKeywordInExpression()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class Net
            {
                public int _value = 6;
            }

            class Perl : Net
            {
                public new int _value = 7;

                public void Write()
                {
                    int x = [|base._value + 1;|]
                }
            }
            """, """
            using System;
            class Net
            {
                public int _value = 6;
            }

            class Perl : Net
            {
                public new int _value = 7;

                public int GetX()
                {
                    return base._value + 1;
                }

                public void Write(int x)
                {
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestFieldReferenceInOptionalParameter()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y = int.MaxValue)
                {
                    int m = [|x * y|];
                    return m;
                }

                void M1()
                {
                    M(7);
                }
            }
            """, """
            using System;
            class TestClass
            {
                int M(int x, int m, int y = int.MaxValue)
                {
                    return m;
                }

                void M1()
                {
                    M(7, 7 * int.MaxValue);
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestNamedParameterNecessary()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                int M(int x, int y = 5, int z = 3)
                {
                    int m = [|z * y|];
                    return m;
                }

                void M1()
                {
                    M(z: 0, y: 2);
                }
            }
            """, """
            using System;
            class TestClass
            {
                int M(int x, int m, int y = 5, int z = 3)
                {
                    return m;
                }

                void M1()
                {
                    M(z: 0, m: 0 * 2, y: 2);
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestHighlightReturnType()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                [|int|] M(int x)
                {
                    return x;
                }

                void M1()
                {
                    M(5);
                }
            }
            """);
    }

    [Fact]
    public async Task TestTypeOfOnString()
    {
        await TestInRegularAndScriptAsync("""
            using System;
            class TestClass
            {
                void M()
                {
                    var x = [|typeof(string);|]
                }
            }
            """, """
            using System;
            class TestClass
            {
                void M(Type x)
                {
                }
            }
            """, index: 0);
    }

    [Fact]
    public async Task TestClassObject()
    {
        await TestMissingInRegularAndScriptAsync("""
            class F
            {
                public int x;
                public int y;

                public F(int x, int y)
                {
                    this.x = x;
                    this.y = y;
                }
            }

            class TestClass
            {
                int N(F f)
                {
                    return f.[|x|];
                }

                void M()
                {
                    N(new F(1, 2));
                }
            }
            """);
    }

    [Fact]
    public async Task TestReferenceInDifferentDocumentWithUsings()
    {
        await TestInRegularAndScriptAsync("""
            <Workspace>
                <Project Language= "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Linq;

            namespace Foo
            {
                public class Bar
                {
                    public Bar()
                    {
                        [|var x = 2|];
                    }
                }
            }
                    </Document>

                    <Document>
            namespace Refactorings
            {
                using Foo;
                class Program
                {
                    public Program(int x)
                    {
                        var bar = new Bar();

                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, """
            <Workspace>
                <Project Language= "C#" AssemblyName="Assembly1" CommonReferences="true">
                    <Document>
            using System;
            using System.Linq;

            namespace Foo
            {
                public class Bar
                {
                    public Bar(int x)
                    {
                    }
                }
            }
                    </Document>

                    <Document>
            namespace Refactorings
            {
                using Foo;
                class Program
                {
                    public Program(int x)
                    {
                        var bar = new Bar(2);

                    }
                }
            }
                    </Document>
                </Project>
            </Workspace>
            """, 0);
    }

    [Fact]
    public async Task TestIntroduceParameterOnParameter()
    {
        await TestMissingInRegularAndScriptAsync("""
            using System;

            class Program
            {
                public static void Main(string[] args)
                {
                    Console.WriteLine([|args|]);
                }
            }
            """);
    }

    [Fact]
    public async Task TestIntroduceParameterOnExpressionContainingParameter()
    {
        await TestInRegularAndScriptAsync("""
            public class C
            {
                public void M(string s)
                {
                    localFunction();

                    void localFunction()
                    {
                        _ = [|s|].ToString();
                    }
                }
            }
            """, """
            public class C
            {
                public void M(string s)
                {
                    localFunction(s);

                    void localFunction(string s)
                    {
                        _ = {|Rename:s|}.ToString();
                    }
                }
            }
            """, 0);
    }

    [Fact]
    public async Task TestIntroduceParameterOnOverload()
    {
        await TestInRegularAndScriptAsync("""
            M();
            int M(int _) => M([|1234|]);
            """, """
            M();
            int M(int _, int v) => M({|Rename:v|}, 1234);
            """, 0);
    }

    [Fact]
    public async Task TestIntroduceParameterOnAttributeArgument()
    {
        await TestMissingInRegularAndScriptAsync("""
            public class C
            {
                [Theory]
                [InlineData([|"A"|])]
                public void Test(string s)
                {
                }
            }
            """);
    }

    [Fact]
    public async Task TestNotOnNamedType1()
    {
        await TestMissingInRegularAndScriptAsync(
            """
            using System;

            class C
            {
                void M()
                {
                    [||]Console.WriteLine();
                }
            }
            """);
    }
}
