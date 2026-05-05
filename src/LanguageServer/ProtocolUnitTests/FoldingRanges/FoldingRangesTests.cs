// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.FoldingRanges;

public sealed class FoldingRangesTests : AbstractLanguageServerProtocolTests
{
    public FoldingRangesTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public Task TestGetFoldingRangeAsync_Imports(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            using {|imports:System;
            using System.Linq;|}
            """);

    [Theory(Skip = "GetFoldingRangeAsync does not yet support comments."), CombinatorialData]
    public Task TestGetFoldingRangeAsync_Comments(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            {|foldingRange:// A comment|}
            {|foldingRange:/* A multiline
            comment */|}
            """);

    [Theory, CombinatorialData]
    public Task TestGetFoldingRangeAsync_Regions(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            {|region:#region ARegion
            #endregion|}
            """, "ARegion");

    [Theory, CombinatorialData]
    public Task TestGetFoldingRangeAsync_Members(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            class C{|foldingRange:
            {
                public void M(){|implementation:
                {
                }|}
            }|}
            """);

    [Theory, CombinatorialData]
    public Task TestGetFoldingRangeAsync_AutoCollapse(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            class C{|foldingRange:
            {
                private Action<int> Foo(){|implementation: => i =>{|foldingRange:
                {
                };|}|}
            }|}
            """);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/7974")]
    public Task TestGetFoldingRangeAsync_LineFoldingOnly_NoOverlappingRanges(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            class C{|foldingRange:
            {
                public void M1(){|implementation:
                {
                    var x = 1;
                }|}
                public void M2(){|implementation:
                {
                    var y = 2;
                }|}
            }|}
            """, lineFoldingOnly: true);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/7974")]
    public Task TestGetFoldingRangeAsync_LineFoldingOnly_StartLineOverlapsChoosesInner(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            class C { public void M1() {|implementation:{
                    var x = 1;
                }|}
            }
            """, lineFoldingOnly: true);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/7974")]
    public Task TestGetFoldingRangeAsync_LineFoldingOnly_EndLineOverlaps(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            class C{|foldingRange:
            {
                void M(){|implementation:
                {
                    void Local(){|foldingRange:
                    {
                    }|}}|}}|}
            """, lineFoldingOnly: true);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/7974")]
    public Task TestGetFoldingRangeAsync_LineFoldingOnly_EndLineOverlapsStartLine(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            class C{|foldingRange:
            {
                void M(){|implementation:
                {
                    if (true){|foldingRange:
                    {|}
                    } else{|foldingRange: {
                    }|}
                }|}
            }|}
            """, lineFoldingOnly: true);

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/vscode-csharp/issues/7974")]
    public Task TestGetFoldingRangeAsync_WithoutLineFoldingOnly_AllowsRangesOnSameLine(bool mutatingLspWorkspace)
        => AssertFoldingRanges(mutatingLspWorkspace, """
            class C {|foldingRange:{ public void M1() {|implementation:{
                    if (true){|foldingRange:
                    {
                    }|} else{|foldingRange: {
                    }|}
                }|}
            }|}
            """, lineFoldingOnly: false);

    private async Task AssertFoldingRanges(
        bool mutatingLspWorkspace,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup,
        string collapsedText = null,
        bool lineFoldingOnly = false)
    {
        var clientCapabilities = new LSP.ClientCapabilities
        {
            TextDocument = new LSP.TextDocumentClientCapabilities
            {
                FoldingRange = new LSP.FoldingRangeSetting
                {
                    LineFoldingOnly = lineFoldingOnly
                }
            }
        };

        var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
        var expected = testLspServer.GetLocations()
            .SelectMany(kvp => kvp.Value.Select(location => CreateFoldingRange(kvp.Key, location.Range, collapsedText ?? "...", lineFoldingOnly)))
            .OrderByDescending(range => range.StartLine)
            .ThenByDescending(range => range.StartCharacter)
            .ToArray();

        var results = await RunGetFoldingRangeAsync(testLspServer);
        AssertJsonEquals(expected, results);
    }

    private static async Task<LSP.FoldingRange[]> RunGetFoldingRangeAsync(TestLspServer testLspServer)
    {
        var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
        var request = new LSP.FoldingRangeParams()
        {
            TextDocument = CreateTextDocumentIdentifier(document.GetURI())
        };

        return await testLspServer.ExecuteRequestAsync<LSP.FoldingRangeParams, LSP.FoldingRange[]>(LSP.Methods.TextDocumentFoldingRangeName,
            request, CancellationToken.None);
    }

    private static LSP.FoldingRange CreateFoldingRange(string kind, LSP.Range range, string collapsedText, bool lineFoldingOnly)
        => new()
        {
            Kind = kind switch
            {
                "foldingRange" => null,
                null => null,
                _ => new(kind)
            },
            StartCharacter = lineFoldingOnly ? null : range.Start.Character,
            EndCharacter = lineFoldingOnly ? null : range.End.Character,
            StartLine = range.Start.Line,
            EndLine = range.End.Line,
            CollapsedText = collapsedText
        };
}
