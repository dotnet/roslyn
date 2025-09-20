// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertProgram;
using Microsoft.CodeAnalysis.CSharp.TopLevelStatements;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertProgram;

using VerifyCS = CSharpCodeFixVerifier<ConvertToProgramMainDiagnosticAnalyzer, ConvertToProgramMainCodeFixProvider>;

public sealed class ConvertToProgramMainAnalyzerTests
{
    [Fact]
    public Task NotOfferedWhenUserPrefersTopLevelStatements()
        => new VerifyCS.Test
        {
            TestCode = """
            System.Console.WriteLine(0);
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, true } },
        }.RunAsync();

    [Fact]
    public Task NotOfferedWhenUserPrefersProgramMainButNoTopLevelStatements()
        => new VerifyCS.Test
        {
            TestCode = """
            class C
            {
                void M()
                {
                    System.Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false } },
        }.RunAsync();

    [Fact]
    public Task OfferedWhenUserPrefersProgramMainAndTopLevelStatements_Silent()
        => new VerifyCS.Test
        {
            TestCode = """
            {|IDE0211:
            System.Console.WriteLine(0);
            |}
            """,
            FixedCode = """

            internal class Program
            {
                private static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent } },
        }.RunAsync();

    [Fact]
    public Task TestHeader1()
        => new VerifyCS.Test
        {
            TestCode = """
            {|IDE0211:// This is a file banner

            System.Console.WriteLine(0);
            |}
            """,
            FixedCode = """
            // This is a file banner

            internal class Program
            {
                private static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent } },
        }.RunAsync();

    [Fact]
    public Task TestHeader2()
        => new VerifyCS.Test
        {
            TestCode = """
            {|IDE0211:// This is a file banner
            using System;

            System.Console.WriteLine(0);
            |}
            """,
            FixedCode = """
            // This is a file banner
            using System;

            internal class Program
            {
                private static void Main(string[] args)
                {
                    Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent } },
        }.RunAsync();

    [Fact]
    public Task NotOfferedInLibrary()
        => new VerifyCS.Test
        {
            TestCode = """
            {|CS8805:System.Console.WriteLine(0);|}
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent } },
        }.RunAsync();

    [Fact]
    public Task NotOfferedWhenSuppressed()
        => new VerifyCS.Test
        {
            TestCode = """
            System.Console.WriteLine(0);
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.None } },
        }.RunAsync();

    [Fact]
    public Task OfferedWhenUserPrefersProgramMainAndTopLevelStatements_Suggestion()
        => new VerifyCS.Test
        {
            TestCode = """
            {|IDE0211:System|}.Console.WriteLine(0);
            """,
            FixedCode = """
            internal class Program
            {
                private static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task PreferNoAccessibility()
        => new VerifyCS.Test
        {
            TestCode = """
            {|IDE0211:System|}.Console.WriteLine(0);
            """,
            FixedCode = """
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
            Options =
            {
                { CodeStyleOptions2.AccessibilityModifiersRequired, AccessibilityModifiersRequired.Never },
                { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion },
            },
        }.RunAsync();

    [Fact]
    public Task TestWithExistingUsings()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            {|IDE0211:Console|}.WriteLine(0);
            """,
            FixedCode = """
            using System;

            internal class Program
            {
                private static void Main(string[] args)
                {
                    Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestWithNumericReturn()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            {|IDE0211:Console|}.WriteLine(0);

            return 0;
            """,
            FixedCode = """
            using System;

            internal class Program
            {
                private static int Main(string[] args)
                {
                    Console.WriteLine(0);

                    return 0;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestWithLocalFunction()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            {|IDE0211:Console|}.WriteLine(0);

            void M()
            {
            }

            return 0;
            """,
            FixedCode = """
            using System;

            internal class Program
            {
                private static int Main(string[] args)
                {
                    Console.WriteLine(0);

                    void M()
                    {
                    }

                    return 0;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestWithAwait()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            {|IDE0211:await|} Console.Out.WriteLineAsync();
            """,
            FixedCode = """
            using System;
            using System.Threading.Tasks;

            internal class Program
            {
                private static async Task Main(string[] args)
                {
                    await Console.Out.WriteLineAsync();
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestWithAwaitAndNumericReturn()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            {|IDE0211:await|} Console.Out.WriteLineAsync();

            return 0;
            """,
            FixedCode = """
            using System;
            using System.Threading.Tasks;

            internal class Program
            {
                private static async Task<int> Main(string[] args)
                {
                    await Console.Out.WriteLineAsync();

                    return 0;
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61126")]
    public Task TestNormalCommentStaysInsideMainIfTouchingStatement()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            // This comment probably describes logic of the statement below
            {|IDE0211:Console|}.WriteLine(0);
            """,
            FixedCode = """
            using System;

            internal class Program
            {
                private static void Main(string[] args)
                {
                    // This comment probably describes logic of the statement below
                    Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/61126")]
    public Task TestNormalCommentMovesIfNotTouching()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            // This comment probably does not describe the logic of the statement below

            {|IDE0211:Console|}.WriteLine(0);
            """,
            FixedCode = """
            using System;

            // This comment probably does not describe the logic of the statement below

            internal class Program
            {
                private static void Main(string[] args)
                {
                    Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestTopLevelStatementExplanationCommentRemoved()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            // See https://aka.ms/new-console-template for more information
            {|IDE0211:Console|}.WriteLine(0);
            """,
            FixedCode = """
            using System;

            internal class Program
            {
                private static void Main(string[] args)
                {
                    Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestTopLevelStatementExplanationCommentRemoved2()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            // See https://aka.ms/new-console-template for more information

            {|IDE0211:Console|}.WriteLine(0);
            """,
            FixedCode = """
            using System;

            internal class Program
            {
                private static void Main(string[] args)
                {
                    Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestTopLevelStatementExplanationCommentRemoved3()
        => new VerifyCS.Test
        {
            TestCode = """
            // See https://aka.ms/new-console-template for more information

            {|IDE0211:System|}.Console.WriteLine(0);
            """,
            FixedCode = """
            internal class Program
            {
                private static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestTopLevelStatementExplanationCommentRemoved4()
        => new VerifyCS.Test
        {
            TestCode = """
            // See https://aka.ms/new-console-template for more information
            {|IDE0211:System|}.Console.WriteLine(0);
            """,
            FixedCode = """
            internal class Program
            {
                private static void Main(string[] args)
                {
                    System.Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestPreprocessorDirective1()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            #if true

            {|IDE0211:Console|}.WriteLine(0);

            #endif
            """,
            FixedCode = """
            using System;

            #if true

            internal class Program
            {
                private static void Main(string[] args)
                {
                    Console.WriteLine(0);
                }
            }

            #endif
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestPreprocessorDirective2()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            #if true

            {|IDE0211:Console|}.WriteLine(0);

            return;

            #endif
            """,
            FixedCode = """
            using System;

            #if true

            internal class Program
            {
                private static void Main(string[] args)
                {
                    Console.WriteLine(0);

                    return;
                }
            }

            #endif
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/62943")]
    public Task TestHasExistingPart()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            {|IDE0211:Console|}.WriteLine(0);

            partial class Program
            {
                int x;
            }
            """,
            FixedCode = """
            using System;

            partial class Program
            {
                int x;

                private static void Main(string[] args)
                {
                    Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Theory, WorkItem("https://github.com/dotnet/roslyn/issues/62943")]
    [InlineData("public")]
    [InlineData("internal")]
    [InlineData("static")]
    [InlineData("abstract")]
    [InlineData("file")]
    public Task TestHasExistingPart_KeepsModifiers(string modifier)
        => new VerifyCS.Test
        {
            TestCode = $$"""
            using System;

            {|IDE0211:Console|}.WriteLine(0);

            {{modifier}} partial class Program
            {
                static int x;
            }
            """,
            FixedCode = $$"""
            using System;

            {{modifier}} partial class Program
            {
                static int x;

                private static void Main(string[] args)
                {
                    Console.WriteLine(0);
                }
            }
            """,
            LanguageVersion = LanguageVersion.CSharp11,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();

    [Fact]
    public Task TestBeforeExistingClass()
        => new VerifyCS.Test
        {
            TestCode = """
            using System;

            {|IDE0211:Console|}.WriteLine(0);

            class X
            {
                int x;
            }
            """,
            FixedCode = """
            using System;

            internal class Program
            {
                private static void Main(string[] args)
                {
                    Console.WriteLine(0);
                }
            }

            class X
            {
                int x;
            }
            """,
            LanguageVersion = LanguageVersion.CSharp9,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options = { { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion } },
        }.RunAsync();
}
