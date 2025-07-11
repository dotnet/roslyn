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

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertConversionOperators;

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertTryCastToDirectCastCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.ConvertCast)]
public sealed class ConvertTryCastToDirectCastTests
{
    [Fact]
    public Task ConvertFromAsToExplicit()
        => new VerifyCS.Test
        {
            TestCode = """
            class Program
            {
                public static void Main()
                {
                    var x = 1 as[||] object;
                }
            }
            """,
            FixedCode = """
            class Program
            {
                public static void Main()
                {
                    var x = (object)1;
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure
        }.RunAsync();

    [Fact]
    public Task ConvertFromAsToExplicit_ValueType()
        => new VerifyCS.Test
        {
            TestCode = """
            class Program
            {
                public static void Main()
                {
                    var x = 1 as[||] byte;
                }
            }
            """,
            CompilerDiagnostics = CompilerDiagnostics.None, // CS0077 is present, but we present the refactoring anyway (this may overlap with a diagnostic fixer)
            OffersEmptyRefactoring = false,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();

    [Fact]
    public Task ConvertFromAsToExplicit_NoTypeSyntaxRightOfAs()
        => new VerifyCS.Test
        {
            TestCode = """
            class Program
            {
                public static void Main()
                {
                    var x = 1 as[||] 1;
                }
            }
            """,
            CompilerDiagnostics = CompilerDiagnostics.None,
            OffersEmptyRefactoring = false,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();

    [Theory]
    [InlineData("(1 as object) as $$C",
                "(C)(1 as object)")]
    [InlineData("(1 as$$ object) as C",
                "((object)1) as C")]
    public Task ConvertFromAsToExplicit_Nested(string asExpression, string cast)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            class C { }

            class Program
            {
                public static void Main()
                {
                    var x = {{asExpression}};
                }
            }
            """,
            FixedCode = $$"""
            class C { }

            class Program
            {
                public static void Main()
                {
                    var x = {{cast}};
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();

    [Theory]
    [InlineData("(1 + 1) as $$object",
                "(object)(1 + 1)")]
    [InlineData("(1 $$+ 1) as object",
                "(object)(1 + 1)")]
    public Task ConvertFromAsToExplicit_OtherBinaryExpressions(string asExpression, string cast)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            class Program
            {
                public static void Main()
                {
                    var x = {{asExpression}};
                }
            }
            """,
            FixedCode = $$"""
            class Program
            {
                public static void Main()
                {
                    var x = {{cast}};
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
        }.RunAsync();

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
    public Task ConvertFromAsToExplicit_TriviaHandling(string asExpression, string cast)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            class Program
            {
                public static void Main()
                {
                    var x = {{asExpression}};
                }
            }
            """,
            FixedCode = $$"""
            class Program
            {
                public static void Main()
                {
                    var x = {{cast}};
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64052")]
    public Task ConvertFromAsToExplicit_NullableReferenceType_NullableEnable()
        => new VerifyCS.Test
        {
            TestCode = """
            #nullable enable

            class Program
            {
                public static void Main()
                {
                    var x = null as[||] string;
                }
            }
            """,
            FixedCode = """
            #nullable enable

            class Program
            {
                public static void Main()
                {
                    var x = (string?)null;
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64052")]
    public Task ConvertFromAsToExplicit_NullableReferenceType_NullableDisable()
        => new VerifyCS.Test
        {
            TestCode = """
            #nullable disable

            class Program
            {
                public static void Main()
                {
                    var x = null as[||] string;
                }
            }
            """,
            FixedCode = """
            #nullable disable

            class Program
            {
                public static void Main()
                {
                    var x = (string)null;
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64466")]
    public async Task ConvertFromExplicitToAs_NullableValueType()
    {
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
            TestCode = """
            class Program
            {
                public static void Main()
                {
                    var x = null as[||] byte?;
                }
            }
            """,
            FixedCode = FixedCode,
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
        }.RunAsync();
    }
}
