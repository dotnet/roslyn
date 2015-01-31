// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    internal abstract partial class AbstractVisualStudioTaskList : IVsTaskProvider
    {
        protected readonly bool ErrorListInstalled;
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IAsynchronousOperationListener Listener;
        protected readonly IForegroundNotificationService NotificationService;

        private uint _providerCookie;
        private IVsTaskList3 _tasklist;

        protected AbstractVisualStudioTaskList(
            IServiceProvider serviceProvider,
            IForegroundNotificationService notificationService,
            string featureName,
            IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            Contract.ThrowIfNull(serviceProvider);
            this.ServiceProvider = serviceProvider;

            this.NotificationService = notificationService;
            this.Listener = new AggregateAsynchronousOperationListener(asyncListeners, featureName);

            // TODO: remove this type and its derived types.
            // now, new error list is installed by default, and no way to take it out.
            // set ErrorListInstalled to true
            this.ErrorListInstalled = true;
        }

        /// <summary>
        /// should be called from derived class since registering task list will call us back to enumerate existing task items.
        /// </summary>
        protected void InitializeTaskList()
        {
            _tasklist = ServiceProvider.GetService(typeof(SVsTaskList)) as IVsTaskList3;

            Contract.ThrowIfNull(_tasklist);
            Contract.ThrowIfNull(_tasklist as IVsTaskList); // IVsTaskList3 doesn't implement IVsTaskList.
            Contract.ThrowIfFalse(((IVsTaskList)_tasklist).RegisterTaskProvider(this, out _providerCookie) == VSConstants.S_OK);
        }

        protected IVsTask RemoveTasks(IVsTaskItem[] oldTasks)
        {
            return _tasklist.RemoveTasksAsync(_providerCookie, oldTasks.Length, oldTasks);
        }

        protected IVsTask RefreshOrAddTasks(IVsTaskItem[] newTasks)
        {
            if (newTasks.Length <= 0)
            {
                return null;
            }

            // Don't update more than this many items at a time to avoid perf problems
            const int MaxTasksToAddInOneCall = 50;
            if (newTasks.Length <= MaxTasksToAddInOneCall)
            {
                return _tasklist.RefreshOrAddTasksAsync(_providerCookie, newTasks.Length, newTasks);
            }
            else
            {
                var arraySubset = new IVsTaskItem[MaxTasksToAddInOneCall];

                IVsTask lastTask = null;
                for (int i = 0; i < newTasks.Length; i += MaxTasksToAddInOneCall)
                {
                    var subsetSize = Math.Min(MaxTasksToAddInOneCall, newTasks.Length - i);
                    if (subsetSize > 0)
                    {
                        Array.Copy(newTasks, i, arraySubset, 0, subsetSize);
                        lastTask = _tasklist.RefreshOrAddTasksAsync(_providerCookie, subsetSize, arraySubset);
                    }
                }

                // we only need last task. VS already serialize request internally
                return lastTask;
            }
        }
    }
}
