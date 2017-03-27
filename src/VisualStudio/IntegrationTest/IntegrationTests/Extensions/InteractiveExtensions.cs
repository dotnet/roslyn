// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;

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
    }
}
