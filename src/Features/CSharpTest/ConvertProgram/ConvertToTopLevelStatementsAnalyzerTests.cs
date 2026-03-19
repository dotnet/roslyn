// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.TopLevelStatements;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertProgram;

using VerifyCS = CSharpCodeFixVerifier<ConvertToTopLevelStatementsDiagnosticAnalyzer, ConvertToTopLevelStatementsCodeFixProvider>;

public sealed class ConvertToTopLevelStatementsAnalyzerTests
{
    public static IEnumerable<object[]> EndOfDocumentSequences
    {
        get
        {
            yield return new object[] { "" };
            yield return new object[] { "\r\n" };
        }
    }

    [Fact]
    public Task NotOfferedWhenUserPrefersProgramMain()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false } },
        }.RunAsync();

    [Fact]
    public Task NotOfferedPriorToCSharp9()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp8,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
        }.RunAsync();

    [Fact]
    public Task OfferedInCSharp9()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                {|IDE0210:static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }|}
            }

            """,
            FixedCode = """

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
        }.RunAsync();

    [Fact]
    public Task TestFileHeader1()
        => new VerifyCS.Test
        {
            TestCode = """
            // This is a file header

            class Program
            {
                {|IDE0210:static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }|}
            }

            """,
            FixedCode = """
            // This is a file header

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
        }.RunAsync();

    [Fact]
    public Task TestFileHeader2()
        => new VerifyCS.Test
        {
            TestCode = """
            // This is a file header

            namespace N
            {
                class Program
                {
                    {|IDE0210:static void Main(string[] args)
                    {
                        System.Console.WriteLine(0);
                    }|}
                }
            }

            """,
            FixedCode = """
            // This is a file header

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
        }.RunAsync();

    [Fact]
    public Task TestFileHeader3()
        => new VerifyCS.Test
        {
            TestCode = """
            // This is a file header

            namespace N;

            class Program
            {
                {|IDE0210:static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }|}
            }

            """,
            FixedCode = """
            // This is a file header


            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
        }.RunAsync();

    [Fact]
    public Task TestFileHeader4()
        => new VerifyCS.Test
        {
            TestCode = """
            // This is a file header
            using System;

            namespace N;

            class Program
            {
                {|IDE0210:static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }|}
            }

            """,
            FixedCode = """
            // This is a file header
            using System;

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
        }.RunAsync();

    [Fact]
    public Task OfferedWithoutArgs()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                {|IDE0210:static void Main()
                {
                    System.Console.WriteLine(0);
                }|}
            }

            """,
            FixedCode = """

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
        }.RunAsync();

    [Fact]
    public Task NotOfferedInLibrary()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void Main()
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
        }.RunAsync();

    [Fact]
    public Task OfferedOnNameWhenNotHidden()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotOnNonStaticMain()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
            ExpectedDiagnostics =
            {
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                DiagnosticResult.CompilerError("CS5001"),
            }
        }.RunAsync();

    [Fact]
    public Task NotOnGenericMain()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void Main<T>(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
            ExpectedDiagnostics =
            {
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                DiagnosticResult.CompilerError("CS5001"),
            }
        }.RunAsync();

    [Fact]
    public Task NotOnRandomMethod()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void Main1(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
            ExpectedDiagnostics =
            {
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                DiagnosticResult.CompilerError("CS5001"),
            }
        }.RunAsync();

    [Fact]
    public Task NotOnMethodWithNoBody()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void {|CS0501:Main|}(string[] args);
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotOnExpressionBody()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void Main(string[] args)
                    => System.Console.WriteLine(0);
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotOnTypeWithInheritance1()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program : System.Exception
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotOnTypeWithInheritance2()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program : {|CS0535:System.IComparable|}
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotOnMultiPartType()
        => new VerifyCS.Test
        {
            TestCode = """

            partial class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            partial class Program
            {
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotOnPublicType()
        => new VerifyCS.Test
        {
            TestCode = """

            public class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotOnTypeWithAttribute()
        => new VerifyCS.Test
        {
            TestCode = """

            [System.CLSCompliant(true)]
            class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotOnTypeWithDocComment()
        => new VerifyCS.Test
        {
            TestCode = """

            /// <summary></summary>
            class Program
            {
                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotOnTypeWithNormalComment()
        => new VerifyCS.Test
        {
            TestCode = """

            // <summary></summary>
            class Program
            {
                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            // <summary></summary>
            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithMemberWithAttributes()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                [System.CLSCompliant(true)]
                static int x;

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithMethodWithAttribute1()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                [System.CLSCompliant(true)]
                static void M() { }

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithMethodWithAttribute2()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void M() { }

                [System.CLSCompliant(true)]
                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithMemberWithDocComment()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                /// <summary></summary>
                static int x;

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithNonPrivateMember()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                public static int x;

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithNonStaticMember()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                int x;

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithStaticConstructor()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static Program()
                {
                }

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithInstanceConstructor()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private Program()
                {
                }

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithProperty()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private int X { get; }

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithEvent()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private event System.Action X;

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithOperator()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                public static Program operator+(Program p1, Program p2) => null;

                static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task NotWithMethodWithWrongArgsName()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void Main(string[] args1)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestFieldWithNoAccessibility()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static int x;

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            int x = 0;

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestFollowingField()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }

                static int x;
            }

            """,
            FixedCode = """


            int x = 0;
            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestFieldWithPrivateAccessibility()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static int x;

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            int x = 0;

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestFieldWithMultipleDeclarators()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static int x, y;

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            int x = 0, y = 0;

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestFieldWithInitializer()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static int x = 1;

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            int x = 1;

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestReferenceField()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static string x;

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            string x = null;

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestBooleanField()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static bool x;

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            bool x = false;

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestStructField()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static System.DateTime x;

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            System.DateTime x = default;

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestFieldWithComments()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                // Leading
                private static int x = 0; // Trailing

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }

            """,
            FixedCode = """

            // Leading
            int x = 0; // Trailing

            System.Console.WriteLine(0);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestEmptyMethod()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static int x = 0;

                static void {|IDE0210:Main|}(string[] args)
                {
                }
            }

            """,
            FixedCode = """

            int x = 0;

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestMultipleStatements()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static int x = 0;

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(args);
                    return;
                }
            }

            """,
            FixedCode = """

            int x = 0;

            System.Console.WriteLine(args);
            return;

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestOtherMethodBecomesLocalFunction()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static int x = 0;

                static void OtherMethod()
                {
                    return;
                }

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(args);
                }
            }

            """,
            FixedCode = """

            int x = 0;

            void OtherMethod()
            {
                return;
            }

            System.Console.WriteLine(args);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestWithUnsafeMethod()
        => new VerifyCS.Test
        {
            TestCode = """

            class Program
            {
                private static int x = 0;

                unsafe static void OtherMethod()
                {
                    return;
                }

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(args);
                }
            }

            """,
            FixedCode = """

            int x = 0;

            unsafe void OtherMethod()
            {
                return;
            }

            System.Console.WriteLine(args);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestOtherComplexMethodBecomesLocalFunction()
        => new VerifyCS.Test
        {
            TestCode = """

            using System.Threading.Tasks;

            class Program
            {
                private static int x = 0;

                static async Task OtherMethod<T>(T param) where T : struct
                {
                    return;
                }

                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(args);
                }
            }

            """,
            FixedCode = """

            using System.Threading.Tasks;

            int x = 0;

            async Task OtherMethod<T>(T param) where T : struct
            {
                return;
            }

            System.Console.WriteLine(args);

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestAwaitExpression()
        => new VerifyCS.Test
        {
            TestCode = """

            using System.Threading.Tasks;

            class Program
            {
                static async Task {|IDE0210:Main|}(string[] args)
                {
                    await Task.CompletedTask;
                }
            }

            """,
            FixedCode = """

            using System.Threading.Tasks;

            await Task.CompletedTask;

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestInNamespaceWithOtherType()
        => new VerifyCS.Test
        {
            TestCode = """

            using System.Threading.Tasks;

            namespace X.Y
            {
                class Program
                {
                    static async Task {|IDE0210:Main|}(string[] args)
                    {
                        await Task.CompletedTask;
                    }
                }

                class Other
                {
                }
            }

            """,
            FixedCode = """

            using System.Threading.Tasks;

            await Task.CompletedTask;

            namespace X.Y
            {
                class Other
                {
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Theory]
    [MemberData(nameof(EndOfDocumentSequences))]
    public Task TestInTopLevelNamespaceWithOtherType(string endOfDocumentSequence)
        => new VerifyCS.Test
        {
            TestCode = $$"""

            using System.Threading.Tasks;

            namespace X.Y;

            class Program
            {
                static async Task {|IDE0210:Main|}(string[] args)
                {
                    await Task.CompletedTask;
                }
            }

            class Other
            {
            }{{endOfDocumentSequence}}
            """,
            FixedCode = $$"""

            using System.Threading.Tasks;

            await Task.CompletedTask;

            namespace X.Y
            {
                class Other
                {
                }
            }{{endOfDocumentSequence}}
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestInNamespaceWithOtherTypeThatIsReferenced()
        => new VerifyCS.Test
        {
            TestCode = """

            using System.Threading.Tasks;

            namespace X.Y
            {
                class Program
                {
                    static void {|IDE0210:Main|}(string[] args)
                    {
                        System.Console.WriteLine(typeof(Other));
                    }
                }

                class Other
                {
                }
            }

            """,
            FixedCode = """

            using System.Threading.Tasks;
            using X.Y;

            System.Console.WriteLine(typeof(Other));

            namespace X.Y
            {
                class Other
                {
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Theory]
    [MemberData(nameof(EndOfDocumentSequences))]
    public Task TestInTopLevelNamespaceWithOtherTypeThatIsReferenced(string endOfDocumentSequence)
        => new VerifyCS.Test
        {
            TestCode = $$"""

            using System.Threading.Tasks;

            namespace X.Y;

            class Program
            {
                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine(typeof(Other));
                }
            }

            class Other
            {
            }{{endOfDocumentSequence}}
            """,
            FixedCode = $$"""

            using System.Threading.Tasks;
            using X.Y;

            System.Console.WriteLine(typeof(Other));

            namespace X.Y
            {
                class Other
                {
                }
            }{{endOfDocumentSequence}}
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestInNamespaceWithNoOtherTypes()
        => new VerifyCS.Test
        {
            TestCode = """

            using System.Threading.Tasks;

            namespace X.Y
            {
                class Program
                {
                    static void {|IDE0210:Main|}(string[] args)
                    {
                        System.Console.WriteLine();
                    }
                }
            }

            """,
            FixedCode = """

            using System.Threading.Tasks;

            System.Console.WriteLine();

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestInTopLevelNamespaceWithNoOtherTypes()
        => new VerifyCS.Test
        {
            TestCode = """

            using System.Threading.Tasks;

            namespace X.Y;

            class Program
            {
                static void {|IDE0210:Main|}(string[] args)
                {
                    System.Console.WriteLine();
                }
            }

            """,
            FixedCode = """

            using System.Threading.Tasks;

            System.Console.WriteLine();

            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestInSingletonNamespaceWithOtherTypeThatIsReferenced()
        => new VerifyCS.Test
        {
            TestCode = """

            using System.Threading.Tasks;

            namespace X.Y
            {
                class Program
                {
                    static void {|IDE0210:Main|}(string[] args)
                    {
                        System.Console.WriteLine(typeof(Other));
                    }
                }
            }

            namespace X
            {
                class Other
                {
                }
            }

            """,
            FixedCode = """

            using System.Threading.Tasks;
            using X;

            System.Console.WriteLine(typeof(Other));

            namespace X
            {
                class Other
                {
                }
            }

            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion } },
        }.RunAsync();
}
