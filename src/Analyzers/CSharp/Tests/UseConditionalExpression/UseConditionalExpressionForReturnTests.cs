// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.UseConditionalExpression
{
    public partial class UseConditionalExpressionForReturnTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpUseConditionalExpressionForReturnDiagnosticAnalyzer(),
                new CSharpUseConditionalExpressionForReturnCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleReturn()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return 0;
        }
        else
        {
            return 1;
        }
    }
}",
@"
class C
{
    int M()
    {
        return true ? 0 : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleReturn_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            return 1;
        }
    }
}",
@"
class C
{
    int M()
    {
        return true ? throw new System.Exception() : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleReturn_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return 0;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    int M()
    {
        return true ? 0 : throw new System.Exception();
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotWithTwoThrows()
        {
            await TestMissingAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            throw new System.Exception();
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotOnSimpleReturn_Throw1_CSharp6()
        {
            await TestMissingAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            return 1;
        }
    }
}", parameters: new TestParameters(parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp6)));
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotWithSimpleThrow()
        {
            await TestMissingAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            throw;
        }
        else
        {
            return 1;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleReturnNoBlocks()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
            return 0;
        else
            return 1;
    }
}",
@"
class C
{
    int M()
    {
        return true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleReturnNoBlocks_NotInBlock()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        if (true)
            [||]if (true)
                return 0;
            else
                return 1;
    }
}",
@"
class C
{
    int M()
    {
        if (true)
            return true ? 0 : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingReturnValue1()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return 0;
        }
        else
        {
            return;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingReturnValue1_Throw()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingReturnValue2()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return;
        }
        else
        {
            return 1;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingReturnValue2_Throw()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return;
        }
        else
        {
            throw new System.Exception();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingReturnValue3()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return;
        }
        else
        {
            return;
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestWithNoElseBlockButFollowingReturn()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            return 0;
        }

        return 1;
    }
}",
@"
class C
{
    void M()
    {
        return true ? 0 : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestWithNoElseBlockButFollowingReturn_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }

        return 1;
    }
}",
@"
class C
{
    void M()
    {
        return true ? throw new System.Exception() : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestWithNoElseBlockButFollowingReturn_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            return 0;
        }

