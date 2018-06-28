// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess2
{
    public class ErrorList_InProc2 : InProcComponent2
    {
        public ErrorList_InProc2(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task ShowErrorListAsync()
        {
            await ExecuteCommandAsync("View.ErrorList");

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            // Show errors and warnings
            var errorList = await GetGlobalServiceAsync<SVsErrorList, IVsErrorList>();
            ErrorHandler.ThrowOnFailure(errorList.ForceShowErrors());

            await WaitForErrorListAsync();
        }

#if false
        public int ErrorListErrorCount
            => GetErrorCount();

        public void WaitForNoErrorsInErrorList()
        {
            while (GetErrorCount() != 0)
            {
                Thread.Yield();
            }
        }
#endif

        public async Task NavigateToErrorListItemAsync(int itemIndex)
        {
            await WaitForErrorListAsync();
            var errorItems = (await GetErrorItemsAsync()).AsEnumerable().ToArray();
            if (itemIndex >= errorItems.Count())
            {
                throw new ArgumentException($"Cannot Navigate to Item '{itemIndex}', Total Items found '{errorItems.Count()}'.");
            }

            ErrorHandler.ThrowOnFailure(errorItems.ElementAt(itemIndex).NavigateTo());
        }

#if false
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
#endif

        public async Task<ErrorListItem[]> GetErrorListContentsAsync(__VSERRORCATEGORY minimumSeverity = __VSERRORCATEGORY.EC_WARNING)
        {
            await WaitForErrorListAsync();
            var errorItems = await GetErrorItemsAsync();
            try
            {
                return errorItems
                    .AsEnumerable()
                    .Where(e => ((IVsErrorItem)e).GetCategory() <= minimumSeverity)
                    .Select(e => new ErrorListItem(e.GetSeverity(), e.GetDescription(), e.GetProject(), e.GetFileName(), e.GetLine(), e.GetColumn()))
                    .ToArray();
            }
            catch (IndexOutOfRangeException)
            {
                // It is entirely possible that the items in the error list are modified
                // after we start iterating, in which case we want to try again.
                return await GetErrorListContentsAsync(minimumSeverity);
            }
        }

        private async Task<IVsEnumTaskItems> GetErrorItemsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var errorList = await GetGlobalServiceAsync<SVsErrorList, IVsTaskList>();
            ErrorHandler.ThrowOnFailure(errorList.EnumTaskItems(out var items));
            return items;
        }

        private async Task WaitForErrorListAsync()
        {
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.SolutionCrawler);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.DiagnosticService);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ErrorSquiggles);
            await TestServices.Workspace.WaitForAsyncOperationsAsync(FeatureAttribute.ErrorList);
        }
    }

    public static class ErrorListExtensions
    {
        public static IEnumerable<IVsTaskItem> AsEnumerable(this IVsEnumTaskItems items)
        {
            var item = new IVsTaskItem[1];
            while (true)
            {
                var hr = items.Next(1, item, null);
                ErrorHandler.ThrowOnFailure(hr);
                if (hr == VSConstants.S_FALSE)
                {
                    break;
                }

                ////// Filter out items with diagnostic ID starting with 'CA'
                ////var columns = ((IVsTaskItem3)item[0]).GetTaskProvider().GetColumns().ToList();
                ////var errorCodeColumn = columns.SingleOrDefault(column => column.bstrCanonicalName == "errorcode");
                ////ErrorHandler.ThrowOnFailure(((IVsTaskItem3)item[0]).GetColumnValue(errorCodeColumn.iField, out var taskValueType, out var taskValueFlags, out var value, out var accessibilityName));
                ////if (taskValueType == (uint)__VSTASKVALUETYPE.TVT_TEXT)
                ////{
                ////    var errorCode = (string)value;
                ////    if (errorCode.StartsWith("CA"))
                ////    {
                ////        continue;
                ////    }
                ////}

                yield return item[0];
            }
        }

        public static int GetColumnCount(this IVsTaskProvider3 taskProvider)
        {
            ErrorHandler.ThrowOnFailure(taskProvider.GetColumnCount(out var columns));
            return columns;
        }

        public static IEnumerable<VSTASKCOLUMN> GetColumns(this IVsTaskProvider3 taskProvider)
        {
            var columnCount = taskProvider.GetColumnCount();
            var column = new VSTASKCOLUMN[1];
            for (var i = 0; i < columnCount; i++)
            {
                ErrorHandler.ThrowOnFailure(taskProvider.GetColumn(i, column));
                yield return column[0];
            }
        }

        public static IVsTaskProvider3 GetTaskProvider(this IVsTaskItem3 item)
        {
            ErrorHandler.ThrowOnFailure(((IVsTaskItem3)item).GetTaskProvider(out var provider));
            return provider;
        }

        public static __VSERRORCATEGORY GetCategory(this IVsErrorItem errorItem)
        {
            ErrorHandler.ThrowOnFailure(errorItem.GetCategory(out var category));
            return (__VSERRORCATEGORY)category;
        }

        public static string GetSeverity(this IVsTaskItem item)
        {
            return ((IVsErrorItem)item).GetCategory().AsString();
        }

        public static string GetDescription(this IVsTaskItem item)
        {
            ErrorHandler.ThrowOnFailure(item.get_Text(out var description));
            return description;
        }

        public static string GetProject(this IVsTaskItem item)
        {
            var errorItem = item as IVsErrorItem;
            ErrorHandler.ThrowOnFailure(errorItem.GetHierarchy(out var hierarchy));
            ErrorHandler.ThrowOnFailure(hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_Name, out var name));
            return (string)name;
        }

        public static string GetFileName(this IVsTaskItem item)
        {
            ErrorHandler.ThrowOnFailure(item.Document(out var fileName));
            return Path.GetFileName(fileName);
        }

        public static int GetLine(this IVsTaskItem item)
        {
            ErrorHandler.ThrowOnFailure(item.Line(out var line));
            return line + 1;
        }

        public static int GetColumn(this IVsTaskItem item)
        {
            ErrorHandler.ThrowOnFailure(item.Column(out var column));
            return column + 1;
        }

        public static string AsString(this __VSERRORCATEGORY errorCategory)
        {
            switch (errorCategory)
            {
                case __VSERRORCATEGORY.EC_MESSAGE:
                    return "Message";

                case __VSERRORCATEGORY.EC_WARNING:
                    return "Warning";

                case __VSERRORCATEGORY.EC_ERROR:
                    return "Error";

                default:
                    return "Unknown";
            }
        }
    }
}
