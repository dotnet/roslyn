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
    /// <summary>
    /// Note: many of these tests will validate that there is no fix offered here. That's because
    /// for many of them, that fix is offered by the <see cref="AssignOutParametersAboveReturnCodeFixProvider"/>
    /// instead. These tests have been marked as such.
    /// </summary>
    public partial class AssignOutParametersAtStartTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new AssignOutParametersAtStartCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForSimpleReturn()
        {
            // Handled by other fixer
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    char M(out int i)
    {
        [|return 'a';|]
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
        i = 0;
        switch (0)
        {
            default:
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
        i = 0;
        if (b)
            i = 1;

        return 'a';
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestWhenNotAssignedThroughAllPaths2()
        {
            // Handled by other fixer
            await TestMissingInRegularAndScriptAsync(
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
            // Handled by other fixer
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    char M(out int i, out string s)
    {
        [|return 'a';|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMultiple_AssignedInReturn1()
        {
            // Handled by other fixer
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    char M(out int i, out string s)
    {
        [|return s = "";|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMultiple_AssignedInReturn2()
        {
            // Handled by other fixer
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    char M(out int i, out string s)
    {
        [|return (i = 0).ToString();|]
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
        i = 0;
        if (true)
        {
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
        i = 0;
        if (true)
            return 'a';
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
        i = 0;
        if (b)
        {
            return 'a';
        }

        i = 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForExpressionBodyMember()
        {
            // Handled by other fixer
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    char M(out int i) => [|'a';|]
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForLambdaExpressionBody()
        {
            // Handled by other fixer
            await TestMissingInRegularAndScriptAsync(
@"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) => [|'a';|]
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMissingForLocalFunctionExpressionBody()
        {
            // Handled by other fixer
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
            // Handled by other fixer
            await TestMissingInRegularAndScriptAsync(
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
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForLocalFunctionBlockBody()
        {
            // Handled by other fixer
            await TestMissingInRegularAndScriptAsync(
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
        i = 0;
        j = 0;
        if (b)
        {
            return 'a';
        }
        else
        {
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
        i = 0;
        j = 0;
        if (b)
        {
            return 'a';
        }
        else
        {
            return 'a';
        }
    }

    char N(bool b, out int i, out int j)
    {
        i = 0;
        j = 0;
        if (b)
        {
            return 'a';
        }
        else
        {
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
        i = 0;
        j = 0;
        if (b)
            return 'a';
        else
            return 'a';
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
        i = 0;
        j = 0;
        if (b)
            return 'a';
        else
            return 'a';
    }

    char N(bool b, out int i, out int j)
    {
        i = 0;
        j = 0;
        if (b)
            return 'a';
        else
            return 'a';
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
        i = 0;
        j = 0;
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
        i = 0;
        j = 0;
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

    char N(bool b, out int i, out int j)
    {
        i = 0;
        j = 0;
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
}");
        }
    }
}