        throw new System.Exception();
    }
}",
@"
class C
{
    void M()
    {
        return true ? 0 : throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingWithoutElse()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return 0;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingWithoutElse_Throw()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    object M()
    {
        [||]if (true)
        {
            return ""a"";
        }
        else
        {
            return ""b"";
        }
    }
}",
@"
class C
{
    object M()
    {
        return true ? ""a"" : ""b"";
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44036"), Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion1_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    object M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            return ""b"";
        }
    }
}",
@"
class C
{
    object M()
    {
        return true ? throw new System.Exception() : ""b"";
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/44036"), Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion1_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    object M()
    {
        [||]if (true)
        {
            return ""a"";
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    object M()
    {
        return true ? ""a"" : throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    string M()
    {
        [||]if (true)
        {
            return ""a"";
        }
        else
        {
            return null;
        }
    }
}",
@"
class C
{
    string M()
    {
        return true ? ""a"" : null;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion2_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    string M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            return null;
        }
    }
}",
@"
class C
{
    string M()
    {
        return true ? throw new System.Exception() : (string)null;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion2_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    string M()
    {
        [||]if (true)
        {
            return ""a"";
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    string M()
    {
        return true ? ""a"" : throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    string M()
    {
        [||]if (true)
        {
            return null;
        }
        else
        {
            return null;
        }
    }
}",
@"
class C
{
    string M()
    {
        return true ? null : (string)null;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion3_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    string M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            return null;
        }
    }
}",
@"
class C
{
    string M()
    {
        return true ? throw new System.Exception() : (string)null;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion3_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    string M()
    {
        [||]if (true)
        {
            return null;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    string M()
    {
        return true ? (string)null : throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestKeepTriviaAroundIf()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        // leading
        [||]if (true)
        {
            return 0;
        }
        else
        {
            return 1;
        } // trailing
    }
}",
@"
class C
{
    int M()
    {
        // leading
        return true ? 0 : 1; // trailing
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        {|FixAllInDocument:if|} (true)
        {
            return 0;
        }
        else
        {
            return 1;
        }

        if (true)
        {
            return 2;
        }

        return 3;
    }
}",
@"
class C
{
    int M()
    {
        return true ? 0 : 1;

        return true ? 2 : 3;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMultiLine1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return Foo(
                1, 2, 3);
        }
        else
        {
            return 1;
        }
    }
}",
@"
class C
{
    int M()
    {
        return true
            ? Foo(
                1, 2, 3)
            : 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMultiLine2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return 0;
        }
        else
        {
            return Foo(
                1, 2, 3);
        }
    }
}",
@"
class C
{
    int M()
    {
        return true
            ? 0
            : Foo(
                1, 2, 3);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMultiLine3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            return Foo(
                1, 2, 3);
        }
        else
        {
            return Foo(
                4, 5, 6);
        }
    }
}",
@"
class C
{
    int M()
    {
        return true
            ? Foo(
                1, 2, 3)
            : Foo(
                4, 5, 6);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestElseIfWithBlock()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        if (true)
        {
        }
        else [||]if (false)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}",
@"
class C
{
    int M()
    {
        if (true)
        {
        }
        else
        {
            return false ? 1 : 0;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestElseIfWithBlock_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        if (true)
        {
        }
        else [||]if (false)
        {
            throw new System.Exception();
        }
        else
        {
            return 0;
        }
    }
}",
@"
class C
{
    int M()
    {
        if (true)
        {
        }
        else
        {
            return false ? throw new System.Exception() : 0;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestElseIfWithBlock_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        if (true)
        {
        }
        else [||]if (false)
        {
            return 1;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    int M()
    {
        if (true)
        {
        }
        else
        {
            return false ? 1 : throw new System.Exception();
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestElseIfWithoutBlock()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        if (true) return 2;
        else [||]if (false) return 1;
        else return 0;
    }
}",
@"
class C
{
    int M()
    {
        if (true) return 2;
        else return false ? 1 : 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestRefReturns1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    ref int M(ref int i, ref int j)
    {
        [||]if (true)
        {
            return ref i;
        }
        else
        {
            return ref j;
        }
    }
}",
@"
class C
{
    ref int M(ref int i, ref int j)
    {
        return ref true ? ref i : ref j;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestRefReturns1_Throw1()
        {
            await TestMissingAsync(
@"
class C
{
    ref int M(ref int i, ref int j)
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            return ref j;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestRefReturns1_Throw2()
        {
            await TestMissingAsync(
@"
class C
{
    ref int M(ref int i, ref int j)
    {
        [||]if (true)
        {
            return ref i;
        }
        else
        {
            throw new System.Exception();
        }
    }
}");
        }

        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnYieldReturn()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            yield return 0;
        }
        else
        {
            yield return 1;
        }
    }
}",
@"
class C
{
    int M()
    {
        yield return true ? 0 : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnYieldReturn_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }
        else
        {
            yield return 1;
        }
    }
}",
@"
class C
{
    int M()
    {
        yield return true ? throw new System.Exception() : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnYieldReturn_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            yield return 0;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    int M()
    {
        yield return true ? 0 : throw new System.Exception();
    }
}");
        }

        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnYieldReturn_IEnumerableReturnType()
        {
            await TestInRegularAndScript1Async(
@"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M()
    {
        [||]if (true)
        {
            yield return 0;
        }
        else
        {
            yield return 1;
        }
    }
}",
@"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M()
    {
        yield return true ? 0 : 1;
    }
}");
        }

        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotOnMixedYields()
        {
            await TestMissingAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            yield break;
        }
        else
        {
            yield return 1;
        }
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotOnMixedYields_Throw1()
        {
            await TestMissingAsync(
@"
class C
{
    int M()
    {
        [||]if (true)
        {
            yield break;
        }
        else
        {
            throw new System.Exception();
        }
    }
}");
        }

        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotOnMixedYields_IEnumerableReturnType()
        {
            await TestMissingAsync(
@"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M()
    {
        [||]if (true)
        {
            yield break;
        }
        else
        {
            yield return 1;
        }
    }
}");
        }

        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotWithNoElseBlockButFollowingYieldReturn()
        {
            await TestMissingAsync(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            yield return 0;
        }

        yield return 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestWithNoElseBlockButFollowingYieldReturn_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            throw new System.Exception();
        }

        yield return 1;
    }
}",

@"
class C
{
    void M()
    {
        yield return true ? throw new System.Exception() : 1;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotWithNoElseBlockButFollowingYieldReturn_Throw2()
        {
            await TestMissingAsync(
@"
class C
{
    void M()
    {
        [||]if (true)
        {
            yield return 0;
        }

        throw new System.Exception();
    }
}");
        }

        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestNotWithNoElseBlockButFollowingYieldReturn_IEnumerableReturnType()
        {
            await TestMissingAsync(
@"
using System.Collections.Generic;

class C
{
    IEnumerable<int> M()
    {
        [||]if (true)
        {
            yield return 0;
        }

        yield return 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    bool M(int a)
    {
        [||]if (a == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}",
@"
class C
{
    bool M(int a)
    {
        return a == 0;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse1_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    bool M(int a)
    {
        [||]if (a == 0)
        {
            throw new System.Exception();
        }
        else
        {
            return false;
        }
    }
}",
@"
class C
{
    bool M(int a)
    {
        return a == 0 ? throw new System.Exception() : false;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse1_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    bool M(int a)
    {
        [||]if (a == 0)
        {
            return true;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    bool M(int a)
    {
        return a == 0 ? true : throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    bool M(int a)
    {
        [||]if (a == 0)
        {
            return false;
        }
        else
        {
            return true;
        }
    }
}",
@"
class C
{
    bool M(int a)
    {
        return a != 0;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse2_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    bool M(int a)
    {
        [||]if (a == 0)
        {
            throw new System.Exception();
        }
        else
        {
            return true;
        }
    }
}",
@"
class C
{
    bool M(int a)
    {
        return a == 0 ? throw new System.Exception() : true;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse2_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    bool M(int a)
    {
        [||]if (a == 0)
        {
            return false;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
class C
{
    bool M(int a)
    {
        return a == 0 ? false : throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse3()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    bool M(int a)
    {
        [||]if (a == 0)
        {
            return false;
        }

        return true;
    }
}",
@"
class C
{
    bool M(int a)
    {
        return a != 0;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse3_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    bool M(int a)
    {
        [||]if (a == 0)
        {
            throw new System.Exception();
        }

        return true;
    }
}",
@"
class C
{
    bool M(int a)
    {
        return a == 0 ? throw new System.Exception() : true;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse3_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
class C
{
    bool M(int a)
    {
        [||]if (a == 0)
        {
            return false;
        }

        throw new System.Exception();
    }
}",
@"
class C
{
    bool M(int a)
    {
        return a == 0 ? false : throw new System.Exception();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse4()
        {
            await TestInRegularAndScript1Async(
@"
using System.Collections.Generic;

class C
{
    IEnumerable<bool> M(int a)
    {
        [||]if (a == 0)
        {
            yield return false;
        }
        else
        {
            yield return true;
        }
    }
}",
@"
using System.Collections.Generic;

class C
{
    IEnumerable<bool> M(int a)
    {
        yield return a != 0;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse4_Throw1()
        {
            await TestInRegularAndScript1Async(
@"
using System.Collections.Generic;

class C
{
    IEnumerable<bool> M(int a)
    {
        [||]if (a == 0)
        {
            throw new System.Exception();
        }
        else
        {
            yield return true;
        }
    }
}",
@"
using System.Collections.Generic;

class C
{
    IEnumerable<bool> M(int a)
    {
        yield return a == 0 ? throw new System.Exception() : true;
    }
}");
        }

        [WorkItem(43291, "https://github.com/dotnet/roslyn/issues/43291")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse4_Throw2()
        {
            await TestInRegularAndScript1Async(
@"
using System.Collections.Generic;

class C
{
    IEnumerable<bool> M(int a)
    {
        [||]if (a == 0)
        {
            yield return false;
        }
        else
        {
            throw new System.Exception();
        }
    }
}",
@"
using System.Collections.Generic;

class C
{
    IEnumerable<bool> M(int a)
    {
        yield return a == 0 ? false : throw new System.Exception();
    }
}");
        }

        [WorkItem(36117, "https://github.com/dotnet/roslyn/issues/36117")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestMissingWhenCrossingPreprocessorDirective()
        {
            await TestMissingInRegularAndScriptAsync(
@"
class C
{
    int M()
    {
        bool check = true;
#if true
        [||]if (check)
            return 3;
#endif
        return 2;
    }
}");
        }
    }
}
