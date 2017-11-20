// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertLocalFunctionToMethod;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ConvertLocalFunctionToMethod
{
    public class ConvertLocalFunctionToMethodTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new CSharpConvertLocalFunctionToMethodCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLocalFunctionToMethod)]
        public async Task TestCaptures()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    static void Use<T>(T a) {}
    static void Use<T>(ref T a) {}

    static void LocalFunction() {} // trigger rename

    void M<T1, T2>(T1 param1, T2 param2)
        where T1 : struct
        where T2 : struct
    {
        var local1 = 0;
        var local2 = 0;
        void [||]LocalFunction()
        {
            Use(param1);
            Use(ref param2);
            Use(local1);
            Use(ref local2);
            Use(this);
        }
        LocalFunction();
    }
}",
@"class C
{
    static void Use<T>(T a) {}
    static void Use<T>(ref T a) {}

    static void LocalFunction() {} // trigger rename

    void M<T1, T2>(T1 param1, T2 param2)
        where T1 : struct
        where T2 : struct
    {
        var local1 = 0;
        var local2 = 0;
        LocalFunction1<T1, T2>(param1, ref param2, local1, ref local2);
    }

    private void LocalFunction1<T1, T2>(T1 param1, ref T2 param2, int local1, ref int local2)
        where T1 : struct
        where T2 : struct
    {
        Use(param1);
        Use(ref param2);
        Use(local1);
        Use(ref local2);
        Use(this);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLocalFunctionToMethod)]
        public async Task TestTypeParameters()
        {
            await TestInRegularAndScriptAsync(
@"class C<T0>
{
    static void LocalFunction() {} // trigger rename

    void M<T1, T2>(int i)
        where T1 : struct
    {
        void Local1<T3, T4>()
            where T4 : struct
        {
            void [||]LocalFunction<T5, T6>(T5 a, T6 b)
                where T5 : struct
            {
                _ = typeof(T2);
                _ = typeof(T4);
            }
            LocalFunction<byte, int>(5, 6);
            LocalFunction(5, 6);
        }
    }
}",
@"class C<T0>
{
    static void LocalFunction() {} // trigger rename

    void M<T1, T2>(int i)
        where T1 : struct
    {
        void Local1<T3, T4>()
            where T4 : struct
        {
            LocalFunction1<T2, T4, byte, int>(5, 6);
            LocalFunction1<T2, T4, int, int>(5, 6);
        }
    }

    private static void LocalFunction1<T2, T4, T5, T6>(T5 a, T6 b)
        where T4 : struct
        where T5 : struct
    {
        _ = typeof(T2);
        _ = typeof(T4);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLocalFunctionToMethod)]
        public async Task TestNameConflict()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void LocalFunction() {} // trigger rename

    void M()
    {
        void [||]LocalFunction() => M();
        LocalFunction();
        System.Action x = LocalFunction;
    }
}",
@"class C
{
    void LocalFunction() {} // trigger rename

    void M()
    {
        LocalFunction1();
        System.Action x = LocalFunction1;
    }

    private void LocalFunction1() => M();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLocalFunctionToMethod)]
        public async Task TestNamedArguments1()
        {
            await TestAsync(
@"class C
{
    void LocalFunction() {} // trigger rename

    void M()
    {
        int var = 2;
        int [||]LocalFunction(int i)
        {
            return var;
        }
        LocalFunction(i: 0);
    }
}",
@"class C
{
    void LocalFunction() {} // trigger rename

    void M()
    {
        int var = 2;
        LocalFunction1(i: 0, var);
    }

    private static int LocalFunction1(int i, int var)
    {
        return var;
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7_2));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLocalFunctionToMethod)]
        public async Task TestNamedArguments2()
        {
            await TestAsync(
@"class C
{
    void LocalFunction() {} // trigger rename

    void M()
    {
        int var = 2;
        int [||]LocalFunction(int i)
        {
            return var;
        }
        LocalFunction(i: 0);
    }
}",
@"class C
{
    void LocalFunction() {} // trigger rename

    void M()
    {
        int var = 2;
        LocalFunction1(i: 0, var: var);
    }

    private static int LocalFunction1(int i, int var)
    {
        return var;
    }
}", parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp7));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConvertLocalFunctionToMethod)]
        public async Task TestCaretPositon()
        {
            await TestAsync("C [||]LocalFunction(C c)");
            await TestAsync("C Local[||]Function(C c)");
            await TestAsync("C [|LocalFunction|](C c)");
            await TestAsync("C LocalFunction[||](C c)");
            await TestMissingAsync("C Local[|Function|](C c)");
            await TestMissingAsync("[||]C LocalFunction(C c)");
            await TestMissingAsync("[|C|] LocalFunction(C c)");
            await TestMissingAsync("C[||] LocalFunction(C c)");
            await TestMissingAsync("C LocalFunction([||]C c)");
            await TestMissingAsync("C LocalFunction(C [||]c)");

            async Task TestAsync(string signature)
            {
                await TestInRegularAndScriptAsync(
$@"class C
{{
    void M()
    {{
        {signature}
        {{
            return null;
        }}
    }}
}}",
@"class C
{
    void M()
    {
    }

    private static C LocalFunction(C c)
    {
        return null;
    }
}");
            }

            async Task TestMissingAsync(string signature)
            {
                await this.TestMissingAsync(
$@"class C
{{
    void M()
    {{
        {signature}
        {{
            return null;
        }}
    }}
}}");
            }
        }
    }
}
