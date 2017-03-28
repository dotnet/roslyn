// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.Interactive
{
    public static class InteractiveExtensions
    {
        public static void VerifyCurrentTokenType(this AbstractIntegrationTest test, string tokenType)
        {
            CommonExtensions.VerifyCurrentTokenType(test, tokenType, test.VisualStudio.Instance.CSharpInteractiveWindow);
        }

        public static void VerifyCompletionItemExists(this AbstractIntegrationTest test, params string[] expectedItems)
        {
            CommonExtensions.VerifyCompletionItemExists(test.VisualStudio.Instance.CSharpInteractiveWindow, expectedItems);
        }

        public static void VerifyCaretPosition(this AbstractIntegrationTest test, int expectedCaretPosition)
        {
            CommonExtensions.VerifyCaretPosition(test.VisualStudio.Instance.CSharpInteractiveWindow, expectedCaretPosition);
        }

        public static void InvokeCompletionList(this AbstractIntegrationTest test)
        {
            CommonExtensions.InvokeCompletionList(test);
        }

        public static void WaitForReplOutput(this AbstractInteractiveWindowTest test, string outputText)
            => test.VisualStudio.Instance.CSharpInteractiveWindow.WaitForReplOutput(outputText);

        public static void WaitForLastReplOutputContains(this AbstractInteractiveWindowTest test, string outputText)
            => test.VisualStudio.Instance.CSharpInteractiveWindow.WaitForLastReplOutputContains(outputText);

        public static void WaitForLastReplOutput(this AbstractInteractiveWindowTest test, string outputText)
            => test.VisualStudio.Instance.CSharpInteractiveWindow.WaitForLastReplOutput(outputText);

        public static void WaitForLastReplInputContains(this AbstractInteractiveWindowTest test, string outputText)
              => test.VisualStudio.Instance.CSharpInteractiveWindow.WaitForLastReplInputContains(outputText);
    }
}
