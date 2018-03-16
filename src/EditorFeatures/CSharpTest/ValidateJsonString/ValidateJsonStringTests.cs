// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.ValidateJsonString;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Json;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ValidateJsonString
{
    public class ValidateJsonStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpValidateJsonStringDiagnosticAnalyzer(), null);

        private IDictionary<OptionKey, object> OptionOn()
        {
            var optionsSet = new Dictionary<OptionKey, object>();
            optionsSet.Add(new OptionKey(JsonOptions.ReportInvalidJsonPatterns, LanguageNames.CSharp), true);
            optionsSet.Add(new OptionKey(JsonOptions.ReportInvalidJsonPatterns, LanguageNames.VisualBasic), true);
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
                diagnosticId: IDEDiagnosticIds.JsonPatternDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning,
                diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0, WorkspacesResources.Constructors_not_allowed));
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
                diagnosticId: IDEDiagnosticIds.JsonPatternDiagnosticId,
                diagnosticSeverity: DiagnosticSeverity.Warning,
                diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0, 
                    string.Format(WorkspacesResources._0_unexpected, '}')));
        }
    }
}
