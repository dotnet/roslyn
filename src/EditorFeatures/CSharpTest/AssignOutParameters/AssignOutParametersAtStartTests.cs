// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.AssignOutParameters;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeFixVerifier<
    Microsoft.CodeAnalysis.Testing.EmptyDiagnosticAnalyzer,
    Microsoft.CodeAnalysis.CSharp.AssignOutParameters.AssignOutParametersAtStartCodeFixProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AssignOutParameters
{
    /// <summary>
    /// Note: many of these tests will validate that there is no fix offered here. That's because
    /// for many of them, that fix is offered by the <see cref="AssignOutParametersAboveReturnCodeFixProvider"/>
    /// instead. These tests have been marked as such.
    /// </summary>
    public class AssignOutParametersAtStartTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMissingWhenAssignedThroughAllPaths()
        {
            var code = @"class C
{
    char M(out int i)
    {
        if (b)
            i = 1;
        else
            i = 2;
        
        return 'a';
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                // Test0.cs(5,13): error CS0103: The name 'b' does not exist in the current context
                DiagnosticResult.CompilerError("CS0103").WithSpan(5, 13, 5, 14).WithArguments("b"),
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMultiple_AssignedInReturn1()
        {
            // Handled by other fixer
            var code = @"class C
{
    char M(out int i, out string s)
    {
        {|CS0177:return s = "";
|}    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                new[]
                {
                    // Test0.cs(5,20): error CS1010: Newline in constant
                    DiagnosticResult.CompilerError("CS1010").WithSpan(5, 20, 5, 20),
                    // Test0.cs(5,22): error CS1002: ; expected
                    DiagnosticResult.CompilerError("CS1002").WithSpan(5, 22, 5, 22),
                },
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
        public async Task TestMultiple_AssignedInReturn2()
        {
            // Handled by other fixer
            var code = @"class C
{
    char M(out int i, out string s)
    {
        {|CS0177:return (i = 0).ToString();|}
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                // Test0.cs(5,16): error CS0029: Cannot implicitly convert type 'string' to 'char'
                DiagnosticResult.CompilerError("CS0029").WithSpan(5, 16, 5, 34).WithArguments("string", "char"),
                fixedSource: code);
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
    }
}",
                // Test0.cs(3,10): error CS0161: 'C.M(bool, out int)': not all code paths return a value
                DiagnosticResult.CompilerError("CS0161").WithSpan(3, 10, 3, 11).WithArguments("C.M(bool, out int)"),
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
            var code = @"class C
{
    char M(out int i) => {|CS0177:'a'|};
}";

            await VerifyCS.VerifyCodeFixAsync(code, code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
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

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
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
        }
    }
}";

            await VerifyCS.VerifyCodeFixAsync(
                source: code,
                // Test0.cs(9,10): error CS1002: ; expected
                DiagnosticResult.CompilerError("CS1002").WithSpan(9, 10, 9, 10),
                fixedSource: code);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsAssignOutParameters)]
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
