﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [UseExportProvider]
    public class DiagnosticDataTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText()
        {
            var code = "";
            await VerifyTextSpanAsync(code, 10, 10, 20, 20, new TextSpan(0, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText1()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 30, 30, 40, 40, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText2()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, -1, 30, 40, 40, new TextSpan(0, code.Length));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText3()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, -1, 30, -1, 40, new TextSpan(0, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText4()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 1, 30, -1, 40, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText5()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 1, 30, 1, 40, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText6()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 1, 30, 2, 40, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText7()
        {
            var code = @"
";

            await VerifyTextSpanAsync(code, 1, 0, 1, 2, new TextSpan(code.Length, 0));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task DiagnosticData_GetText8()
        {
            var code = @"
namespace B
{
    class A
    {
    }
}
";

            await VerifyTextSpanAsync(code, 3, 10, 3, 11, new TextSpan(28, 1));
        }

        private static async Task VerifyTextSpanAsync(string code, int startLine, int startColumn, int endLine, int endColumn, TextSpan span)
        {
            using var workspace = new TestWorkspace(TestExportProvider.ExportProviderWithCSharpAndVisualBasic);
            var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", code);

            var data = new DiagnosticData(
                id: "test1",
                category: "Test",
                message: "test1 message",
                enuMessageForBingSearch: "test1 message format",
                severity: DiagnosticSeverity.Info,
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: false,
                warningLevel: 1,
                projectId: document.Project.Id,
                customTags: ImmutableArray<string>.Empty,
                properties: ImmutableDictionary<string, string>.Empty,
                location: new DiagnosticDataLocation(document.Id, null, "originalFile1", startLine, startColumn, endLine, endColumn),
                language: document.Project.Language);

            var text = await document.GetTextAsync();
            var actual = DiagnosticData.GetExistingOrCalculatedTextSpan(data.DataLocation, text);

            Assert.Equal(span, actual);
        }
    }
}
