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

using VerifyCS = CSharpCodeRefactoringVerifier<ConvertToTopLevelStatementsCodeRefactoringProvider>;

[UseExportProvider]
public class ConvertToTopLevelStatementsRefactoringTests
{
    [Fact]
    public async Task TestNotOnEmptyFile()
    {
        var code = @"
$$
";

        // default preference is to prefer top level namespaces.  As such, we should not have the refactoring here
        // since the analyzer will take over.
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            ExpectedDiagnostics =
            {
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                DiagnosticResult.CompilerError("CS5001"),
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestConvertToTopLevelStatementsWithDefaultTopLevelStatementPreference()
    {
        var code = @"
class Program
{
    static void $$Main(string[] args)
    {
        System.Console.WriteLine(args[0]);
    }
}";

        // default preference is to prefer top level namespaces.  As such, we should not have the refactoring here
        // since the analyzer will take over.
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
        }.RunAsync();
    }

    [Fact]
    public async Task TestConvertToTopLevelStatementsWithProgramMainPreferenceSuggestion()
    {
        // user actually prefers Program.Main.  As such, we only offer to convert to the alternative as a refactoring.
        await new VerifyCS.Test
        {
            TestCode = @"
class Program
{
    static void $$Main(string[] args)
    {
        System.Console.WriteLine(args[0]);
    }
}
",
            FixedCode = @"
System.Console.WriteLine(args[0]);
",
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion },
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotOfferedInLibrary()
    {
        var code = @"
class Program
{
    static void $$Main(string[] args)
    {
        System.Console.WriteLine(args[0]);
    }
}
";

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion },
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNotWithNonViableType()
    {
        var code = @"
class Program
{
    void $$Main(string[] args)
    {
        System.Console.WriteLine(args[0]);
    }
}
";

        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion },
            },
            ExpectedDiagnostics =
            {
                // error CS5001: Program does not contain a static 'Main' method suitable for an entry point
                DiagnosticResult.CompilerError("CS5001"),
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoConvertToTopLevelStatementsWithProgramMainPreferenceSuggestionBeforeCSharp9()
    {
        var code = @"
class Program
{
    static void $$Main(string[] args)
    {
        System.Console.WriteLine(args[0]);
    }
}
";

        // user actually prefers Program.Main.  As such, we only offer to convert to the alternative as a refactoring.
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp8,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Suggestion },
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoConvertToTopLevelStatementsWithTopLevelStatementsPreferenceSuggestion()
    {
        var code = @"
class Program
{
    static void $$Main(string[] args)
    {
        System.Console.WriteLine(args[0]);
    }
}
";
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion },
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestNoConvertToTopLevelStatementsWithTopLevelStatementsPreferenceSilent()
    {
        var code = @"
class Program
{
    static void $$Main(string[] args)
    {
        System.Console.WriteLine(args[0]);
    }
}
";
        await new VerifyCS.Test
        {
            TestCode = code,
            FixedCode = code,
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Silent },
            }
        }.RunAsync();
    }

    [Fact]
    public async Task TestConvertToTopLevelStatementWithTopLevelStatementPreferenceSuppress()
    {
        // if the user has the analyzer suppressed, then we want to supply teh refactoring.
        await new VerifyCS.Test
        {
            TestCode = @"
internal class Program
{
    private static void $$Main(string[] args)
    {
        System.Console.WriteLine(0);
    }
}
",
            FixedCode = @"
System.Console.WriteLine(0);
",
            LanguageVersion = LanguageVersion.CSharp10,
            TestState = { OutputKind = OutputKind.ConsoleApplication },
            Options =
            {
                { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.None },
            }
        }.RunAsync();
    }
}
