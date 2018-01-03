// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ValidateRegexString;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.RegularExpressions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ValidateRegexString
{
    public class ValidateRegexStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpValidateRegexStringDiagnosticAnalyzer(), null);

        private IDictionary<OptionKey, object> OptionOn()
        {
            var optionsSet = new Dictionary<OptionKey, object>();
            optionsSet.Add(new OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.CSharp), true);
            optionsSet.Add(new OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.VisualBasic), true);
            return optionsSet;
        }

        private IDictionary<OptionKey, object> OptionOff()
        {
            var optionsSet = new Dictionary<OptionKey, object>();
            optionsSet.Add(new OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.CSharp), false);
            optionsSet.Add(new OptionKey(RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.VisualBasic), false);
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
                diagnosticId: IDEDiagnosticIds.RegexPatternDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning,
                diagnosticMessage: string.Format(FeaturesResources.Regex_issue_0, WorkspacesResources.Too_many_close_parens));
        }
    }
}
