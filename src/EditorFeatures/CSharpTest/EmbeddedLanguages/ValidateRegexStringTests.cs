﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions.LanguageServices;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EmbeddedLanguages
{
    [Trait(Traits.Feature, Traits.Features.ValidateRegexString)]
    public class ValidateRegexStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        public ValidateRegexStringTests(ITestOutputHelper logger)
           : base(logger)
        {
        }

        internal override (DiagnosticAnalyzer, CodeFixProvider?) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRegexDiagnosticAnalyzer(), null);

        private OptionsCollection OptionOn()
            => Option(IdeAnalyzerOptionsStorage.ReportInvalidRegexPatterns, true);

        [Fact]
        public async Task TestWarning1()
        {
            await TestDiagnosticInfoAsync("""
                using System.Text.RegularExpressions;

                class Program
                {
                    void Main()
                    {
                        var r = new Regex(@"[|)|]");
                    }     
                }
                """,
                globalOptions: OptionOn(),
                diagnosticId: AbstractRegexDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning,
                diagnosticMessage: string.Format(FeaturesResources.Regex_issue_0, FeaturesResources.Too_many_close_parens));
        }

        [Fact]
        public async Task TestWarning2()
        {
            await TestDiagnosticInfoAsync("""
                using System.Text.RegularExpressions;

                class Program
                {
                    void Main()
                    {
                        var r = new Regex("[|\u0029|]");
                    }     
                }
                """,
                globalOptions: OptionOn(),
                diagnosticId: AbstractRegexDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning,
                diagnosticMessage: string.Format(FeaturesResources.Regex_issue_0, FeaturesResources.Too_many_close_parens));
        }

        [Fact]
        public async Task TestWarningMissing1()
        {
            await TestDiagnosticMissingAsync("""
                using System.Text.RegularExpressions;

                class Program
                {
                    void Main()
                    {
                        var r = new Regex(@"[|\u0029|]");
                    }     
                }
                """);
        }
    }
}
