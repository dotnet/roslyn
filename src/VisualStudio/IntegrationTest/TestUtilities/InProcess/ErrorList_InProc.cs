// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Common;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class ErrorList_InProc : InProcComponent
    {
        public static ErrorList_InProc Create()
            => new ErrorList_InProc();

        public void ShowErrorList()
            => ExecuteCommand("View.ErrorList");

        public void WaitForNoErrorsInErrorList(TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            while (GetErrorCount() != 0)
            {
                if (stopwatch.Elapsed >= timeout)
                {
                    var message = new StringBuilder();
                    message.AppendLine("Unexpected errors in error list:");
                    foreach (var error in GetErrorListContents())
                    {
                        message.Append("  ").AppendLine(error.ToString());
                    }

                    throw new TimeoutException(message.ToString());
                }

                Thread.Yield();
            }
        }

        public int GetErrorCount(__VSERRORCATEGORY minimumSeverity = __VSERRORCATEGORY.EC_WARNING)
        {
            var errorItems = GetErrorItems();
            try
            {
                return errorItems
                    .AsEnumerable()
                    .Where(e => ((IVsErrorItem)e).GetCategory() <= minimumSeverity)
                    .Count();
            }
            catch (IndexOutOfRangeException)
            {
                // It is entirely possible that the items in the error list are modified
                // after we start iterating, in which case we want to try again.
                return GetErrorCount(minimumSeverity);
            }
        }

        public ErrorListItem[] GetErrorListContents(__VSERRORCATEGORY minimumSeverity = __VSERRORCATEGORY.EC_WARNING)
        {
            var errorItems = GetErrorItems();
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
                return GetErrorListContents(minimumSeverity);
            }
        }

        private static IVsEnumTaskItems GetErrorItems()
        {
            return InvokeOnUIThread(cancellationToken =>
            {
                var errorList = GetGlobalService<SVsErrorList, IVsTaskList>();
                ErrorHandler.ThrowOnFailure(errorList.EnumTaskItems(out var items));
                return items;
            });
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

                yield return item[0];
            }
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
            var errorItem = (IVsErrorItem)item;
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
