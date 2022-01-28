// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.EmbeddedLanguages
{
    public class ValidateValidateEmbeddedLanguageStringTests : AbstractCSharpDiagnosticProviderBasedUserDiagnosticTest
    {
        internal override (DiagnosticAnalyzer, CodeFixProvider) CreateDiagnosticProviderAndFixer(Workspace workspace)
            => (new CSharpEmbeddedLanguageDiagnosticAnalyzer(), null);

        private OptionsCollection OptionOn()
            => new(LanguageNames.CSharp)
            {
                { JsonFeatureOptions.ReportInvalidJsonPatterns, true }
            };

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
                diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0, FeaturesResources.Constructors_not_allowed));
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
                diagnosticMessage: string.Format(FeaturesResources.JSON_issue_0,
                    string.Format(FeaturesResources._0_unexpected, '}')));
        }
    }
}
