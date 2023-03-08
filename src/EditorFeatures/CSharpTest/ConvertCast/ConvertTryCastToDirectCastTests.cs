// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.ConvertCast;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertConversionOperators
{
    using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertTryCastToDirectCastCodeRefactoringProvider>;

    [Trait(Traits.Feature, Traits.Features.ConvertCast)]
    public class ConvertTryCastToDirectCastTests
    {
        [Fact]
        public async Task ConvertFromAsToExplicit()
        {
            const string InitialMarkup = """
                class Program
                {
                    public static void Main()
                    {
                        var x = 1 as[||] object;
                    }
                }
                """;
            const string ExpectedMarkup = """
                class Program
                {
                    public static void Main()
                    {
                        var x = (object)1;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = ExpectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromAsToExplicit_ValueType()
        {
            const string InitialMarkup = """
                class Program
                {
                    public static void Main()
                    {
                        var x = 1 as[||] byte;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = InitialMarkup,
                CompilerDiagnostics = CompilerDiagnostics.None, // CS0077 is present, but we present the refactoring anyway (this may overlap with a diagnostic fixer)
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromAsToExplicit_NoTypeSyntaxRightOfAs()
        {
            const string InitialMarkup = """
                class Program
                {
                    public static void Main()
                    {
                        var x = 1 as[||] 1;
                    }
                }
                """;
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
        var x = {asExpression};
    }}
}}
";
            var expectedMarkup = @$"
class C {{ }}

class Program
{{
    public static void Main()
    {{
        var x = {cast};
    }}
}}
";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
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
        var x = {asExpression};
    }}
}}
";
            var expectedMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {cast};
    }}
}}
";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Theory]
        [InlineData("/* Leading */ 1 as $$object",
                    "/* Leading */ (object)1")]
        [InlineData("1 as $$object /* Trailing */",
                    "(object)1 /* Trailing */")]
        [InlineData("1 /* Middle1 */ as $$object",
                    "(object)1/* Middle1 */ ")]
        [InlineData("1 as /* Middle2 */ $$object",
                    "/* Middle2 */ (object)1")]
        [InlineData("/* Leading */ 1 /* Middle1 */ as /* Middle2 */ $$object /* Trailing */",
                    "/* Leading */ /* Middle2 */ (object)1/* Middle1 */  /* Trailing */")]
        public async Task ConvertFromAsToExplicit_TriviaHandling(string asExpression, string cast)
        {
            var initialMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {asExpression};
    }}
}}
";
            var expectedMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {cast};
    }}
}}
";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64052")]
        public async Task ConvertFromAsToExplicit_NullableReferenceType_NullableEnable()
        {
            var initialMarkup = """
                #nullable enable

                class Program
                {
                    public static void Main()
                    {
                        var x = null as[||] string;
                    }
                }
                """;
            var expectedMarkup = """
                #nullable enable

                class Program
                {
                    public static void Main()
                    {
                        var x = (string?)null;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64052")]
        public async Task ConvertFromAsToExplicit_NullableReferenceType_NullableDisable()
        {
            var initialMarkup = """
                #nullable disable

                class Program
                {
                    public static void Main()
                    {
                        var x = null as[||] string;
                    }
                }
                """;
            var expectedMarkup = """
                #nullable disable

                class Program
                {
                    public static void Main()
                    {
                        var x = (string)null;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }

        [Fact]
        [WorkItem("https://github.com/dotnet/roslyn/issues/64466")]
        public async Task ConvertFromExplicitToAs_NullableValueType()
        {
            const string InitialMarkup = """
                class Program
                {
                    public static void Main()
                    {
                        var x = null as[||] byte?;
                    }
                }
                """;
            const string FixedCode = """
                class Program
                {
                    public static void Main()
                    {
                        var x = (byte?)null;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = FixedCode,
                CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
            }.RunAsync();
        }
    }
}
