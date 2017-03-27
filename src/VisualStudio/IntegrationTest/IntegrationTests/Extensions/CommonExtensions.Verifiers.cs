// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions
{
    public static partial class CommonExtensions
    {
        public static void VerifyCurrentTokenType(AbstractIntegrationTest test, string tokenType, TextViewWindow_OutOfProc window)
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

        public static void VerifyCompletionItemExists(TextViewWindow_OutOfProc window, string[] expectedItems)
        {
            var completionItems = window.GetCompletionItems();
            foreach (var expectedItem in expectedItems)
            {
                Assert.Contains(expectedItem, completionItems);
            }
        }

        public static void VerifyCaretPosition(TextViewWindow_OutOfProc window, int expectedCaretPosition)
        {
            var position = window.GetCaretPosition();
            Assert.Equal(expectedCaretPosition, position);
        }
    }
}
