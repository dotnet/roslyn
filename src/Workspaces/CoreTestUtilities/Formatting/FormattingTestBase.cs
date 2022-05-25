// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.Formatting
{
    [UseExportProvider]
    public abstract class FormattingTestBase
    {
        private protected Task AssertFormatAsync(
            string expected,
            string code,
            string language,
            bool debugMode = false,
            OptionsCollection? changedOptionSet = null,
            bool testWithTransformation = true)
        {
            return AssertFormatAsync(expected, code, new[] { new TextSpan(0, code.Length) }, language, debugMode, changedOptionSet, testWithTransformation);
        }

        private protected async Task AssertFormatAsync(
            string expected,
            string code,
            IEnumerable<TextSpan> spans,
            string language,
#pragma warning disable IDE0060 // Remove unused parameter - https://github.com/dotnet/roslyn/issues/44225
            bool debugMode = false,
#pragma warning restore IDE0060 // Remove unused parameter
            OptionsCollection? changedOptionSet = null,
            bool treeCompare = true,
            ParseOptions? parseOptions = null)
        {
            using (var workspace = new AdhocWorkspace())
            {
                var project = workspace.CurrentSolution.AddProject("Project", "Project.dll", language);
                if (parseOptions != null)
                {
                    project = project.WithParseOptions(parseOptions);
                }

                var document = project.AddDocument("Document", SourceText.From(code));

                var formattingService = document.GetRequiredLanguageService<ISyntaxFormattingService>();
                var formattingOptions = changedOptionSet != null ?
                    formattingService.GetFormattingOptions(changedOptionSet.ToAnalyzerConfigOptions(document.Project.LanguageServices), fallbackOptions: null) :
                    formattingService.DefaultOptions;

                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(CancellationToken.None);
                var root = await syntaxTree.GetRootAsync();
                await AssertFormatAsync(workspace.Services, expected, root, spans.AsImmutable(), formattingOptions, await document.GetTextAsync());

                // format with node and transform
                AssertFormatWithTransformation(workspace.Services, expected, root, spans, formattingOptions, treeCompare, parseOptions);
            }
        }

        protected abstract SyntaxNode ParseCompilation(string text, ParseOptions? parseOptions);

        internal void AssertFormatWithTransformation(
            HostWorkspaceServices services, string expected, SyntaxNode root, IEnumerable<TextSpan> spans, SyntaxFormattingOptions options, bool treeCompare = true, ParseOptions? parseOptions = null)
        {
            var newRootNode = Formatter.Format(root, spans, services, options, rules: null, CancellationToken.None);

            Assert.Equal(expected, newRootNode.ToFullString());

            // test doesn't use parsing option. add one if needed later
            var newRootNodeFromString = ParseCompilation(expected, parseOptions);

            if (treeCompare)
            {
                // simple check to see whether two nodes are equivalent each other.
                Assert.True(newRootNodeFromString.IsEquivalentTo(newRootNode));
            }
        }

        private static async Task AssertFormatAsync(HostWorkspaceServices services, string expected, SyntaxNode root, ImmutableArray<TextSpan> spans, SyntaxFormattingOptions options, SourceText sourceText)
        {
            // Verify formatting the input code produces the expected result
            var result = Formatter.GetFormattedTextChanges(root, spans, services, options);
            AssertResult(expected, sourceText, result);

            // Verify formatting the output code produces itself (formatting is idempotent)
            var resultText = sourceText.WithChanges(result);
            if (TryAdjustSpans(sourceText, result, resultText, spans, out var adjustedSpans))
            {
                var resultRoot = await root.SyntaxTree.WithChangedText(resultText).GetRootAsync();
                var idempotentResult = Formatter.GetFormattedTextChanges(resultRoot, adjustedSpans, services, options);
                AssertResult(expected, resultText, idempotentResult);
            }
        }

        private static bool TryAdjustSpans(SourceText inputText, IList<TextChange> changes, SourceText outputText, ImmutableArray<TextSpan> inputSpans, out ImmutableArray<TextSpan> outputSpans)
        {
            if (changes.Count == 0)
            {
                outputSpans = inputSpans;
                return true;
            }

            var outputBuilder = ImmutableArray.CreateBuilder<TextSpan>(inputSpans.Length);
            for (var i = 0; i < inputSpans.Length; i++)
            {
                var span = inputSpans[i];
                if (span.Start == 0 && span.End == inputText.Length)
                {
                    // The input span is the full document
                    outputBuilder.Add(TextSpan.FromBounds(0, outputText.Length));
                    continue;
                }

                // The input span cannot be automatically adjusted
                outputSpans = default;
                return false;
            }

            outputSpans = outputBuilder.MoveToImmutable();
            return true;
        }

        protected static void AssertResult(string expected, SourceText sourceText, IList<TextChange> result)
        {
            var actual = sourceText.WithChanges(result).ToString();
            AssertEx.EqualOrDiff(expected, actual);
        }
    }
}
