// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.AssignOutParameters;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AssignOutParameters
{
    using VerifyCS = CSharpCodeFixVerifier<
        EmptyDiagnosticAnalyzer,
        AssignOutParametersAtStartCodeFixProvider>;

    /// <summary>
    /// Note: many of these tests will validate that there is no fix offered here. That's because for many of them, that
    /// fix is offered by the AssignOutParametersAboveReturnCodeFixProvider instead. These tests have been marked as
    /// such.
    /// </summary>
    [Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
    public class AssignOutParametersAtStartTests
    {
        [Fact]
        public async Task TestForSimpleReturn()
        {
            // Handled by other fixer
            var code = @"class C
{
    char M(out int i)
    {
        {|CS0177:return 'a';|}
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
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
        i = 0;
        switch (0)
        {
            default:
                return 'a';
        }
    }
}");
        }

        [Fact]
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

        [Fact]
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
        i = 0;
        if (b)
            i = 1;

        return 'a';
    }
}");
        }

        [Fact]
        public async Task TestWhenNotAssignedThroughAllPaths2()
        {
            // Handled by other fixer
            var code = @"class C
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
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
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

        [Fact]
        public async Task TestMultiple()
        {
            // Handled by other fixer
            var code = @"class C
{
    char M(out int i, out string s)
    {
        {|CS0177:{|CS0177:return 'a';|}|}
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestMultiple_AssignedInReturn1()
        {
            // Handled by other fixer
            var code = @"class C
{
    string M(out int i, out string s)
    {
        {|CS0177:return s = """";|}
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestMultiple_AssignedInReturn2()
        {
            // Handled by other fixer
            var code = @"class C
{
    string M(out int i, out string s)
    {
        {|CS0177:return (i = 0).ToString();|}
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
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
        i = 0;
        if (true)
        {
            return 'a';
        }
    }
}");
        }

        [Fact]
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
        i = 0;
        if (true)
            return 'a';
    }
}");
        }

        [Fact]
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
        i = 0;
        if (b)
        {
            return 'a';
        }

        i = 1;
        throw null;
    }
}");
        }

        [Fact]
        public async Task TestForExpressionBodyMember()
        {
            // Handled by other fixer
            var code = @"class C
{
    char M(out int i) => {|CS0177:'a'|};
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestForLambdaExpressionBody()
        {
            // Handled by other fixer
            var code = @"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) => {|CS0177:'a'|};
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestMissingForLocalFunctionExpressionBody()
        {
            // Handled by other fixer
            var code = @"class C
{
    void X()
    {
        char {|CS0177:D|}(out int i) => 'a';
        D(out _);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestForLambdaBlockBody()
        {
            // Handled by other fixer
            var code = @"class C
{
    delegate char D(out int i);
    void X()
    {
        D d = (out int i) =>
        {
            {|CS0177:return 'a';|}
        };
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
        public async Task TestForLocalFunctionBlockBody()
        {
            // Handled by other fixer
            var code = @"class C
{
    void X()
    {
        char D(out int i)
        {
            {|CS0177:return 'a';|}
        }

        D(out _);
    }
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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
        i = 0;
        j = 0;
        if (b)
            return 'a';
        else
            return 'a';
    }
}");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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
