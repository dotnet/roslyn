// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertProgram;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertProgram;

using VerifyCS = CSharpCodeRefactoringVerifier<ConvertToProgramMainCodeRefactoringProvider>;

[UseExportProvider]
public sealed class ConvertToProgramMainRefactoringTests
{
    [Fact]
    public Task TestNotOnFileWithNoGlobalStatements()
        => new VerifyCS.Test
        {
            TestCode = """
            $$
            class C
            {
            }
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            ExpectedDiagnostics =
            {
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                DiagnosticResult.CompilerError("CS5001"),
            }
        }.RunAsync();

    [Fact]
    public Task TestNotOnEmptyFile()
        => new VerifyCS.Test
        {
            TestCode = """
            $$
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            ExpectedDiagnostics =
            {
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                DiagnosticResult.CompilerError("CS5001"),
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToProgramMainWithDefaultTopLevelStatementPreference()
        => new VerifyCS.Test
        {
            TestCode = """
            $$System.Console.WriteLine(0);
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
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
        }.RunAsync();

    [Fact]
    public Task TestNotOfferedInLibrary()
        => new VerifyCS.Test
        {
            TestCode = """
            $${|CS8805:System.Console.WriteLine(0);|}
            """,
            LanguageVersion = LanguageVersion.CSharp10,
        }.RunAsync();

    [Fact]
    public Task TestConvertToProgramMainWithTopLevelStatementPreferenceSuggestion()
        => new VerifyCS.Test
        {
            TestCode = """
            $$System.Console.WriteLine(0);
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
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion },
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertToProgramMainWithProgramMainPreferenceSuggestion()
        => new VerifyCS.Test
        {
            TestCode = """
            $$System.Console.WriteLine(0);
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion },
            }
        }.RunAsync();

    [Fact]
    public Task TestNoConvertToProgramMainWithProgramMainPreferenceSilent()
        => new VerifyCS.Test
        {
            TestCode = """
            $$System.Console.WriteLine(0);
            """,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent },
            }
        }.RunAsync();

    [Fact]
    public Task TestConvertToProgramMainWithProgramMainPreferenceSuppress()
        => new VerifyCS.Test
        {
            TestCode = """
            $$System.Console.WriteLine(0);
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
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.None },
            }
        }.RunAsync();
}
