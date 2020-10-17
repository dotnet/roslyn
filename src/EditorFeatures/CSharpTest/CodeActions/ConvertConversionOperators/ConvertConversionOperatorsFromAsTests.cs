// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions.CSharpCodeRefactoringVerifier<Microsoft.CodeAnalysis.CSharp.CodeRefactorings.ConvertConversionOperators.CSharpConvertConversionOperatorsFromAsRefactoringProvider>;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings.ConvertConversionOperators
{
    [Trait(Traits.Feature, Traits.Features.ConvertConversionOperators)]
    public class ConvertConversionOperatorsFromAsTests
    {
        [Fact]
        public async Task ConvertFromAsToExplicit()
        {
            const string InitialMarkup = @"
class Program
{
    public static void Main()
    {
        var x = 1 as[||] object;
    }
}";
            const string ExpectedMarkup = @"
class Program
{
    public static void Main()
    {
        var x = (object)1;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = ExpectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromAsToExplicit_ValueType()
        {
            const string InitialMarkup = @"
class Program
{
    public static void Main()
    {
        var x = 1 as[||] byte;
    }
}";
            const string ExpectedMarkup = @"
class Program
{
    public static void Main()
    {
        var x = (byte)1;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = ExpectedMarkup,
                CompilerDiagnostics = CompilerDiagnostics.None, // CS0077 is present, but we present the refactoring anyway (this may overlap with a diagnostic fixer)
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromAsToExplicit_NoTypeSyntaxRightOfAs()
        {
            const string InitialMarkup = @"
class Program
{
    public static void Main()
    {
        var x = 1 as[||] 1;
    }
}";
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = InitialMarkup,
                CompilerDiagnostics = CompilerDiagnostics.None,
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [Theory]
        [InlineData("(1 as object) as $$C",
                    "(C)(1 as object)")]
        [InlineData("(1 as$$ object) as C",
                    "((object)1) as C")]
        public async Task ConvertFromAsToExplicit_Nested(string asExpression, string cast)
        {
            var initialMarkup = @$"
class C {{ }}

class Program
{{
    public static void Main()
    {{
        var x = { asExpression };
    }}
}}
";
            var expectedMarkup = @$"
class C {{ }}

class Program
{{
    public static void Main()
    {{
        var x = { cast };
    }}
}}
";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [Theory]
        [InlineData("(1 + 1) as $$object",
                    "(object)(1 + 1)")]
        [InlineData("(1 $$+ 1) as object",
                    "(object)(1 + 1)")]
        public async Task ConvertFromAsToExplicit_OtherBinaryExpressions(string asExpression, string cast)
        {
            var initialMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = { asExpression };
    }}
}}
";
            var expectedMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = { cast };
    }}
}}
";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }
    }
}
