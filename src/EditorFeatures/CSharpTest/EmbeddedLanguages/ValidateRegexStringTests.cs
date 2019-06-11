// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EmbeddedLanguages
{
    public class ValidateRegexStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpRegexDiagnosticAnalyzer(), null);

        private IDictionary<OptionKey, object> OptionOn()
        {
            var optionsSet = new Dictionary<OptionKey, object>();
            optionsSet.Add(new OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.CSharp), true);
            return optionsSet;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateRegexString)]
        public async Task TestWarning1()
        {
            await TestDiagnosticInfoAsync(@"
using System.Text.RegularExpressions;

class Program
{
    void Main()
    {
        var r = new Regex(@""[|)|]"");
    }     
}",
                options: OptionOn(),
                diagnosticId: AbstractRegexDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning,
                diagnosticMessage: string.Format(WorkspacesResources.Regex_issue_0, WorkspacesResources.Too_many_close_parens));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateRegexString)]
        public async Task TestWarning2()
        {
            await TestDiagnosticInfoAsync(@"
using System.Text.RegularExpressions;

class Program
{
    void Main()
    {
        var r = new Regex(""[|\u0029|]"");
    }     
}",
                options: OptionOn(),
                diagnosticId: AbstractRegexDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning,
                diagnosticMessage: string.Format(WorkspacesResources.Regex_issue_0, WorkspacesResources.Too_many_close_parens));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateRegexString)]
        public async Task TestWarningMissing1()
        {
            await TestDiagnosticMissingAsync(@"
using System.Text.RegularExpressions;

class Program
{
    void Main()
    {
        var r = new Regex(@""[|\u0029|]"");
    }     
}");
        }
    }
}
