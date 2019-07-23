// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.AssignOutParameters;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AssignOutParameters
{
    public partial class AssignOutParametersAboveReturnTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new AssignOutParametersAboveReturnCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForSimpleReturn()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(out int i)
    {
        [|return 'a';|]
    }
}",
@"class C
{
    char M(out int i)
    {
        i = 0;
        return 'a';
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForSwitchSectionReturn()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(out int i)
    {
        switch (0)
        {
            default:
                [|return 'a';|]
        }
    }
}",
@"class C
{
    char M(out int i)
    {
        switch (0)
        {
            default:
                i = 0;
                return 'a';
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMissingWhenVariableAssigned()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    char M(out int i)
    {
        i = 0;
        [|return 'a';|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestWhenNotAssignedThroughAllPaths1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(bool b, out int i)
    {
        if (b)
            i = 1;
        
        [|return 'a';|]
    }
}",
@"class C
{
    char M(bool b, out int i)
    {
        if (b)
            i = 1;
        i = 0;
        return 'a';
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestWhenNotAssignedThroughAllPaths2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    bool M(out int i1, out int i2)
    {
        [|return Try(out i1) || Try(out i2);|]
    }

    bool Try(out int i)
    {
        i = 0;
        return true;
    }
}",
@"class C
{
    bool M(out int i1, out int i2)
    {
        i2 = 0;
        return Try(out i1) || Try(out i2);
    }

    bool Try(out int i)
    {
        i = 0;
        return true;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMissingWhenAssignedThroughAllPaths()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    char M(out int i)
    {
        if (b)
            i = 1;
        else
            i = 2;
        
        [|return 'a';|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMultiple()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(out int i, out string s)
    {
        [|return 'a';|]
    }
}",
@"class C
{
    char M(out int i, out string s)
    {
        i = 0;
        s = null;
        return 'a';
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMultiple_AssignedInReturn1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(out int i, out string s)
    {
        [|return s = "";|]
    }
}",
@"class C
{
    char M(out int i, out string s)
    {
        i = 0;
        return s = "";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMultiple_AssignedInReturn2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(out int i, out string s)
    {
        [|return (i = 0).ToString();|]
    }
}",
@"class C
{
    char M(out int i, out string s)
    {
        s = null;
        return (i = 0).ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestNestedReturn()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(out int i)
    {
        if (true)
        {
            [|return 'a';|]
        }
    }
}",
@"class C
{
    char M(out int i)
    {
        if (true)
        {
            i = 0;
            return 'a';
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestNestedReturnNoBlock()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(out int i)
    {
        if (true)
            [|return 'a';|]
    }
}",
@"class C
{
    char M(out int i)
    {
        if (true)
        {
            i = 0;
            return 'a';
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestNestedReturnEvenWhenWrittenAfter()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(bool b, out int i)
    {
        if (b)
        {
            [|return 'a';|]
        }

        i = 1;
    }
}",
@"class C
{
    char M(bool b, out int i)
    {
        if (b)
        {
            i = 0;
            return 'a';
        }

        i = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForExpressionBodyMember()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(out int i) => [|'a';|]
}",
@"class C
{
    char M(out int i)
    {
        i = 0;
        return 'a';
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForLambdaExpressionBody()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) => [|'a';|]
    }
}",
@"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) => { i = 0; return 'a'; };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMissingForLocalFunctionExpressionBody()
        {
            // Note desirable.  Happens because error is not reported on expr-body
            // like for lambdas.
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    void X()
    {
        char D(out int i) => [|'a';|]
        D(out _);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForLambdaBlockBody()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) =>
        {
            [|return 'a';|]
        }
    }
}",
@"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) =>
        {
            i = 0;
            return 'a';
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForLocalFunctionBlockBody()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    void X()
    {
        char D(out int i)
        {
            [|return 'a';|]
        }

        D(out _);
    }
}",
@"class C
{
    void X()
    {
        char D(out int i)
        {
            i = 0;
            return 'a';
        }

        D(out _);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForOutParamInSinglePath()
        {
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    char M(bool b, out int i)
    {
        if (b)
            i = 1;
        else
            SomeMethod(out i);

        [|return 'a';|]
    }

    void SomeMethod(out int i) => i = 0;
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestFixAll1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            {|FixAllInDocument:return 'a';|}
        }
        else
        {
            return 'a';
        }
    }
}",
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            j = 0;
            return 'a';
        }
        else
        {
            i = 0;
            j = 0;
            return 'a';
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestFixAll1_MultipleMethods()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            {|FixAllInDocument:return 'a';|}
        }
        else
        {
            return 'a';
        }
    }
    char N(bool b, out int i, out int j)
    {
        if (b)
        {
            return 'a';
        }
        else
        {
            return 'a';
        }
    }
}",
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            j = 0;
            return 'a';
        }
        else
        {
            i = 0;
            j = 0;
            return 'a';
        }
    }
    char N(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            j = 0;
            return 'a';
        }
        else
        {
            i = 0;
            j = 0;
            return 'a';
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestFixAll2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
            {|FixAllInDocument:return 'a';|}
        else
            return 'a';
    }
}",
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            j = 0;
            return 'a';
        }
        else
        {
            i = 0;
            j = 0;
            return 'a';
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestFixAll2_MultipleMethods()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
            {|FixAllInDocument:return 'a';|}
        else
            return 'a';
    }
    char N(bool b, out int i, out int j)
    {
        if (b)
            return 'a';
        else
            return 'a';
    }
}",
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            j = 0;
            return 'a';
        }
        else
        {
            i = 0;
            j = 0;
            return 'a';
        }
    }
    char N(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            j = 0;
            return 'a';
        }
        else
        {
            i = 0;
            j = 0;
            return 'a';
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestFixAll3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            {|FixAllInDocument:return 'a';|}
        }
        else
        {
            j = 0;
            return 'a';
        }
    }
}",
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            j = 0;
            return 'a';
        }
        else
        {
            j = 0;
            i = 0;
            return 'a';
        }
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestFixAll3_MultipleMethods()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            {|FixAllInDocument:return 'a';|}
        }
        else
        {
            j = 0;
            return 'a';
        }
    }
    char N(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            return 'a';
        }
        else
        {
            j = 0;
            return 'a';
        }
    }
}",
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            j = 0;
            return 'a';
        }
        else
        {
            j = 0;
            i = 0;
            return 'a';
        }
    }
    char N(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            j = 0;
            return 'a';
        }
        else
        {
            j = 0;
            i = 0;
            return 'a';
        }
    }
}");
        }
    }
}
