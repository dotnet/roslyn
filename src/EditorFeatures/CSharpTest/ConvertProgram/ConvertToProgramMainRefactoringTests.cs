// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.ConvertProgram;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ConvertProgram
{
    using VerifyCS = CSharpCodeRefactoringVerifier<ConvertToProgramMainCodeRefactoringProvider>;

    public class ConvertToProgramMainRefactoringTests
    {
        [Fact]
        public async Task TestConvertToProgramMainWithDefaultTopLevelStatementPreference()
        {
            // default preference is to prefer top level namespaces.  As such, we only offer to convert to the alternative as a refactoring.
            await new VerifyCS.Test
            {
                TestCode = @"
$$System.Console.WriteLine(0);
",
                FixedCode = @"
internal class Program
{
    private static void Main(string[] args)
    {
        System.Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToProgramMainWithTopLevelStatementPreferenceSuggestion()
        {
            // user actually prefers top level statements.  As such, we only offer to convert to the alternative as a refactoring.
            await new VerifyCS.Test
            {
                TestCode = @"
$$System.Console.WriteLine(0);
",
                FixedCode = @"
internal class Program
{
    private static void Main(string[] args)
    {
        System.Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options =
                {
                    { CSharpCodeStyleOptions.PreferTopLevelStatements, true, NotificationOption2.Suggestion },
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertToProgramMainWithProgramMainPreferenceSuggestion()
        {
            var code = @"
$$System.Console.WriteLine(0);
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
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestNoConvertToProgramMainWithProgramMainPreferenceSilent()
        {
            var code = @"
$$System.Console.WriteLine(0);
";
            await new VerifyCS.Test
            {
                TestCode = code,
                FixedCode = code,
                LanguageVersion = LanguageVersion.CSharp10,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options =
                {
                    { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.Silent },
                }
            }.RunAsync();
        }

        [Fact]
        public async Task TestConvertToProgramMainWithProgramMainPreferenceSuppress()
        {
            // if the user has the analyzer suppressed, then we want to supply teh refactoring.
            await new VerifyCS.Test
            {
                TestCode = @"
$$System.Console.WriteLine(0);
",
                FixedCode = @"
internal class Program
{
    private static void Main(string[] args)
    {
        System.Console.WriteLine(0);
    }
}",
                LanguageVersion = LanguageVersion.CSharp10,
                TestState = { OutputKind = OutputKind.ConsoleApplication },
                Options =
                {
                    { CSharpCodeStyleOptions.PreferTopLevelStatements, false, NotificationOption2.None },
                }
            }.RunAsync();
        }
    }
}
