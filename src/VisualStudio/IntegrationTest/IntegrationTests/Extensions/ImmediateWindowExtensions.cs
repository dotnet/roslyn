// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.ImmediateWindow
{
    public static class ImmediateWindowExtensions
    {
        public static void InvokeCompletionList(this AbstractIntegrationTest test)
            => CommonExtensions.InvokeCompletionList(test);

        public static void VerifyCompletionItemExistsInImmediateWindow(this AbstractIntegrationTest test, params string[] expectedItems)
            => CommonExtensions.VerifyCompletionItemExists(test.VisualStudio.Instance.ImmediateWindow, expectedItems);

        public static void VerifyCompletionItemDoesNotExistInImmediateWindow(this AbstractIntegrationTest test, params string[] expectedItems)
            => CommonExtensions.VerifyCompletionItemDoesNotExist(test.VisualStudio.Instance.ImmediateWindow, expectedItems);

        public static void ShowImmediateWindow(this AbstractIntegrationTest test)
            => test.VisualStudio.Instance.ImmediateWindow.ShowWindow();

        public static void SendKeysToImmediateWindow(this AbstractIntegrationTest test, params object[] keys)
            => test.VisualStudio.Instance.ImmediateWindow.SendKeys(keys);

        public static void ClearImmediateWindow(this AbstractIntegrationTest test)
            => test.VisualStudio.Instance.ImmediateWindow.Clear();
    }
}
