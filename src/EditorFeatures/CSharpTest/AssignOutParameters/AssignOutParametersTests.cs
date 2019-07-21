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
    public partial class AssignOutParametersTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (null, new AssignOutParametersCodeFixProvider());

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForSimpleReturn()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    char M(out int i)
    {
        [|return '';|]
    }
}",
@"class C
{
    char M(out int i)
    {
        i = 0;
        return '';
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
        [|return '';|]
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
        
        [|return '';|]
    }
}",
@"class C
{
    char M(bool b, out int i)
    {
        if (b)
            i = 1;
        i = 0;
        return '';
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
        
        [|return '';|]
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
        [|return '';|]
    }
}",
@"class C
{
    char M(out int i, out string s)
    {
        i = 0;
        s = null;
        return '';
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
            [|return '';|]
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
            return '';
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
            [|return '';|]
    }
}",
@"class C
{
    char M(out int i)
    {
        if (true)
        {
            i = 0;
            return '';
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
            [|return '';|]
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
            return '';
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
    char M(out int i) => [|'';|]
}",
@"class C
{
    char M(out int i)
    {
        i = 0;
        return '';
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
        D d = (out int i) => [|'';|]
    }
}",
@"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) => { i = 0; return ''; };
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
            return [|'';|]
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
            return '';
        }
    }
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
            {|FixAllInDocument:return '';|}
        }
        else
        {
            return '';
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
            return '';
        }
        else
        {
            i = 0;
            j = 0;
            return '';
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
            {|FixAllInDocument:return '';|}
        else
            return '';
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
            return '';
        }
        else
        {
            i = 0;
            j = 0;
            return '';
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
            {|FixAllInDocument:return '';|}
        }
        else
        {
            j = 0;
            return '';
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
            return '';
        }
        else
        {
            j = 0;
            i = 0;
            return '';
        }
    }
}");
        }
    }
}
