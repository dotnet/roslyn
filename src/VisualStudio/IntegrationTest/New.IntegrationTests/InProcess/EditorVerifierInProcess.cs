// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Extensibility.Testing;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.VisualStudio.NewIntegrationTests.InProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    [TestService]
    internal partial class EditorVerifierInProcess : ITextViewWindowVerifierInProcess
    {
        TestServices ITextViewWindowVerifierInProcess.TestServices => TestServices;

        ITextViewWindowInProcess ITextViewWindowVerifierInProcess.TextViewWindow => TestServices.Editor;

        public async Task CurrentLineTextAsync(
            string expectedText,
            bool assertCaretPosition = false,
            CancellationToken cancellationToken = default)
        {
            if (assertCaretPosition)
            {
                await CurrentLineTextAndAssertCaretPositionAsync(expectedText, cancellationToken);
            }
            else
            {
                var lineText = await TestServices.Editor.GetCurrentLineTextAsync(cancellationToken);
                Assert.Equal(expectedText, lineText);
            }
        }

        private async Task CurrentLineTextAndAssertCaretPositionAsync(
            string expectedText,
            CancellationToken cancellationToken)
        {
            var expectedCaretIndex = expectedText.IndexOf("$$");
            if (expectedCaretIndex < 0)
            {
                throw new ArgumentException("Expected caret position to be specified with $$", nameof(expectedText));
            }

            var expectedCaretMarkupEndIndex = expectedCaretIndex + "$$".Length;

            var expectedTextBeforeCaret = expectedText[..expectedCaretIndex];
            var expectedTextAfterCaret = expectedText[expectedCaretMarkupEndIndex..];

            var lineText = await TestServices.Editor.GetCurrentLineTextAsync(cancellationToken);
            var lineTextBeforeCaret = await TestServices.Editor.GetLineTextBeforeCaretAsync(cancellationToken);
            var lineTextAfterCaret = await TestServices.Editor.GetLineTextAfterCaretAsync(cancellationToken);

            Assert.Equal(expectedTextBeforeCaret, lineTextBeforeCaret);
            Assert.Equal(expectedTextAfterCaret, lineTextAfterCaret);
            Assert.Equal(expectedTextBeforeCaret.Length + expectedTextAfterCaret.Length, lineText.Length);
        }

        public async Task TextEqualsAsync(
            string expectedText,
            CancellationToken cancellationToken)
        {
            var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
            var editorText = view.TextSnapshot.GetText();
            var caretPosition = (await TestServices.Editor.GetCaretPositionAsync(cancellationToken)).BufferPosition.Position;
            editorText = editorText.Insert(caretPosition, "$$");
            AssertEx.EqualOrDiff(expectedText, editorText);
        }

        public async Task TextContainsAsync(
            string expectedText,
            bool assertCaretPosition = false,
            CancellationToken cancellationToken = default)
        {
            if (assertCaretPosition)
            {
                await TextContainsAndAssertCaretPositionAsync(expectedText, cancellationToken);
            }
            else
            {
                var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
                var editorText = view.TextSnapshot.GetText();
                Assert.Contains(expectedText, editorText);
            }
        }

        private async Task TextContainsAndAssertCaretPositionAsync(
            string expectedText,
            CancellationToken cancellationToken)
        {
            var caretStartIndex = expectedText.IndexOf("$$");
            if (caretStartIndex < 0)
            {
                throw new ArgumentException("Expected caret position to be specified with $$", nameof(expectedText));
            }

            var caretEndIndex = caretStartIndex + "$$".Length;

            var expectedTextBeforeCaret = expectedText[..caretStartIndex];
            var expectedTextAfterCaret = expectedText[caretEndIndex..];

            var expectedTextWithoutCaret = expectedTextBeforeCaret + expectedTextAfterCaret;

            var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
            var editorText = view.TextSnapshot.GetText();
            Assert.Contains(expectedTextWithoutCaret, editorText);

            var index = editorText.IndexOf(expectedTextWithoutCaret);

            var caretPosition = (await TestServices.Editor.GetCaretPositionAsync(cancellationToken)).BufferPosition.Position;
            Assert.Equal(caretStartIndex + index, caretPosition);
        }

        public async Task CaretPositionAsync(int expectedCaretPosition, CancellationToken cancellationToken)
        {
            Assert.Equal(expectedCaretPosition, (await TestServices.Editor.GetCaretPositionAsync(cancellationToken)).BufferPosition.Position);
        }

        public async Task ErrorTagsAsync(
            (string errorType, TextSpan textSpan, string taggedText, string tooltipText)[] expectedTags, CancellationToken cancellationToken)
        {
            await TestServices.Workspace.WaitForAllAsyncOperationsAsync(
                [FeatureAttribute.Workspace, FeatureAttribute.SolutionCrawlerLegacy, FeatureAttribute.DiagnosticService, FeatureAttribute.ErrorSquiggles],
                cancellationToken);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1));

                if (!await TagEqualsAsync())
                    continue;

                return;
            }

            async Task<bool> TagEqualsAsync()
            {
                var actualTags = await TestServices.Editor.GetTagsAsync<IErrorTag>(cancellationToken);

                if (expectedTags.Length != actualTags.Length)
                    return false;

                for (var i = 0; i < expectedTags.Length; i++)
                {
                    var expectedTag = expectedTags[i];
                    var actualTaggedSpan = actualTags[i];

                    if (expectedTag.errorType != actualTaggedSpan.Tag.ErrorType)
                        return false;

                    if (expectedTag.textSpan.Start != actualTaggedSpan.Span.Start.Position)
                        return false;

                    if (expectedTag.textSpan.Length != actualTaggedSpan.Span.Length)
                        return false;

                    var actualTaggedText = actualTaggedSpan.Span.GetText();
                    if (expectedTag.taggedText != actualTaggedText)
                        return false;

                    AssertEx.NotNull(actualTaggedSpan.Tag.ToolTipContent);
                    var containerElement = (ContainerElement)actualTaggedSpan.Tag.ToolTipContent;
                    var actualTooltipText = CollectTextInRun(containerElement);
                    if (expectedTag.tooltipText != actualTooltipText)
                        return false;
                }

                return true;
            }

            static string CollectTextInRun(ContainerElement? containerElement)
            {
                var builder = new StringBuilder();

                if (containerElement is not null)
                {
                    foreach (var element in containerElement.Elements)
                    {
                        if (element is ClassifiedTextElement classifiedTextElement)
                        {
                            foreach (var run in classifiedTextElement.Runs)
                            {
                                builder.Append(run.Text);
                            }
                        }
                    }
                }

                return builder.ToString();
            }
        }
    }
}
