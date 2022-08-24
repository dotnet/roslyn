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

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Hover
{
    public class EditorConfigHoverTests : AbstractLanguageServerProtocolTests
    {
        private static readonly LSP.ClientCapabilities s_hoverCapabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities
            {
                Hover = new LSP.HoverSetting
                {
                    ContentFormat = new LSP.MarkupKind[]
                    {
                        LSP.MarkupKind.Markdown
                    }
                }
            }
        };

        [Fact]
        public async Task TestShowHoverOnWhitespaceSettingName()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
insert{|caret:_|}final_newline = true
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var expectedText = EditorConfigSettingsData.InsertFinalNewLine.GetSettingNameDocumentation();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(CompilerExtensionsResources.Insert_Final_Newline, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestShowHoverOnCodeStyleSettingName()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_style{|caret:_|}coalesce_expression = true
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(CompilerExtensionsResources.Prefer_coalesce_expression, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestShowHoverOnAnalyzerSettingName()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet{|caret:_|}diagnostic.CS0021.severity = warning
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.NotNull(results);
        }

        [Fact]
        public async Task TestDontShowHoverOnSettingName()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
unknown{|caret:_|}setting = 
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Null(results);
        }

        [Fact]
        public async Task TestShowHoverOnWhitespaceIntSettingValue()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
indent_size = {|caret:4|}
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Null(results);
        }

        [Fact]
        public async Task TestShowHoverOnWhitespaceBoolSettingValue()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
insert_final_newline = {|caret:t|}rue
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(CompilerExtensionsResources.Yes, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestShowHoverOnWhitespaceEnumSettingValue()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
csharp_space_around_binary_operators = {|caret:b|}efore_and_after
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(CompilerExtensionsResources.Before_and_after, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestShowHoverOnWhitespaceStringSettingValue()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
end_of_line = {|caret:c|}rlf
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(CompilerExtensionsResources.Crlf, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestShowHoverOnWhitespaceMulitpleEnumSettingValue()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
csharp_new_line_before_open_brace = accessors, {|caret:i|}ndexers
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(CompilerExtensionsResources.Indexers, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestShowHoverOnCodeStyleBoolSettingValue()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_style_qualification_for_field = {|caret:t|}rue
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(CompilerExtensionsResources.Prefer_this_or_Me, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestShowHoverOnCodeStyleEnumSettingValue()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_style_require_accessibility_modifiers = {|caret:f|}or_non_interface_members
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(CompilerExtensionsResources.For_non_interface_members, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestShowHoverOnAnalyzerSettingValue()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_diagnostic.CS0021.severity = {|caret:w|}arning
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Equal(CompilerExtensionsResources.Warning_severity, results.Contents.Third.Value);
        }

        [Fact]
        public async Task TestDontShowHoverOnSettingValue()
        {
            var markup = @"
<Workspace>
    <Project Language=""C#"" AssemblyName=""Assembly1"" CommonReferences=""true"" FilePath=""z:\TestProject\Assembly1.csproj"">
        <Document FilePath=""File1.cs"">
class C { }
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\TestProject\.editorconfig"">
dotnet_diagnostic.CS0021.severity = {|caret:u|}nknown
        </AnalyzerConfigDocument>
    </Project>
</Workspace>";

            using var testLspServer = await CreateXmltestLspServerAndUpdateWithAnalyzerReferences(markup);
            var expectedLocation = testLspServer.GetLocations("caret").Single();

            var results = await HoverTests.RunGetHoverAsync(
                testLspServer,
                expectedLocation).ConfigureAwait(false);
            Assert.Null(results);
        }

        internal async Task<TestLspServer> CreateXmltestLspServerAndUpdateWithAnalyzerReferences(string markup)
        {
            var testLspServer = await CreateXmlTestLspServerAsync(markup, initializationOptions: new InitializationOptions { ServerKind = WellKnownLspServerKinds.EditorConfigLspServer, ClientCapabilities = s_hoverCapabilities });
            var p = testLspServer.GetCurrentSolution().Projects.Single().WithAnalyzerReferences(new[] { TestAnalyzerReferences });
            await testLspServer.TestWorkspace.ChangeSolutionAsync(p.Solution).ConfigureAwait(false);

            return testLspServer;
        }
    }
}
