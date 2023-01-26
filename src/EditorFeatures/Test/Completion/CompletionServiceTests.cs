// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Completion
{
    [UseExportProvider]
    public class CompletionServiceTests
    {
        [Fact]
        public async Task TestNuGetCompletionProvider()
        {
            var code = @"
using System.Diagnostics;
class Test {
    void Method() {
        Debug.Assert(true, ""$$"");
    }
}
";

            using var workspace = TestWorkspace.CreateCSharp(code, openDocuments: true);

            var nugetCompletionProvider = new DebugAssertTestCompletionProvider();
            var reference = new MockAnalyzerReference(nugetCompletionProvider);
            var project = workspace.CurrentSolution.Projects.Single().AddAnalyzerReference(reference);
            var completionService = project.Services.GetRequiredService<CompletionService>();

            var document = project.Documents.Single();
            var caretPosition = workspace.DocumentWithCursor.CursorPosition ?? throw new InvalidOperationException();
            var completions = await completionService.GetCompletionsAsync(document, caretPosition, CompletionOptions.Default, OptionSet.Empty);

            // NuGet providers are not included until it's loaded and cached, this is to avoid potential delays, especially on UI thread.
            Assert.Empty(completions.ItemsList);

            // NuGet analyzers for the project will be loaded when this returns 
            var waiter = workspace.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.CompletionSet);
            await waiter.ExpeditedWaitAsync();

            completions = await completionService.GetCompletionsAsync(document, caretPosition, CompletionOptions.Default, OptionSet.Empty);

            Assert.NotEmpty(completions.ItemsList);

            var item = Assert.Single(completions.ItemsList.Where(item => item.ProviderName == typeof(DebugAssertTestCompletionProvider).FullName));
            Assert.Equal(nameof(DebugAssertTestCompletionProvider), item.DisplayText);

            var expectedDescriptionText = nameof(DebugAssertTestCompletionProvider);
            var actualDescriptionText = (await completionService.GetDescriptionAsync(document, item, CompletionOptions.Default, SymbolDescriptionOptions.Default).ConfigureAwait(false))!.Text;
            Assert.Equal(expectedDescriptionText, actualDescriptionText);

            var expectedChange = new TextChange(item.Span, nameof(DebugAssertTestCompletionProvider));
            var actualChange = (await completionService.GetChangeAsync(document, item).ConfigureAwait(false)).TextChange;
            Assert.Equal(expectedChange, actualChange);
        }

        private class MockAnalyzerReference : AnalyzerReference, ICompletionProviderFactory
        {
            private readonly CompletionProvider _completionProvider;

            public MockAnalyzerReference(CompletionProvider completionProvider)
            {
                _completionProvider = completionProvider;
            }

            public override string FullPath => "";
            public override object Id => nameof(MockAnalyzerReference);

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
                => ImmutableArray<DiagnosticAnalyzer>.Empty;

            public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
                => ImmutableArray<DiagnosticAnalyzer>.Empty;

            public ImmutableArray<CompletionProvider> GetCompletionProviders()
                => ImmutableArray.Create(_completionProvider);
        }

        private sealed class DebugAssertTestCompletionProvider : CompletionProvider
        {
            public DebugAssertTestCompletionProvider()
            {
            }

            public override bool ShouldTriggerCompletion(SourceText text, int caretPosition, CompletionTrigger trigger, OptionSet options)
            {
                return trigger.Kind switch
                {
                    CompletionTriggerKind.Invoke => true,
                    CompletionTriggerKind.InvokeAndCommitIfUnique => true,
                    CompletionTriggerKind.Insertion => trigger.Character == '"',
                    _ => false,
                };
            }

            public override async Task ProvideCompletionsAsync(CompletionContext context)
            {
                var completionItem = CompletionItem.Create(displayText: nameof(DebugAssertTestCompletionProvider), displayTextSuffix: "", rules: CompletionItemRules.Default);
                context.AddItem(completionItem);
                context.CompletionListSpan = await GetTextChangeSpanAsync(context.Document, context.CompletionListSpan, context.CancellationToken).ConfigureAwait(false);
            }

            public override Task<CompletionDescription> GetDescriptionAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            {
                return Task.FromResult(CompletionDescription.FromText(nameof(DebugAssertTestCompletionProvider)));
            }

            public override Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey, CancellationToken cancellationToken)
            {
                return Task.FromResult(CompletionChange.Create(new TextChange(item.Span, nameof(DebugAssertTestCompletionProvider))));
            }

            private static async Task<TextSpan> GetTextChangeSpanAsync(Document document, TextSpan startSpan, CancellationToken cancellationToken)
            {
                var result = startSpan;
                var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var token = root.FindToken(result.Start);
                if (syntaxFacts.IsStringLiteral(token) || syntaxFacts.IsVerbatimStringLiteral(token))
                {
                    var text = root.GetText();

                    // Expand selection in both directions until a double quote or any line break character is reached
                    static bool IsWordCharacter(char ch) => !(ch == '"' || TextUtilities.IsAnyLineBreakCharacter(ch));

                    result = CommonCompletionUtilities.GetWordSpan(
                        text, startSpan.Start, IsWordCharacter, IsWordCharacter, alwaysExtendEndSpan: true);
                }

                return result;
            }
        }
    }
}
