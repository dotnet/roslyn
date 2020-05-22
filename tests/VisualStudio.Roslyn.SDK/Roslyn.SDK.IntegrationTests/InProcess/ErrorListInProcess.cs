// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Testing.InProcess
{
    public class ErrorListInProcess : InProcComponent
    {
        public ErrorListInProcess(TestServices testServices)
            : base(testServices)
        {
        }

        public async Task<int> GetErrorCountAsync(__VSERRORCATEGORY minimumSeverity = __VSERRORCATEGORY.EC_WARNING)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var errorItems = await GetErrorItemsAsync();
            try
            {
                return errorItems
                    .AsEnumerable()
                    .Where(e =>
                    {
                        ThreadHelper.ThrowIfNotOnUIThread();
                        return ((IVsErrorItem)e).GetCategory() <= minimumSeverity;
                    })
                    .Count();
            }
            catch (IndexOutOfRangeException)
            {
                // It is entirely possible that the items in the error list are modified
                // after we start iterating, in which case we want to try again.
                return await GetErrorCountAsync(minimumSeverity);
            }
        }

        private async Task<IVsEnumTaskItems> GetErrorItemsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var errorList = await GetGlobalServiceAsync<SVsErrorList, IVsTaskList>();
            ErrorHandler.ThrowOnFailure(errorList.EnumTaskItems(out var items));
            return items;
        }
    }
}
