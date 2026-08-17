// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Mef;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.Snippets;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using RoslynSnippets = Microsoft.CodeAnalysis.Snippets;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostInlineCompletionEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task Constructor()
        => VerifyInlineCompletionAsync(
            input: """
                <div></div>

                @code
                {
                    ctor$$
                }
                """,
            output: """
                <div></div>

                @code
                {
                    public File1()
                    {
                        $0
                    }
                }
                """);

    [Fact]
    public Task Constructor_SmallIndent()
        => VerifyInlineCompletionAsync(
            input: """
                <div></div>

                @code
                {
                  ctor$$
                }
                """,
            output: """
                <div></div>

                @code
                {
                  public File1()
                  {
                    $0
                  }
                }
                """,
            tabSize: 2);

    [Fact]
    public Task InHtml_DoesNothing()
        => VerifyInlineCompletionAsync(
            input: """
                <div>ctor$$</div>
                """);

    private async Task VerifyInlineCompletionAsync(TestCode input, string? output = null, int tabSize = 4)
    {
        var document = CreateProjectAndRazorDocument(input.Text);
        var inputText = await document.GetTextAsync(DisposalToken);
        var position = inputText.GetLinePosition(input.Position);

        ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().ClientSpaceSettings with { IndentSize = tabSize });
        var endpoint = new CohostInlineCompletionEndpoint(IncompatibleProjectService, RemoteServiceInvoker, ClientSettingsManager);

        var list = await endpoint.GetTestAccessor().HandleRequestAsync(document, position, ClientSettingsManager.GetClientSettings().ToRazorFormattingOptions().ToLspFormattingOptions(), DisposalToken);

        if (output is null)
        {
            Assert.Null(list);
            return;
        }

        Assert.NotNull(list);

        // Asserting Roslyn invariants, which won't necessarily break us, but will mean we're lacking test coverage
        var item = Assert.Single(list.Items);
        Assert.Equal(InsertTextFormat.Snippet, item.TextFormat);
        Assert.NotNull(item.Range);
        Assert.Null(item.Command);

        var change = new TextChange(inputText.GetTextSpan(item.Range), item.Text);

        inputText = inputText.WithChanges([change]);
        AssertEx.EqualOrDiff(output, inputText.ToString());
    }

    private protected override TestComposition ConfigureLocalComposition(TestComposition composition)
    {
        return composition.AddParts(typeof(TestSnippetInfoService));
    }

    [ExportLanguageService(typeof(RoslynSnippets.ISnippetInfoService), LanguageNames.CSharp), Shared, PartNotDiscoverable]
    private class TestSnippetInfoService : RoslynSnippets.ISnippetInfoService
    {
        public IEnumerable<RoslynSnippets.SnippetInfo> GetSnippetsIfAvailable()
        {
            var snippetsFile = GetTestSnippetsFile();
            var testSnippetsXml = XDocument.Load(snippetsFile);
            var snippets = XmlSnippetParser.CodeSnippet.ReadSnippets(testSnippetsXml);

            return snippets.Select(s => new RoslynSnippets.SnippetInfo(s.Shortcut, s.Title, s.Title, snippetsFile));
        }

        private static string GetTestSnippetsFile()
        {
            var appContextBaseDirectory = AppContext.BaseDirectory;
            var currentDirectory = Environment.CurrentDirectory;

            var snippetsFile = TryFindTestSnippetsFile(appContextBaseDirectory)
                ?? (string.Equals(currentDirectory, appContextBaseDirectory, StringComparison.OrdinalIgnoreCase)
                    ? null
                    : TryFindTestSnippetsFile(currentDirectory));

            if (snippetsFile is null)
            {
                throw new InvalidOperationException($"Could not find test snippets file from '{appContextBaseDirectory}' or '{currentDirectory}'.");
            }

            return snippetsFile;
        }

        private static string? TryFindTestSnippetsFile(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory) || !Directory.Exists(baseDirectory))
            {
                return null;
            }

            var outputLocalPath = Path.Combine(baseDirectory, "Cohost", "TestSnippets.snippet");
            if (File.Exists(outputLocalPath))
            {
                return outputLocalPath;
            }

            var repoRoot = SearchUp(baseDirectory, "global.json");
            if (repoRoot is not null)
            {
                var razorRepoRoot = Directory.Exists(Path.Combine(repoRoot, "src", "Razor", "src"))
                    ? Path.Combine(repoRoot, "src", "Razor")
                    : repoRoot;
                foreach (var candidatePath in new[]
                {
                    Path.Combine(razorRepoRoot, "src", "Razor", "test", "Microsoft.VisualStudio.LanguageServices.Razor.UnitTests", "Cohost", "TestSnippets.snippet"),
                    Path.Combine(razorRepoRoot, "src", "Razor", "test", "Microsoft.VisualStudio.LanguageServices.Razor.Test", "Cohost", "TestSnippets.snippet"),
                })
                {
                    if (File.Exists(candidatePath))
                    {
                        return candidatePath;
                    }
                }
            }

            foreach (var candidatePath in Directory.EnumerateFiles(baseDirectory, "TestSnippets.snippet", SearchOption.AllDirectories))
            {
                return candidatePath;
            }

            return null;
        }

        private static string? SearchUp(string baseDirectory, string fileName)
        {
            for (var current = new DirectoryInfo(baseDirectory); current is not null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, fileName)))
                {
                    return current.FullName;
                }
            }

            return null;
        }

        public bool ShouldFormatSnippet(RoslynSnippets.SnippetInfo snippetInfo)
        {
            throw new System.NotImplementedException();
        }

        public bool SnippetShortcutExists_NonBlocking(string? shortcut)
        {
            throw new System.NotImplementedException();
        }
    }
}
