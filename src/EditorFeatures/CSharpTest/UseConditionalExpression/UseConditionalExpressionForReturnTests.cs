// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
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
                new CSharpUseConditionalExpressionForReturnCodeRefactoringProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleReturn()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnSimpleReturnNoBlocks()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion1()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion2()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestConversion3()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestKeepTriviaAroundIf()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestElseIfWithoutBlock()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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

        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnYieldReturn()
        {
            await TestInRegularAndScriptAsync(
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

        [WorkItem(27960, "https://github.com/dotnet/roslyn/issues/27960")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestOnYieldReturn_IEnumerableReturnType()
        {
            await TestInRegularAndScriptAsync(
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
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse2()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse3()
        {
            await TestInRegularAndScriptAsync(
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsUseConditionalExpression)]
        public async Task TestReturnTrueFalse4()
        {
            await TestInRegularAndScriptAsync(
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
