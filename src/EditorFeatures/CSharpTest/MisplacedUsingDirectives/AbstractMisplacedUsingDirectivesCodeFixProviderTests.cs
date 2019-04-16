// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.MisplacedUsingDirectives;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.MisplacedUsingDirectives
{
    /// <summary>
    /// Base test class for the <see cref="MisplacedUsingDirectivesCodeFixProvider"/>.
    /// </summary>
    public abstract class AbstractMisplacedUsingDirectivesCodeFixProviderTests
    {
        internal static readonly CodeStyleOption<AddImportPlacement> PreservePlacementOption =
           new CodeStyleOption<AddImportPlacement>(AddImportPlacement.Preserve, NotificationOption.None);

        internal static readonly CodeStyleOption<AddImportPlacement> InsideNamespaceOption =
            new CodeStyleOption<AddImportPlacement>(AddImportPlacement.InsideNamespace, NotificationOption.Error);

        internal static readonly CodeStyleOption<AddImportPlacement> OutsideNamespaceOption =
            new CodeStyleOption<AddImportPlacement>(AddImportPlacement.OutsideNamespace, NotificationOption.Error);


        protected const string ClassDefinition = @"public class TestClass
{
}";

        protected const string StructDefinition = @"public struct TestStruct
{
}";

        protected const string InterfaceDefinition = @"public interface TestInterface
{
}";

        protected const string EnumDefinition = @"public enum TestEnum
{
    TestValue
}";

        protected const string DelegateDefinition = @"public delegate void TestDelegate();";

        protected abstract DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor);
        protected abstract CodeFixTest<XUnitVerifier> CreateTest((string filename, string content) sourceFile, string fixedSource);

        private protected Task VerifyAnalyzerAsync(string source, CodeStyleOption<AddImportPlacement> usingPlacement, DiagnosticResult[] expected)
        {
            return VerifyCodeFixAsync(source, usingPlacement, expected, fixedSource: null, placeSystemNamespaceFirst: false);
        }

        private protected Task VerifyCodeFixAsync(string source, CodeStyleOption<AddImportPlacement> usingPlacement, DiagnosticResult[] expected, string fixedSource, bool placeSystemNamespaceFirst)
        {
            return VerifyCodeFixAsync(source, usingPlacement, expected, fixedSource, remaining: DiagnosticResult.EmptyDiagnosticResults, placeSystemNamespaceFirst);
        }

        private protected async Task VerifyCodeFixAsync(string source, CodeStyleOption<AddImportPlacement> usingPlacement, DiagnosticResult[] expected, string fixedSource, DiagnosticResult[] remaining, bool placeSystemNamespaceFirst)
        {
            // Create the .editorconfig with the necessary code style settings.
            var editorConfig = $@"
root = true

[*.cs]
dotnet_sort_system_directives_first = {placeSystemNamespaceFirst}
csharp_using_directive_placement = {CSharpCodeStyleOptions.GetUsingDirectivesPlacementEditorConfigString(usingPlacement)}
";

            // Capture the working directory so it can be restored after the test runs.
            var workingDirectory = Environment.CurrentDirectory;

            // Create a temporary folder so that the coding conventions library can be
            // used to read code style preferences from an .editorconfig.
            var testDirectoryName = Path.GetRandomFileName();
            Directory.CreateDirectory(testDirectoryName);
            try
            {
                // Change the working directory to our test directory so that we 
                // can find the .editorconfig from the analyzer to get our 
                // code style settings.
                Environment.CurrentDirectory = testDirectoryName;

                File.WriteAllText(".editorconfig", editorConfig);

                // The contents of this file are ignored, but the coding conventions 
                // library checks for existence before .editorconfig is used.
                File.WriteAllText("Test0.cs", string.Empty);

                // Do not specify the full path to the source file since only the
                // filename will be copied causing mismatch when verifying the fixed state.
                var test = CreateTest(("Test0.cs", source), fixedSource);

                // Set code style settings in the OptionSet so that the CodeFix can
                // access the settings.
                test.OptionsTransforms.Add(
                    optionsSet => optionsSet.WithChangedOption(GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp, placeSystemNamespaceFirst)
                        .WithChangedOption(CSharpCodeStyleOptions.PreferredUsingDirectivePlacement, usingPlacement));

                // Fix the severity of expected diagnostics.
                var fixedExpectedResults = expected.Select(
                    result => result.WithSeverity(usingPlacement.Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));
                test.ExpectedDiagnostics.AddRange(fixedExpectedResults);

                // Fix the severity of remaining diagnostics.
                var fixedRemainingResults = remaining.Select(
                    result => result.WithSeverity(usingPlacement.Notification.Severity.ToDiagnosticSeverity() ?? DiagnosticSeverity.Hidden));
                test.FixedState.ExpectedDiagnostics.AddRange(fixedRemainingResults);

                await test.RunAsync();
            }
            finally
            {
                // Clean up by resetting the working directory and deleting
                // the temporary folder.
                Environment.CurrentDirectory = workingDirectory;
                try
                {
                    Directory.Delete(testDirectoryName, true);
                }
                catch { }
            }
        }
    }
}
