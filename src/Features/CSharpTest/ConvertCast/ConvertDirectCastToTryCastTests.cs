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
    using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertDirectCastToTryCastCodeRefactoringProvider>;

    [Trait(Traits.Feature, Traits.Features.ConvertCast)]
    public class ConvertDirectCastToTryCastTests
    {
        [Fact]
        public async Task ConvertFromExplicitToAs()
        {
            const string InitialMarkup = """
                class Program
                {
                    public static void Main()
                    {
                        var x = ([||]object)1;
                    }
                }
                """;
            const string ExpectedMarkup = """
                class Program
                {
                    public static void Main()
                    {
                        var x = 1 as object;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = ExpectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Theory]
        [InlineData("dynamic")]
        [InlineData("IComparable")]
        [InlineData("Action")]
        [InlineData("int[]")]
        [InlineData("List<int>")]
        public async Task ConvertFromExplicitToAsSpecialTypes(string targetType)
        {
            var initialMarkup = @$"
using System;
using System.Collections.Generic;

class Program
{{
    public static void Main()
    {{
        var o = new object();
        var x = ([||]{targetType})o;
    }}
}}";
            var expectedMarkup = @$"
using System;
using System.Collections.Generic;

class Program
{{
    public static void Main()
    {{
        var o = new object();
        var x = o as {targetType};
    }}
}}";

            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromExplicitToAs_ValueType()
        {
            const string InitialMarkup = """
                class Program
                {
                    public static void Main()
                    {
                        var x = ([||]byte)1;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = InitialMarkup,
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromExplicitToAs_ValueTypeConstraint()
        {
            const string InitialMarkup = """
                public class C
                {
                    public void M<T>() where T: struct
                    {
                        var o = new object();
                        var t = (T[||])o;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = InitialMarkup,
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromExplicitToAs_Unconstraint()
        {
            const string InitialMarkup = """
                public class C
                {
                    public void M<T>()
                    {
                        var o = new object();
                        var t = (T[||])o;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = InitialMarkup,
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromExplicitToAs_ClassConstraint()
        {
            const string InitialMarkup = """
                public class C
                {
                    public void M<T>() where T: class
                    {
                        var o = new object();
                        var t = (T[||])o;
                    }
                }
                """;
            const string FixedCode = """
                public class C
                {
                    public void M<T>() where T: class
                    {
                        var o = new object();
                        var t = o as T;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = FixedCode,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Theory]
        [InlineData("class", true)]
        [InlineData("interface", false)]
        public async Task ConvertFromExplicitToAs_ConcreteClassOrInterfaceConstraint(string targetTypeKind, bool shouldBeFixed)
        {
            var initialMarkup = @$"
public {targetTypeKind} Target {{ }}

public class C
{{
    public void M<T>() where T: Target
    {{
        var o = new object();
        var t = (T[||])o;
    }}
}}
";
            var fixedCode = @$"
public {targetTypeKind} Target {{ }}

public class C
{{
    public void M<T>() where T: Target
    {{
        var o = new object();
        var t = o as T;
    }}
}}
";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = shouldBeFixed ? fixedCode : initialMarkup,
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromExplicitToAs_NestedTypeParameters()
        {
            var initialMarkup = """
                public class Target { }

                public class C
                {
                    public void M<T, U>()
                        where T: Target
                        where U: T
                    {
                        var o = new object();
                        var u = (U[||])o;
                    }
                }
                """;
            var fixedCode = """
                public class Target { }

                public class C
                {
                    public void M<T, U>()
                        where T: Target
                        where U: T
                    {
                        var o = new object();
                        var u = o as U;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = fixedCode,
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Fact]
        public async Task ConvertFromExplicitToAs_MissingType()
        {
            const string InitialMarkup = """
                public class C
                {
                    public void M()
                    {
                        var o = new object();
                        var t = ({|#0:MissingType|})$$o;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestState = {
                    Sources = { InitialMarkup },
                    // /0/Test0.cs(7,18): error CS0246: Type or namespace "MissingType" not found.
                    ExpectedDiagnostics = { DiagnosticResult.CompilerError("CS0246").WithLocation(0).WithArguments("MissingType") }
                },
                FixedCode = InitialMarkup,
                OffersEmptyRefactoring = false,
                CodeActionValidationMode = CodeActionValidationMode.None,
            }.RunAsync();
        }
        [Theory]
        [InlineData("(C$$)((object)1)",
                    "((object)1) as C")]
        [InlineData("(C)((object$$)1)",
                    "(C)(1 as object)")]
        public async Task ConvertFromExplicitToAs_Nested(string cast, string asExpression)
        {
            var initialMarkup = @$"
class C {{ }}

class Program
{{
    public static void Main()
    {{
        var x = {cast};
    }}
}}
";
            var expectedMarkup = @$"
class C {{ }}

class Program
{{
    public static void Main()
    {{
        var x = {asExpression};
    }}
}}
";
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();

        }

        [Theory]
        [InlineData("/* Leading */ (obj$$ect)1",
                    "/* Leading */ 1 as object")]
        [InlineData("(obj$$ect)1 /* Trailing */",
                    "1 as object /* Trailing */")]
        [InlineData("(obj$$ect)1; // Trailing",
                    "1 as object; // Trailing")]
        [InlineData("(/* Middle1 */ obj$$ect)1",
                    """
                    1 as
                    /* Middle1 */ object
                    """)]
        [InlineData("(obj$$ect /* Middle2 */ )1",
                    "1 as object /* Middle2 */ ")]
        [InlineData("(obj$$ect) /* Middle3 */ 1",
                    "/* Middle3 */ 1 as object")]
        [InlineData("/* Leading */ (/* Middle1 */ obj$$ect /* Middle2 */ ) /* Middle3 */ 1 /* Trailing */",
                    """
                    /* Leading */ /* Middle3 */ 1 as
                    /* Middle1 */ object /* Middle2 */  /* Trailing */
                    """)]
        [InlineData("""
            ($$
            object
            )
            1
            """, """

            1 as
            object
            """)]
        public async Task ConvertFromExplicitToAs_Trivia(string cast, string asExpression)
        {
            var initialMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {cast};
    }}
}}
";
            var expectedMarkup = @$"
class Program
{{
    public static void Main()
    {{
        var x = {asExpression};
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
        public async Task ConvertFromExplicitToAs_NullableReferenceType_NullableEnable()
        {
            var initialMarkup = """
                #nullable enable

                class Program
                {
                    public static void Main()
                    {
                        var x = ([||]string?)null;
                    }
                }
                """;
            var expectedMarkup = """
                #nullable enable

                class Program
                {
                    public static void Main()
                    {
                        var x = null as string;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64052")]
        public async Task ConvertFromExplicitToAs_NullableReferenceType_NullableDisable()
        {
            var initialMarkup = """
                #nullable disable

                class Program
                {
                    public static void Main()
                    {
                        var x = ([||]string?)null;
                    }
                }
                """;
            var expectedMarkup = """
                #nullable disable

                class Program
                {
                    public static void Main()
                    {
                        var x = null as string;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = initialMarkup,
                FixedCode = expectedMarkup,
                CompilerDiagnostics = CompilerDiagnostics.None, // Suppress compiler warning about nullable string in non-nullable context
                CodeActionValidationMode = CodeActionValidationMode.Full,
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
                        var x = ([||]byte?)null;
                    }
                }
                """;
            const string FixedCode = """
                class Program
                {
                    public static void Main()
                    {
                        var x = null as byte?;
                    }
                }
                """;
            await new VerifyCS.Test
            {
                TestCode = InitialMarkup,
                FixedCode = FixedCode,
                CodeActionValidationMode = CodeActionValidationMode.Full,
            }.RunAsync();
        }
    }
}
