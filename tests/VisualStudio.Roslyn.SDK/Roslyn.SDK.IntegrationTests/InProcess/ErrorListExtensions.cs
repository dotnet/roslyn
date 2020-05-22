// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Testing.InProcess
{
    internal static class ErrorListExtensions
    {
        public static IEnumerable<IVsTaskItem> AsEnumerable(this IVsEnumTaskItems items)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

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

        public static __VSERRORCATEGORY GetCategory(this IVsErrorItem errorItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ErrorHandler.ThrowOnFailure(errorItem.GetCategory(out var category));
            return (__VSERRORCATEGORY)category;
        }
    }
}
