// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.InlayHint;

public sealed class CSharpInlayHintTests : AbstractInlayHintTests
{
    public CSharpInlayHintTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory]
    [InlineData("""string.Format(format: "{{escaped}} {1,-8:X4}{|hint:|} / {0:N1}{|hint:|}", arg0: 12.5m, arg1: 42)""", "→ 002A|→ 12.5", true)]
    [InlineData("""string.Format("\u007B0\u003AN2\u007D{|hint:|}", 12.5m)""", "→ 12.50", true)]
    [InlineData("""new System.Text.StringBuilder().AppendFormat(System.Globalization.CultureInfo.GetCultureInfo("fr-FR"), "{0:N2}{|hint:|}", 12.5m)""", "→ 12,50", true)]
    [InlineData("""System.Console.WriteLine("{0:D4}{|hint:|}", 42)""", "", true)]
    [InlineData("""new System.IO.StringWriter().Write("{0:X4}{|hint:|}", 42)""", "", true)]
    [InlineData("""string.Format("{3:X4}{|hint:|} {0:D2}{|hint:|}", new object[] { 1, 2, 3, 42 })""", "→ 002A|→ 01", true)]
    [InlineData("""string.Format("{0:N2}{|hint:|} {1:N2}", 12.5m, unknown)""", "→ 12.50", true)]
    [InlineData("""(42).ToString("'literal {0:N2}'0"){|hint:|}""", "→ literal {0:N2}42", true)]
    [InlineData("""string.Format("{{0:N2}}", 12.5m)""", "", true)]
    [InlineData("""string.Format("{0,+:N2} {0:N2}", 12.5m)""", "", true)]
    [InlineData("""string.Format(provider, "{0:N2}", 12.5m)""", "", true)]
    [InlineData("""string.Format("{0:D4}", 42)""", "", false)]
    public async Task CompositeFormatPreviewsUseItemPositions(string expression, string expected, bool enabled)
    {
        var markup = "class C { void M(decimal unknown, System.IFormatProvider provider) { " + expression + "; } }";
        var workspace = LspTestWorkspace.CreateWorkspaceElement(LanguageNames.CSharp, files: [markup]);
        var project = workspace.Element("Project")!;
        project.Element("Document")!.SetAttributeValue("FilePath", "/Format.cs");
        project.Add(new XElement("AnalyzerConfigDocument", new XAttribute("FilePath", "/.editorconfig"), """
            root = true
            [*.cs]
            dotnet_format_string_preview_hints =
            """ + (enabled ? "true" : "false")));
        await using var server = await CreateXmlTestLspServerAsync(workspace.ToString(), mutatingLspWorkspace: false);
        server.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp, false);
        server.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp, false);
        var document = server.GetCurrentSolution().Projects.Single().Documents.Single();
        var text = await document.GetTextAsync(CancellationToken.None);
        var request = new LSP.InlayHintParams
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI()),
            Range = ProtocolConversions.TextSpanToRange(new TextSpan(0, text.Length), text),
        };
        var hints = await server.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]>(
            LSP.Methods.TextDocumentInlayHintName, request, CancellationToken.None);
        Assert.NotNull(hints);
        Assert.Equal(expected.Length == 0 ? [] : expected.Split('|'), hints.Select(hint => hint.Label.First));
        if (expected.Length > 0)
            Assert.Equal(server.GetLocations("hint").Select(location => location.Range.Start), hints.Select(hint => hint.Position));
        foreach (var hint in hints)
        {
            var resolved = await server.ExecuteRequestAsync<LSP.InlayHint, LSP.InlayHint>(LSP.Methods.InlayHintResolveName, hint, CancellationToken.None);
            Assert.NotNull(resolved);
            Assert.NotNull(resolved.ToolTip);
        }
    }

    [Theory, CombinatorialData]
    public Task TestOneInlayParameterHintAsync(bool mutatingLspWorkspace)
        => RunVerifyInlayHintAsync("""
            class A
            {
                void M(int x)
                {
                }

                void M2()
                {
                    M({|x:|}5);
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestMultipleInlayParameterHintsAsync(bool mutatingLspWorkspace)
        => RunVerifyInlayHintAsync("""
            class A
            {
                void M(int a, double b, bool c)
                {
                }

                void M2()
                {
                    M({|a:|}5, {|b:|}5.5, {|c:|}true);
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestOneInlayTypeHintAsync(bool mutatingLspWorkspace)
        => RunVerifyInlayHintAsync("""
            class A
            {
                void M()
                {
                    var {|int:|}x = 5;
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestMultipleInlayTypeHintsAsync(bool mutatingLspWorkspace)
        => RunVerifyInlayHintAsync("""
            using System;
            class A
            {
                void M()
                {
                    var {|int:|}x = 5;
                    var {|object:|}obj = new Object();
                }
            }
            """, mutatingLspWorkspace);

    [Theory, CombinatorialData]
    public Task TestInlayTypeHintsDeconstructAsync(bool mutatingLspWorkspace)
        => RunVerifyInlayHintAsync("""
            class A
            {
                void X((int, bool) d)
                {
                    var (i, b) = d;
                }
            }
            """, mutatingLspWorkspace, hasTextEdits: false);

    [Theory, CombinatorialData]
    public async Task TestReturnsInlayHintsEvenIfCacheMisses(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    var {|int:|}x = 5;
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, CapabilitiesWithVSExtensions);
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp, true);
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp, true);
        var document = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single();
        var textDocument = CreateTextDocumentIdentifier(document.GetURI());
        var sourceText = await document.GetTextAsync();
        var span = TextSpan.FromBounds(0, sourceText.Length);

        var inlayHintParams = new LSP.InlayHintParams
        {
            TextDocument = textDocument,
            Range = ProtocolConversions.TextSpanToRange(span, sourceText)
        };

        var actualInlayHints = await testLspServer.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]?>(LSP.Methods.TextDocumentInlayHintName, inlayHintParams, CancellationToken.None);
        Assert.NotNull(actualInlayHints);
        var firstInlayHint = actualInlayHints.First();
        var data = JsonSerializer.Deserialize<InlayHintResolveData>(firstInlayHint.Data!.ToString()!, ProtocolConversions.LspJsonSerializerOptions);
        Assert.NotNull(data);
        var firstResultId = data.ResultId;

        // Verify the inlay hint item is in the cache.
        var cache = testLspServer.GetRequiredLspService<InlayHintCache>();
        Assert.NotNull(cache.GetCachedEntry(firstResultId));

        // Execute a few more requests to ensure the first request is removed from the cache.
        await testLspServer.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]?>(LSP.Methods.TextDocumentInlayHintName, inlayHintParams, CancellationToken.None);
        await testLspServer.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]?>(LSP.Methods.TextDocumentInlayHintName, inlayHintParams, CancellationToken.None);
        var lastInlayHints = await testLspServer.ExecuteRequestAsync<LSP.InlayHintParams, LSP.InlayHint[]?>(LSP.Methods.TextDocumentInlayHintName, inlayHintParams, CancellationToken.None);
        Assert.NotNull(lastInlayHints);
        Assert.True(lastInlayHints.Any());

        // Assert that the first result id is no longer in the cache.
        Assert.Null(cache.GetCachedEntry(firstResultId));

        // Assert that the resolve request returns the inlay hint even if not in the cache.
        var firstResolvedHint = await testLspServer.ExecuteRequestAsync<LSP.InlayHint, LSP.InlayHint>(LSP.Methods.InlayHintResolveName, firstInlayHint, CancellationToken.None);
        Assert.NotNull(firstResolvedHint?.ToolTip);
    }

    private async Task RunVerifyInlayHintAsync(string markup, bool mutatingLspWorkspace, bool hasTextEdits = true)
    {
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace,
            new LSP.VSInternalClientCapabilities
            {
                SupportsVisualStudioExtensions = true,
                Workspace = new WorkspaceClientCapabilities
                {
                    InlayHint = new InlayHintWorkspaceSetting
                    {
                        RefreshSupport = true
                    }
                }
            });
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForParameters, LanguageNames.CSharp, true);
        testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(InlineHintsOptionsStorage.EnabledForTypes, LanguageNames.CSharp, true);
        await VerifyInlayHintAsync(testLspServer, hasTextEdits);
    }
}
