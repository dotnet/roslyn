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

using VerifyCS = CSharpCodeRefactoringVerifier<CSharpConvertDirectCastToTryCastCodeRefactoringProvider>;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.ConvertCast)]
public sealed class ConvertDirectCastToTryCastTests
{
    [Fact]
    public Task ConvertFromExplicitToAs()
        => new VerifyCS.Test
        {
            TestCode = """
            class Program
            {
                public static void Main()
                {
                    var x = ([||]object)1;
                }
            }
            """,
            FixedCode = """
            class Program
            {
                public static void Main()
                {
                    var x = 1 as object;
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();

    [Theory]
    [InlineData("dynamic")]
    [InlineData("IComparable")]
    [InlineData("Action")]
    [InlineData("int[]")]
    [InlineData("List<int>")]
    public Task ConvertFromExplicitToAsSpecialTypes(string targetType)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            using System;
            using System.Collections.Generic;

            class Program
            {
                public static void Main()
                {
                    var o = new object();
                    var x = ([||]{{targetType}})o;
                }
            }
            """,
            FixedCode = $$"""
            using System;
            using System.Collections.Generic;

            class Program
            {
                public static void Main()
                {
                    var o = new object();
                    var x = o as {{targetType}};
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();

    [Fact]
    public Task ConvertFromExplicitToAs_ValueType()
        => new VerifyCS.Test
        {
            TestCode = """
            class Program
            {
                public static void Main()
                {
                    var x = ([||]byte)1;
                }
            }
            """,
            OffersEmptyRefactoring = false,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();

    [Fact]
    public Task ConvertFromExplicitToAs_ValueTypeConstraint()
        => new VerifyCS.Test
        {
            TestCode = """
            public class C
            {
                public void M<T>() where T: struct
                {
                    var o = new object();
                    var t = (T[||])o;
                }
            }
            """,
            OffersEmptyRefactoring = false,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();

    [Fact]
    public Task ConvertFromExplicitToAs_Unconstraint()
        => new VerifyCS.Test
        {
            TestCode = """
            public class C
            {
                public void M<T>()
                {
                    var o = new object();
                    var t = (T[||])o;
                }
            }
            """,
            OffersEmptyRefactoring = false,
            CodeActionValidationMode = CodeActionValidationMode.None,
        }.RunAsync();

    [Fact]
    public async Task ConvertFromExplicitToAs_ClassConstraint()
    {
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
            TestCode = """
            public class C
            {
                public void M<T>() where T: class
                {
                    var o = new object();
                    var t = (T[||])o;
                }
            }
            """,
            FixedCode = FixedCode,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();
    }

    [Theory]
    [InlineData("class", true)]
    [InlineData("interface", false)]
    public async Task ConvertFromExplicitToAs_ConcreteClassOrInterfaceConstraint(string targetTypeKind, bool shouldBeFixed)
    {
        var initialMarkup = $$"""
            public {{targetTypeKind}} Target { }

            public class C
            {
                public void M<T>() where T: Target
                {
                    var o = new object();
                    var t = (T[||])o;
                }
            }
            """;
        var fixedCode = $$"""
            public {{targetTypeKind}} Target { }

            public class C
            {
                public void M<T>() where T: Target
                {
                    var o = new object();
                    var t = o as T;
                }
            }
            """;
        await new VerifyCS.Test
        {
            TestCode = initialMarkup,
            FixedCode = shouldBeFixed ? fixedCode : initialMarkup,
            OffersEmptyRefactoring = false,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();
    }

    [Fact]
    public Task ConvertFromExplicitToAs_NestedTypeParameters()
        => new VerifyCS.Test
        {
            TestCode = """
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
            """,
            FixedCode = """
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
            """,
            OffersEmptyRefactoring = false,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();

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
    public Task ConvertFromExplicitToAs_Nested(string cast, string asExpression)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            class C { }

            class Program
            {
                public static void Main()
                {
                    var x = {{cast}};
                }
            }
            """,
            FixedCode = $$"""
            class C { }

            class Program
            {
                public static void Main()
                {
                    var x = {{asExpression}};
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();

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
    public Task ConvertFromExplicitToAs_Trivia(string cast, string asExpression)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            class Program
            {
                public static void Main()
                {
                    var x = {{cast}};
                }
            }
            """,
            FixedCode = $$"""
            class Program
            {
                public static void Main()
                {
                    var x = {{asExpression}};
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.SemanticStructure,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64052")]
    public Task ConvertFromExplicitToAs_NullableReferenceType_NullableEnable()
        => new VerifyCS.Test
        {
            TestCode = """
            #nullable enable

            class Program
            {
                public static void Main()
                {
                    var x = ([||]string?)null;
                }
            }
            """,
            FixedCode = """
            #nullable enable

            class Program
            {
                public static void Main()
                {
                    var x = null as string;
                }
            }
            """,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64052")]
    public Task ConvertFromExplicitToAs_NullableReferenceType_NullableDisable()
        => new VerifyCS.Test
        {
            TestCode = """
            #nullable disable

            class Program
            {
                public static void Main()
                {
                    var x = ([||]string?)null;
                }
            }
            """,
            FixedCode = """
            #nullable disable

            class Program
            {
                public static void Main()
                {
                    var x = null as string;
                }
            }
            """,
            CompilerDiagnostics = CompilerDiagnostics.None, // Suppress compiler warning about nullable string in non-nullable context
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/64466")]
    public async Task ConvertFromExplicitToAs_NullableValueType()
    {
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
            TestCode = """
            class Program
            {
                public static void Main()
                {
                    var x = ([||]byte?)null;
                }
            }
            """,
            FixedCode = FixedCode,
            CodeActionValidationMode = CodeActionValidationMode.Full,
        }.RunAsync();
    }
}
