// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ErrorList_InProc : InProcComponent
    {
        public static ErrorList_InProc Create()
            => new ErrorList_InProc();

        public void ShowErrorList()
            => ExecuteCommand("View.ErrorList");

        public int ErrorListErrorCount
            => GetErrorCount();

        public void WaitForNoErrorsInErrorList()
        {
            while (GetErrorCount() != 0)
            {
                Thread.Yield();
            }
        }

        public void NavigateToErrorListItem(int itemIndex)
        {
            var errorItems = GetErrorItems().AsEnumerable();
            if (itemIndex > errorItems.Count())
            {
                throw new ArgumentException($"Cannot Navigate to Item '{itemIndex}', Total Items found '{errorItems.Count()}'.");
            }
            errorItems.ElementAt(itemIndex).Navigate();
        }

        public int GetErrorCount()
        {
            var errorItems = GetErrorItems();
            try
            {
                return errorItems
                    .AsEnumerable()
                    .Count();
            }
            catch (IndexOutOfRangeException)
            {
                // It is entirely possible that the items in the error list are modified
                // after we start iterating, in which case we want to try again.
                return GetErrorCount();
            }
        }

        public ErrorListItem[] GetErrorListContents()
        {
            var errorItems = GetErrorItems();
            try
            {
                return errorItems
                    .AsEnumerable()
                    .Select(e => new ErrorListItem(e))
                    .ToArray();
            }
            catch (IndexOutOfRangeException)
            {
                // It is entirely possible that the items in the error list are modified
                // after we start iterating, in which case we want to try again.
                return GetErrorListContents();
            }
        }

        private ErrorItems GetErrorItems()
            => ((DTE2)GetDTE()).ToolWindows.ErrorList.ErrorItems;
    }

    public static class ErrorListExtensions
    {
        public static IEnumerable<EnvDTE80.ErrorItem> AsEnumerable(this EnvDTE80.ErrorItems items)
        {
            for (var i = 1; i <= items.Count; i++)
            {
                yield return items.Item(i);
            }
        }

        public static string AsString(this EnvDTE80.vsBuildErrorLevel errorLevel)
        {
            switch (errorLevel)
            {
                case vsBuildErrorLevel.vsBuildErrorLevelLow:
                    return "Message";
                case vsBuildErrorLevel.vsBuildErrorLevelMedium:
                    return "Warning";
                case vsBuildErrorLevel.vsBuildErrorLevelHigh:
                    return "Error";
                default:
                    return "Unknown";
            }
        }
    }
}
