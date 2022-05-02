// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal class EditorVerifierInProcess : InProcComponent
    {
        public EditorVerifierInProcess(TestServices testServices)
            : base(testServices)
        {
        }

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
                var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
                var lineText = view.Caret.Position.BufferPosition.GetContainingLine().GetText();
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

            var expectedTextBeforeCaret = expectedText.Substring(0, expectedCaretIndex);
            var expectedTextAfterCaret = expectedText.Substring(expectedCaretMarkupEndIndex);

            var view = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
            var bufferPosition = view.Caret.Position.BufferPosition;
            var line = bufferPosition.GetContainingLine();
            var lineText = line.GetText();
            var lineTextBeforeCaret = lineText[..(bufferPosition.Position - line.Start)];
            var lineTextAfterCaret = lineText[(bufferPosition.Position - line.Start)..];

            Assert.Equal(expectedTextBeforeCaret, lineTextBeforeCaret);
            Assert.Equal(expectedTextAfterCaret, lineTextAfterCaret);
            Assert.Equal(expectedTextBeforeCaret.Length + expectedTextAfterCaret.Length, lineText.Length);
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

            var caretPosition = await TestServices.Editor.GetCaretPositionAsync(cancellationToken);
            Assert.Equal(caretStartIndex + index, caretPosition);
        }

        public async Task CodeActionAsync(
            string expectedItem,
            bool applyFix = false,
            bool verifyNotShowing = false,
            bool ensureExpectedItemsAreOrdered = false,
            FixAllScope? fixAllScope = null,
            bool blockUntilComplete = true,
            CancellationToken cancellationToken = default)
        {
            var expectedItems = new[] { expectedItem };

            bool? applied;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                applied = await CodeActionsAsync(expectedItems, applyFix ? expectedItem : null, verifyNotShowing,
                    ensureExpectedItemsAreOrdered, fixAllScope, blockUntilComplete, cancellationToken);
            } while (applied is false);
        }

        /// <returns>
        /// <list type="bullet">
        /// <item><description><see langword="true"/> if <paramref name="applyFix"/> is specified and the fix is successfully applied</description></item>
        /// <item><description><see langword="false"/> if <paramref name="applyFix"/> is specified but the fix is not successfully applied</description></item>
        /// <item><description><see langword="null"/> if <paramref name="applyFix"/> is false, so there is no fix to apply</description></item>
        /// </list>
        /// </returns>
        public async Task<bool?> CodeActionsAsync(
            IEnumerable<string> expectedItems,
            string? applyFix = null,
            bool verifyNotShowing = false,
            bool ensureExpectedItemsAreOrdered = false,
            FixAllScope? fixAllScope = null,
            bool blockUntilComplete = true,
            CancellationToken cancellationToken = default)
        {
            await TestServices.Editor.ShowLightBulbAsync(cancellationToken);

            if (verifyNotShowing)
            {
                await CodeActionsNotShowingAsync(cancellationToken);
                return null;
            }

            var actions = await TestServices.Editor.GetLightBulbActionsAsync(cancellationToken);

            if (expectedItems != null && expectedItems.Any())
            {
                if (ensureExpectedItemsAreOrdered)
                {
                    TestUtilities.ThrowIfExpectedItemNotFoundInOrder(
                        actions,
                        expectedItems);
                }
                else
                {
                    TestUtilities.ThrowIfExpectedItemNotFound(
                        actions,
                        expectedItems);
                }
            }

            if (fixAllScope.HasValue)
            {
                Assumes.Present(applyFix);
            }

            if (!RoslynString.IsNullOrEmpty(applyFix))
            {
                var result = await TestServices.Editor.ApplyLightBulbActionAsync(applyFix, fixAllScope, blockUntilComplete, cancellationToken);

                if (blockUntilComplete)
                {
                    // wait for action to complete
                    await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb, cancellationToken);
                }

                return result;
            }

            return null;
        }

        public async Task CodeActionsNotShowingAsync(CancellationToken cancellationToken)
        {
            if (await TestServices.Editor.IsLightBulbSessionExpandedAsync(cancellationToken))
            {
                throw new InvalidOperationException("Expected no light bulb session, but one was found.");
            }
        }

        public async Task CaretPositionAsync(int expectedCaretPosition, CancellationToken cancellationToken)
        {
            Assert.Equal(expectedCaretPosition, await TestServices.Editor.GetCaretPositionAsync(cancellationToken));
        }
    }
}
