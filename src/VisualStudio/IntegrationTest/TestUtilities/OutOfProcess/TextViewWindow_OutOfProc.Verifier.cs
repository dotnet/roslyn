// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Xunit;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public abstract partial class TextViewWindow_OutOfProc : OutOfProcComponent
    {
        public class Verifier<TTextViewWindow> where TTextViewWindow : TextViewWindow_OutOfProc
        {
            protected readonly TTextViewWindow _textViewWindow;
            protected readonly VisualStudioInstance _instance;

            public Verifier(TTextViewWindow textViewWindow, VisualStudioInstance instance)
            {
                _textViewWindow = textViewWindow;
                _instance = instance;
            }

            public void CodeAction(
                string expectedItem,
                bool applyFix = false,
                bool verifyNotShowing = false,
                bool ensureExpectedItemsAreOrdered = false,
                FixAllScope? fixAllScope = null,
                bool blockUntilComplete = true)
            {
                var expectedItems = new[] { expectedItem };
                CodeActions(expectedItems, applyFix ? expectedItem : null, verifyNotShowing,
                    ensureExpectedItemsAreOrdered, fixAllScope, blockUntilComplete);
            }

            public void CodeActions(
                IEnumerable<string> expectedItems,
                string applyFix = null,
                bool verifyNotShowing = false,
                bool ensureExpectedItemsAreOrdered = false,
                FixAllScope? fixAllScope = null,
                bool blockUntilComplete = true)
            {
                _textViewWindow.ShowLightBulb();
                _textViewWindow.WaitForLightBulbSession();

                if (verifyNotShowing)
                {
                    CodeActionsNotShowing();
                    return;
                }

                var actions = _textViewWindow.GetLightBulbActions();

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
                    _textViewWindow.ApplyLightBulbAction(applyFix, fixAllScope, blockUntilComplete);

                    if (blockUntilComplete)
                    {
                        // wait for action to complete
                        _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.LightBulb);
                    }
                }
            }

            public void CodeActionsNotShowing()
            {
                if (_textViewWindow.IsLightBulbSessionExpanded())
                {
                    throw new InvalidOperationException("Expected no light bulb session, but one was found.");
                }
            }

            public void CurrentTokenType(string tokenType)
            {
                _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
                _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
                _instance.Workspace.WaitForAsyncOperations(FeatureAttribute.Classification);
                var actualTokenTypes = _textViewWindow.GetCurrentClassifications();
                Assert.Equal(actualTokenTypes.Length, 1);
                Assert.Contains(tokenType, actualTokenTypes[0]);
                Assert.NotEqual("text", tokenType);
            }

            public void CompletionItemsExist(params string[] expectedItems)
            {
                var completionItems = _textViewWindow.GetCompletionItems();
                foreach (var expectedItem in expectedItems)
                {
                    Assert.Contains(expectedItem, completionItems);
                }
            }

            public void CompletionItemsDoNotExist(params string[] unexpectedItems)
            {
                var completionItems = _textViewWindow.GetCompletionItems();
                foreach (var unexpectedItem in unexpectedItems)
                {
                    Assert.DoesNotContain(unexpectedItem, completionItems);
                }
            }

            public void CaretPosition(int expectedCaretPosition)
            {
                var position = _textViewWindow.GetCaretPosition();
                Assert.Equal(expectedCaretPosition, position);
            }
        }
    }
}
