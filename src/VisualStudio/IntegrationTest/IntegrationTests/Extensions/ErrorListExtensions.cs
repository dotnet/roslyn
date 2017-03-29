// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.ErrorList
{
    public static partial class ErrorListExtensions
    {
        public static void ShowErrorList(this AbstractIntegrationTest test)
        {
            test.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            test.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
            test.WaitForAsyncOperations(FeatureAttribute.ErrorSquiggles);
            test.WaitForAsyncOperations(FeatureAttribute.ErrorList);
            test.VisualStudio.Instance.ErrorList.ShowErrorList();
        }

        public static void WaitForNoErrorsInErrorList(this AbstractIntegrationTest test)
        {
            test.VisualStudio.Instance.ErrorList.WaitForNoErrorsInErrorList();
        }

        public static int GetErrorListErrorCount(this AbstractIntegrationTest test)
            => test.VisualStudio.Instance.ErrorList.ErrorListErrorCount;

        public static ErrorListItem[] GetErrorListContents(this AbstractIntegrationTest test)
        {
            test.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            test.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
            test.WaitForAsyncOperations(FeatureAttribute.ErrorSquiggles);
            test.WaitForAsyncOperations(FeatureAttribute.ErrorList);
            return test.VisualStudio.Instance.ErrorList.GetErrorListContents();
        }

        public static void NavigateToErrorListItem(this AbstractIntegrationTest test, int itemIndex)
        {
            test.WaitForAsyncOperations(FeatureAttribute.SolutionCrawler);
            test.WaitForAsyncOperations(FeatureAttribute.DiagnosticService);
            test.WaitForAsyncOperations(FeatureAttribute.ErrorSquiggles);
            test.WaitForAsyncOperations(FeatureAttribute.ErrorList);
            test.VisualStudio.Instance.ErrorList.NavigateToErrorListItem(itemIndex);
        }
    }
}
