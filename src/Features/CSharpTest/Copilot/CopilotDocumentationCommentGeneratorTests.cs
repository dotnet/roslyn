// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Copilot.UnitTests;

[Trait(Traits.Feature, Traits.Features.DocumentationComments)]
public sealed class CopilotDocumentationCommentGeneratorTests
{
    private const string Shell = """
        <summary>

        </summary>
        <param name="a"></param>
        <param name="b"></param>
        <returns></returns>
        """;

    private static SyntaxNode ParseMethod(string method)
    {
        var tree = CSharpSyntaxTree.ParseText($"class C {{ {method} }}");
        return tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
    }

    private static DocumentationCommentProposal GetProposal()
    {
        var memberNode = ParseMethod("int Add(int a, int b) => a + b;");
        var caret = Shell.IndexOf("<summary>", StringComparison.Ordinal) + "<summary>".Length;
        var proposal = CopilotDocumentationCommentGenerator.GetSnippetProposal(Shell, memberNode, position: 0, caret);
        Assert.NotNull(proposal);
        return proposal;
    }

    [Fact]
    public void GetSnippetProposal_ExtractsSummaryRemarksParamsAndReturns()
    {
        var proposal = GetProposal();

        var tags = proposal.ProposedEdits.Select(e => (e.TagType, e.SymbolName)).ToList();
        Assert.Contains((DocumentationCommentTagType.Summary, (string?)null), tags);
        Assert.Contains((DocumentationCommentTagType.Remarks, (string?)null), tags);
        Assert.Contains((DocumentationCommentTagType.Param, "a"), tags);
        Assert.Contains((DocumentationCommentTagType.Param, "b"), tags);
        Assert.Contains((DocumentationCommentTagType.Returns, (string?)null), tags);
    }

    [Fact]
    public void GetSnippetProposal_NullPosition_ReturnsNull()
    {
        var memberNode = ParseMethod("int Add(int a, int b) => a + b;");
        Assert.Null(CopilotDocumentationCommentGenerator.GetSnippetProposal(Shell, memberNode, position: null, caret: 0));
    }

    [Fact]
    public async Task GenerateEditsAsync_MapsCopilotResponseOntoProposalSpans()
    {
        var proposal = GetProposal();
        var copilot = new TestCopilotCodeAnalysisService((new Dictionary<string, string>
        {
            ["Summary"] = "Adds two numbers.",
            ["Param-a"] = "the first addend",
            ["Param-b"] = "the second addend",
            ["Returns"] = "their sum",
        }, IsQuotaExceeded: false));

        var edits = await CopilotDocumentationCommentGenerator.GenerateEditsAsync(proposal, copilot, indentText: "", CancellationToken.None);

        // Summary + two params + returns map to edits; the unconditional Remarks edit is dropped because the
        // model returned no remarks.
        Assert.Equal(4, edits.Length);
        Assert.Contains(edits, e => e.ReplacementText.Contains("Adds two numbers."));
        Assert.Contains(edits, e => e.ReplacementText.Contains("the first addend"));
        Assert.Contains(edits, e => e.ReplacementText.Contains("the second addend"));
        Assert.Contains(edits, e => e.ReplacementText.Contains("their sum"));

        // Every produced edit targets one of the zero-length insertion spans described by the proposal.
        foreach (var edit in edits)
        {
            Assert.Equal(0, edit.SpanToReplace.Length);
            Assert.Contains(proposal.ProposedEdits, p => p.SpanToReplace == edit.SpanToReplace);
        }
    }

    [Fact]
    public async Task GenerateEditsAsync_QuotaExceeded_ReturnsEmpty()
    {
        var proposal = GetProposal();
        var copilot = new TestCopilotCodeAnalysisService((new Dictionary<string, string> { ["Summary"] = "Adds two numbers." }, IsQuotaExceeded: true));

        var edits = await CopilotDocumentationCommentGenerator.GenerateEditsAsync(proposal, copilot, indentText: "", CancellationToken.None);

        Assert.Empty(edits);
    }

    [Fact]
    public async Task GenerateEditsAsync_NullResponse_ReturnsEmpty()
    {
        var proposal = GetProposal();
        var copilot = new TestCopilotCodeAnalysisService((null, IsQuotaExceeded: false));

        var edits = await CopilotDocumentationCommentGenerator.GenerateEditsAsync(proposal, copilot, indentText: "", CancellationToken.None);

        Assert.Empty(edits);
    }

    private sealed class TestCopilotCodeAnalysisService(
        (Dictionary<string, string>? ResponseDictionary, bool IsQuotaExceeded) result) : ICopilotCodeAnalysisService
    {
        public Task<(Dictionary<string, string>? responseDictionary, bool isQuotaExceeded)> GetDocumentationCommentAsync(DocumentationCommentProposal proposal, CancellationToken cancellationToken)
            => Task.FromResult((result.ResponseDictionary, result.IsQuotaExceeded));

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<string>> GetAvailablePromptTitlesAsync(Document document, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task AnalyzeDocumentAsync(Document document, TextSpan? span, string promptTitle, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableArray<Diagnostic>> GetCachedDocumentDiagnosticsAsync(Document document, TextSpan? span, ImmutableArray<string> promptTitles, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task StartRefinementSessionAsync(Document oldDocument, Document newDocument, Diagnostic? primaryDiagnostic, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<string> GetOnTheFlyDocsPromptAsync(OnTheFlyDocsInfo onTheFlyDocsInfo, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<(string responseString, bool isQuotaExceeded)> GetOnTheFlyDocsResponseAsync(string prompt, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> IsFileExcludedAsync(string filePath, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<bool> IsImplementNotImplementedExceptionsAvailableAsync(CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<ImmutableDictionary<SyntaxNode, ImplementationDetails>> ImplementNotImplementedExceptionsAsync(
            Document document,
            ImmutableDictionary<SyntaxNode, ImmutableArray<ReferencedSymbol>> methodOrProperties,
            CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
