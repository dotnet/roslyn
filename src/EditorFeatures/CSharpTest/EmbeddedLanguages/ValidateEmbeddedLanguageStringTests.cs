// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.EmbeddedLanguages;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EmbeddedLanguages
{
    public class ValidateValidateEmbeddedLanguageStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpEmbeddedLanguageDiagnosticAnalyzer(), null);

        private IDictionary<OptionKey, object> OptionOn()
        {
            var optionsSet = new Dictionary<OptionKey, object>();
            optionsSet.Add(new OptionKey(JsonFeatureOptions.ReportInvalidJsonPatterns, LanguageNames.CSharp), true);
            optionsSet.Add(new OptionKey(JsonFeatureOptions.ReportInvalidJsonPatterns, LanguageNames.VisualBasic), true);
            return optionsSet;
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)]
        public async Task TestWarning1()
        {
            await TestDiagnosticInfoAsync(@"
class Program
{
    void Main()
    {
        var r = /*lang=json,strict*/ ""[|new|] Json()"";
    }     
}",
                options: OptionOn(),
                diagnosticId: JsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning,
                diagnosticMessage: string.Format(WorkspacesResources.JSON_issue_0, WorkspacesResources.Constructors_not_allowed));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)]
        public async Task TestWarning2()
        {
            await TestDiagnosticInfoAsync(@"
class Program
{
    void Main()
    {
        var r = /*lang=json*/ ""[|}|]"";
    }     
}",
                options: OptionOn(),
                diagnosticId: JsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning,
                diagnosticMessage: string.Format(WorkspacesResources.JSON_issue_0, 
                    string.Format(WorkspacesResources._0_unexpected, '}')));
        }
    }
}
