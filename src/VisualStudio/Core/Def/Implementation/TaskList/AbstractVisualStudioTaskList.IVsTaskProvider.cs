// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    /// <summary>
    /// Since we're using the latest IVsTaskList3 RefreshOrAddTasksAsync and RemoveTasksAsync 
    /// APIs, we won't normally need to enumerate over each task list item.  
    /// 
    /// The only exception to this is if someone were to call RefreshAllProviders.  If we 
    /// want to handle that case, we should simply raise an event here that causes RoslynVSTaskList
    /// to re-add all of our items.  This is a terrible waste though and should never happen to us.
    /// </summary>
    internal abstract partial class AbstractVisualStudioTaskList : IVsTaskProvider
    {
        // This gets called once when we register, but shouldn't get called again 
        // except build case. in build case, VS will refresh all errors at the end of build
        public virtual int EnumTaskItems(out IVsEnumTaskItems ppenum)
        {
            ppenum = new EmptyVsEnumTaskItems();
            return VSConstants.S_OK;
        }

        public int ImageList(out IntPtr imageList)
        {
            imageList = default(IntPtr);
            return VSConstants.E_NOTIMPL;
        }

        public int OnTaskListFinalRelease(IVsTaskList taskList)
        {
            return VSConstants.E_NOTIMPL;
        }

        public int ReRegistrationKey(out string key)
        {
            key = null;
            return VSConstants.E_NOTIMPL;
        }

        public int SubcategoryList(uint cbstr, string[] str, out uint actual)
        {
            actual = default(uint);
            return VSConstants.E_NOTIMPL;
        }

        private class EmptyVsEnumTaskItems : IVsEnumTaskItems
        {
            public int Clone(out IVsEnumTaskItems ppenum)
            {
                ppenum = null;
                return VSConstants.E_NOTIMPL;
            }

            public int Next(uint celt, IVsTaskItem[] rgelt, uint[] pceltFetched)
            {
                return VSConstants.E_NOTIMPL;
            }

            public int Reset()
            {
                return VSConstants.E_NOTIMPL;
            }

            public int Skip(uint celt)
            {
                return VSConstants.E_NOTIMPL;
            }
        }
    }
}
