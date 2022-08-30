// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditorConfigSettings;
using Microsoft.CodeAnalysis.CSharp.EditorConfigSettings;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using System;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Diagnostics
{
    public class EditorConfigDiagnosticTests : AbstractPullDiagnosticTestsBase
    {
        private static readonly LSP.VSInternalClientCapabilities s_diagnosticCapabilities = new LSP.VSInternalClientCapabilities
        {
            SupportsVisualStudioExtensions = true,
            SupportsDiagnosticRequests = true,
        };

        [Fact]
        public async Task TestWrongValueInIntWhitespaceSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
indent_size = true
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0003");
        }

        [Fact]
        public async Task TestWrongValueInBoolWhitespaceSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
csharp_new_line_before_else = yes
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0003");
        }

        [Fact]
        public async Task TestWrongValueInStringWhitespaceSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
end_of_line = true
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0003");
        }

        [Fact]
        public async Task TestWrongValueInEnumWhitespaceSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
csharp_prefer_braces = 4
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0003");
        }

        [Fact]
        public async Task TestWrongValueInBoolCodeStyleSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
csharp_style_prefer_null_check_over_type_check = ignore
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0003");
        }

        [Fact]
        public async Task TestWrongValueInEnumCodeStyleSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
csharp_using_directive_placement = true
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0003");
        }

        [Fact]
        public async Task TestRepeatedSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
indent_size = 2
indent_size = 4
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0009");
        }

        [Fact]
        public async Task TestRepeatedValuesinSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
csharp_new_line_before_open_brace = indexers, indexers
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0010");
        }

        [Fact]
        public async Task TestNonSupportedSeveritiesInSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
csharp_space_around_binary_operators = ignore:warning
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0004");
        }

        [Fact]
        public async Task TestNonSupportedMultipleValuesInSettingDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
dotnet_style_parentheses_in_arithmetic_binary_operators = always_for_clarity, never_if_unnecessary
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0005");
        }

        [Fact]
        public async Task TestIncorrectSettingDefinitionDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
indent_size = 2 =
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Equal(1, results.Length);
            Assert.Equal(results.Single().Diagnostics.Single().Code, "EC0001");
        }

        [Fact]
        public async Task TestCorrectSettingDefinitionDiagnostic()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
[*.cs]
indent_size = 2 # Defining indent size
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);

            var document = testLspServer.GetCurrentSolution().Projects.Single().AnalyzerConfigDocuments.Single();
            await testLspServer.OpenDocumentAsync(document.GetURI());

            var results = await RunGetDocumentPullDiagnosticsAsync(testLspServer, document.GetURI(), true);

            Assert.Empty(results.Single().Diagnostics);
        }

        internal async Task<TestLspServer> CreateXmltestLspServerAndUpdateWithAnalyzerReferences(string markup)
        {
            var testLspServer = await CreateXmlTestLspServerAsync(markup, initializationOptions: new InitializationOptions
            {
                ServerKind = WellKnownLspServerKinds.EditorConfigLspServer,
                ClientCapabilities = s_diagnosticCapabilities,
                OptionUpdater = (globalOptions) =>
                {
                    globalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp), BackgroundAnalysisScope.FullSolution);
                    globalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic), BackgroundAnalysisScope.FullSolution);
                    globalOptions.SetGlobalOption(new OptionKey(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, InternalLanguageNames.TypeScript), BackgroundAnalysisScope.FullSolution);
                    globalOptions.SetGlobalOption(new OptionKey(InternalDiagnosticsOptions.NormalDiagnosticMode), DiagnosticMode.Pull);
                },
                SourceGeneratedMarkups = Array.Empty<string>(),
            });

            return testLspServer;
        }
    }
}
