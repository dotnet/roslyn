// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    partial class TextViewWindow_InProc2
    {
        public class Verifier<TTextViewWindow>
            where TTextViewWindow : TextViewWindow_InProc2
        {
            protected readonly TTextViewWindow _textViewWindow;

            public Verifier(TTextViewWindow textViewWindow)
            {
                _textViewWindow = textViewWindow;
            }

            public async Task CodeActionAsync(
                string expectedItem,
                bool applyFix = false,
                bool verifyNotShowing = false,
                bool ensureExpectedItemsAreOrdered = false,
                FixAllScope? fixAllScope = null,
                bool willBlockUntilComplete = true)
            {
                var expectedItems = new[] { expectedItem };
                await CodeActionsAsync(expectedItems, applyFix ? expectedItem : null, verifyNotShowing,
                    ensureExpectedItemsAreOrdered, fixAllScope, willBlockUntilComplete);
            }

            public async Task CodeActionsAsync(
                IEnumerable<string> expectedItems,
                string applyFix = null,
                bool verifyNotShowing = false,
                bool ensureExpectedItemsAreOrdered = false,
                FixAllScope? fixAllScope = null,
                bool willBlockUntilComplete = true)
            {
                await _textViewWindow.ShowLightBulbAsync();
                await _textViewWindow.WaitForLightBulbSessionAsync();

                if (verifyNotShowing)
                {
                    await CodeActionsNotShowingAsync();
                    return;
                }

                var actions = await _textViewWindow.GetLightBulbActionsAsync();

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

                if (!string.IsNullOrEmpty(applyFix) || fixAllScope.HasValue)
                {
                    await _textViewWindow.ApplyLightBulbActionAsync(applyFix, fixAllScope, willBlockUntilComplete);

                    // wait for action to complete
                    await _textViewWindow.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.LightBulb);
                }
            }

            public async Task CodeActionsNotShowingAsync()
            {
                if (await _textViewWindow.IsLightBulbSessionExpandedAsync())
                {
                    throw new InvalidOperationException("Expected no light bulb session, but one was found.");
                }
            }

            public async Task CurrentTokenTypeAsync(string tokenType)
            {
                await _textViewWindow.TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SolutionCrawler);
                await _textViewWindow.TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.DiagnosticService);
                await _textViewWindow.TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.Classification);
                var actualTokenTypes = await _textViewWindow.GetCurrentClassificationsAsync();
                Assert.Equal(actualTokenTypes.Length, 1);
                Assert.Contains(tokenType, actualTokenTypes[0]);
                Assert.NotEqual("text", tokenType);
            }

            public async Task CurrentCompletionItemAsync(string expectedItem)
            {
                var currentItem = await _textViewWindow.GetCurrentCompletionItemAsync();
                Assert.Equal(expectedItem, currentItem);
            }

            public async Task CompletionItemsExistAsync(params string[] expectedItems)
            {
                var completionItems = await _textViewWindow.GetCompletionItemsAsync();
                foreach (var expectedItem in expectedItems)
                {
                    Assert.Contains(expectedItem, completionItems);
                }
            }

            public async Task CompletionItemsDoNotExistAsync( params string[] unexpectedItems)
            {
                var completionItems = await _textViewWindow.GetCompletionItemsAsync();
                foreach (var unexpectedItem in unexpectedItems)
                {
                    Assert.DoesNotContain(unexpectedItem, completionItems);
                }
            }

            public async Task CaretPositionAsync(int expectedCaretPosition)
            {
                var position = await _textViewWindow.GetCaretPositionAsync();
                Assert.Equal(expectedCaretPosition, position);
            }
        }
    }
}
