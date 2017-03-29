// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions
{
    public static partial class CommonExtensions
    {
        public static void VerifyCurrentTokenType(this AbstractIntegrationTest test, string tokenType, TextViewWindow_OutOfProc window)
        {
            test.WaitForAsyncOperations(
                FeatureAttribute.SolutionCrawler,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.Classification);
            var actualTokenTypes = window.GetCurrentClassifications();
            Assert.Equal(actualTokenTypes.Length, 1);
            Assert.Contains(tokenType, actualTokenTypes[0]);
            Assert.NotEqual("text", tokenType);
        }

        public static void VerifyCompletionItemExists(this AbstractIntegrationTest test, params string[] expectedItems)
        {
            var completionItems = test.TextViewWindow.GetCompletionItems();
            foreach (var expectedItem in expectedItems)
            {
                Assert.Contains(expectedItem, completionItems);
            }
        }

        public static void VerifyCompletionUnexpectedItemDoesNotExist(this AbstractIntegrationTest test, params string[] unexpectedItems)
        {
            var completionItems = test.TextViewWindow.GetCompletionItems();
            foreach (var unexpectedItem in unexpectedItems)
            {
                Assert.DoesNotContain(unexpectedItem, completionItems);
            }
        }

        public static void VerifyCaretPosition(this AbstractIntegrationTest test, int expectedCaretPosition)
        {
            var position = test.TextViewWindow.GetCaretPosition();
            Assert.Equal(expectedCaretPosition, position);
        }

        public  static void VerifyCodeActions(
            this AbstractIntegrationTest test,
            IEnumerable<string> expectedItems,
            string applyFix = null,
            bool verifyNotShowing = false,
            bool ensureExpectedItemsAreOrdered = false,
            FixAllScope? fixAllScope = null,
            bool blockUntilComplete = true)
        {
            test.TextViewWindow.ShowLightBulb();
            test.TextViewWindow.WaitForLightBulbSession();

            if (verifyNotShowing)
            {
                test.VerifyCodeActionsNotShowing();
                return;
            }

            var actions = test.TextViewWindow.GetLightBulbActions();

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
                test.TextViewWindow.ApplyLightBulbAction(applyFix, fixAllScope, blockUntilComplete);

                if (blockUntilComplete)
                {
                    // wait for action to complete
                    test.WaitForAsyncOperations(FeatureAttribute.LightBulb);
                }
            }
        }

        public static void VerifyCodeAction(
            this AbstractIntegrationTest test,
            string expectedItem,
            bool applyFix = false,
            bool verifyNotShowing = false,
            bool ensureExpectedItemsAreOrdered = false,
            FixAllScope? fixAllScope = null,
            bool blockUntilComplete = true)
        {
            var expectedItems = new[] { expectedItem };
            VerifyCodeActions(test, expectedItems, applyFix ? expectedItem : null, verifyNotShowing,
                ensureExpectedItemsAreOrdered, fixAllScope, blockUntilComplete);
        }

        public static void VerifyCodeActionsNotShowing(this AbstractIntegrationTest test)
        {
            if (test.TextViewWindow.IsLightBulbSessionExpanded())
            {
                throw new InvalidOperationException("Expected no light bulb session, but one was found.");
            }
        }
    }
}