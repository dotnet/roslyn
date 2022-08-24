// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion
{
    public class EditorConfigCompletionTests : AbstractLanguageServerProtocolTests
    {
        private static readonly LSP.ClientCapabilities s_completionCapabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities
            {
                Completion = new LSP.CompletionSetting
                {
                    CompletionListSetting = new LSP.CompletionListSetting
                    {
                        ItemDefaults = new string[] { CompletionHandler.EditRangeSetting }
                    },
                },
            }
        };

        [Fact]
        public async Task TestShowSettingsNameInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
{|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.NotEmpty(results.Items);
        }

        [Fact]
        public async Task TestWhitespaceSettingsNameInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
indent_siz{|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "z",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Contains(results.Items, item => item.Label == "indent_size");
        }

        [Fact]
        public async Task TestAnalyzerSettingsNameInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
CS0021{|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "1",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Contains(results.Items, item => item.Label == "dotnet_diagnostic.CS0021.severity");
        }

        [Fact]
        public async Task TestCodeStyleSettingsNameInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
unused_parameters{|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "s",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Contains(results.Items, item => item.Label == "dotnet_code_quality_unused_parameters");
        }

        [Fact]
        public async Task TestDontShowSettingsNameInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
indent_size d{|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "d",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Null(results);
        }

        [Fact]
        public async Task TestWhitespaceSettingsIntValuesInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
indent_size={|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(3, results.Items.Length);
        }

        [Fact]
        public async Task TestWhitespaceSettingsBoolValuesInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
csharp_space_after_dot={|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(2, results.Items.Length);
        }

        [Fact]
        public async Task TestWhitespaceSettingsEnumValuesInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_style_operator_placement_when_wrapping={|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(2, results.Items.Length);
        }

        [Fact]
        public async Task TestWhitespaceSettingsMulitpleEnumValuesInEditorconfigFiles1()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
csharp_new_line_before_open_brace={|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(12, results.Items.Length);
        }

        [Fact]
        public async Task TestWhitespaceSettingsMulitpleEnumValuesInEditorconfigFiles2()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
csharp_new_line_before_open_brace=accessors,{|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(12, results.Items.Length);
        }

        [Fact]
        public async Task TestWhitespaceSettingsMulitpleEnumValuesInEditorconfigFiles3()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
csharp_space_between_parentheses={|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(3, results.Items.Length);
        }

        [Fact]
        public async Task TestCodeStyleSettingsBoolValuesInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_style_prefer_simplified_interpolation={|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(2, results.Items.Length);
        }

        [Fact]
        public async Task TestCodeStyleSettingsEnumValuesInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_style_parentheses_in_arithmetic_binary_operators={|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(2, results.Items.Length);
        }

        [Fact]
        public async Task TestAnalyzerSettingsValuesInEditorconfigFiles()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_diagnostic.CS0021.severity={|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "z",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(4, results.Items.Length);
        }

        [Fact]
        public async Task TestDontShowSettingsValuesInEditorconfigFiles1()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
unknown_setting={|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Null(results);
        }

        [Fact]
        public async Task TestDontShowSettingsValuesInEditorconfigFiles2()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_diagnostic.CS0037.severity=none {|caret:|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";
            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "=",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await CompletionTests.RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Null(results);
        }

        internal async Task<TestLspServer> CreateXmltestLspServerAndUpdateWithAnalyzerReferences(string markup)
        {
            var testLspServer = await CreateXmlTestLspServerAsync(markup, initializationOptions: new InitializationOptions { ServerKind = WellKnownLspServerKinds.EditorConfigLspServer, ClientCapabilities = s_completionCapabilities });
            var p = testLspServer.GetCurrentSolution().Projects.Single().WithAnalyzerReferences(new[] { TestAnalyzerReferences });
            await testLspServer.TestWorkspace.ChangeSolutionAsync(p.Solution).ConfigureAwait(false);

            return testLspServer;
        }
    }
}
