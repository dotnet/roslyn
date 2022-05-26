﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.AssignOutParameters.AssignOutParametersAboveReturnCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AssignOutParameters
{
    public class AssignOutParametersAboveReturnTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForSimpleReturn()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(out int i)
    {
        {|CS0177:return 'a';|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(out int i)
    {
        switch (0)
        {
            default:
                {|CS0177:return 'a';|}
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
            var code = @"class C
{
    char M(out int i)
    {
        i = 0;
        return 'a';
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestWhenNotAssignedThroughAllPaths1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(bool b, out int i)
    {
        if (b)
            i = 1;
        
        {|CS0177:return 'a';|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    bool M(out int i1, out int i2)
    {
        {|CS0177:return Try(out i1) || Try(out i2);|}
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
            var code = @"class C
{
    char M(bool b, out int i)
    {
        if (b)
            i = 1;
        else
            i = 2;
        
        return 'a';
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMultiple()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(out int i, out string s)
    {
        {|CS0177:{|CS0177:return 'a';|}|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    string M(out int i, out string s)
    {
        {|CS0177:return s = """";|}
    }
}",
@"class C
{
    string M(out int i, out string s)
    {
        i = 0;
        return s = """";
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMultiple_AssignedInReturn2()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    string M(out int i, out string s)
    {
        {|CS0177:return (i = 0).ToString();|}
    }
}",
@"class C
{
    string M(out int i, out string s)
    {
        s = null;
        return (i = 0).ToString();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestNestedReturn()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(out int i)
    {
        if (true)
        {
            {|CS0177:return 'a';|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(out int i)
    {
        if (true)
            {|CS0177:return 'a';|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(bool b, out int i)
    {
        if (b)
        {
            {|CS0177:return 'a';|}
        }

        i = 1;
        throw null;
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
        throw null;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForExpressionBodyMember()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(out int i) => {|CS0177:'a'|};
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) => {|CS0177:'a'|};
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    void X()
    {
        char {|CS0177:D|}(out int i) => 'a';
        D(out _);
    }
}",
@"class C
{
    void X()
    {
        char D(out int i) { i = 0; return 'a'; }

        D(out _);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForLambdaBlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) =>
        {
            {|CS0177:return 'a';|}
        };
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
        };
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestForLocalFunctionBlockBody()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    void X()
    {
        char D(out int i)
        {
            {|CS0177:return 'a';|}
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
            var code = @"class C
{
    char M(bool b, out int i)
    {
        if (b)
            i = 1;
        else
            SomeMethod(out i);

        return 'a';
    }

    void SomeMethod(out int i) => i = 0;
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestFixAll1()
        {
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            {|CS0177:{|CS0177:return 'a';|}|}
        }
        else
        {
            {|CS0177:{|CS0177:return 'a';|}|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            {|CS0177:{|CS0177:return 'a';|}|}
        }
        else
        {
            {|CS0177:{|CS0177:return 'a';|}|}
        }
    }
    char N(bool b, out int i, out int j)
    {
        if (b)
        {
            {|CS0177:{|CS0177:return 'a';|}|}
        }
        else
        {
            {|CS0177:{|CS0177:return 'a';|}|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
            {|CS0177:{|CS0177:return 'a';|}|}
        else
            {|CS0177:{|CS0177:return 'a';|}|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
            {|CS0177:{|CS0177:return 'a';|}|}
        else
            {|CS0177:{|CS0177:return 'a';|}|}
    }
    char N(bool b, out int i, out int j)
    {
        if (b)
            {|CS0177:{|CS0177:return 'a';|}|}
        else
            {|CS0177:{|CS0177:return 'a';|}|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            {|CS0177:return 'a';|}
        }
        else
        {
            j = 0;
            {|CS0177:return 'a';|}
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
            await VerifyCS.VerifyCodeFixAsync(
@"class C
{
    char M(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            {|CS0177:return 'a';|}
        }
        else
        {
            j = 0;
            {|CS0177:return 'a';|}
        }
    }
    char N(bool b, out int i, out int j)
    {
        if (b)
        {
            i = 0;
            {|CS0177:return 'a';|}
        }
        else
        {
            j = 0;
            {|CS0177:return 'a';|}
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
