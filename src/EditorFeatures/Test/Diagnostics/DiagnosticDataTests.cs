// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Traits = Microsoft.CodeAnalysis.Test.Utilities.Traits;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
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

        [Fact, Trait(Test.Utilities.Traits.Feature, Test.Utilities.Traits.Features.Diagnostics)]
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
            using (var workspace = new TestWorkspace(TestExportProvider.ExportProviderWithCSharpAndVisualBasic))
            {
                var document = workspace.CurrentSolution.AddProject("TestProject", "TestProject", LanguageNames.CSharp).AddDocument("TestDocument", code);

                var data = new DiagnosticData(
                    "test1", "Test", "test1 message", "test1 message format",
                    DiagnosticSeverity.Info, false, 1,
                    workspace, document.Project.Id, new DiagnosticDataLocation(document.Id,
                        null, "originalFile1", startLine, startColumn, endLine, endColumn));

                var text = await document.GetTextAsync();
                var actual = data.GetExistingOrCalculatedTextSpan(text);

                Assert.Equal(span, actual);
            }
        }
    }
}
