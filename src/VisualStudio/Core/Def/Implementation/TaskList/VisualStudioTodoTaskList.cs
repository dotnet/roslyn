// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList
{
    [Export(typeof(VisualStudioTodoTaskList))]
    internal partial class VisualStudioTodoTaskList : AbstractVisualStudioTaskList
    {
        private readonly Dictionary<object, IVsTaskItem[]> _todoItemMap = new Dictionary<object, IVsTaskItem[]>();
        private readonly IOptionService _optionService;
        private readonly ITodoListProvider _todoListProvider;

        // Batch process updates from the pendingUpdates list.
        private readonly object _gate = new object();
        private readonly List<ValueTuple<object, ImmutableArray<ITaskItem>>> _pendingUpdates = new List<ValueTuple<object, ImmutableArray<ITaskItem>>>();
        private bool _notificationQueued = false;

        [ImportingConstructor]
        public VisualStudioTodoTaskList(
            SVsServiceProvider serviceProvider,
            IForegroundNotificationService notificationService,
            IOptionService optionService,
            ITodoListProvider todoListProvider,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners) :
            base(serviceProvider, notificationService, FeatureAttribute.TodoCommentList, asyncListeners)
        {
            // this should be called after all fields are initialized
            InitializeTaskList();

            _optionService = optionService;
            _todoListProvider = todoListProvider;

            // we return after initializing task provider since getting option information
            // require old task list provider.
            if (ErrorListInstalled)
            {
                return;
            }

            todoListProvider.TodoListUpdated += OnTodoListUpdated;
        }

        public override int EnumTaskItems(out IVsEnumTaskItems ppenum)
        {
            RefreshOrAddTasks(_todoItemMap.Values.SelectMany(x => x).ToArray());

            return base.EnumTaskItems(out ppenum);
        }

        private void OnTodoListUpdated(object sender, TaskListEventArgs args)
        {
            if (args.TaskListType != PredefinedTaskItemTypes.Todo)
            {
                return;
            }

            lock (_gate)
            {
                _pendingUpdates.Add(ValueTuple.Create(args.Id, args.TaskItems));
                if (!_notificationQueued)
                {
                    _notificationQueued = true;
                    UpdateTodoTaskList();
                }
            }
        }

        private void UpdateTodoTaskList()
        {
            this.NotificationService.RegisterNotification(() =>
            {
                using (Logger.LogBlock(FunctionId.TaskList_Refresh, CancellationToken.None))
                {
                    using (var listPooledObject = SharedPools.Default<List<IVsTaskItem>>().GetPooledObject())
                    using (var mapPooledObject = SharedPools.Default<Dictionary<object, ImmutableArray<ITaskItem>>>().GetPooledObject())
                    {
                        var removedTasks = listPooledObject.Object;
                        var addedTasks = mapPooledObject.Object;

                        lock (_gate)
                        {
                            foreach (var args in _pendingUpdates)
                            {
                                var key = args.Item1;
                                var data = args.Item2;

                                IVsTaskItem[] oldTasks;
                                if (_todoItemMap.TryGetValue(key, out oldTasks))
                                {
                                    removedTasks.AddRange(oldTasks);
                                    _todoItemMap.Remove(key);
                                }
                                else
                                {
                                    addedTasks.Remove(key);
                                }

                                if (data.Any())
                                {
                                    addedTasks[key] = data;
                                }
                            }

                            _notificationQueued = false;
                            _pendingUpdates.Clear();
                        }

                        if (removedTasks.Count > 0)
                        {
                            RemoveTasks(removedTasks.ToArray());
                        }

                        using (var taskList = SharedPools.Default<List<IVsTaskItem>>().GetPooledObject())
                        {
                            var newTasks = taskList.Object;
                            foreach (var addedTaskList in addedTasks)
                            {
                                _todoItemMap[addedTaskList.Key] = addedTaskList.Value.Select(i => new VisualStudioTaskItem((TodoTaskItem)i)).ToArray();
                                newTasks.AddRange(_todoItemMap[addedTaskList.Key]);
                            }

                            RefreshOrAddTasks(newTasks.ToArray());
                        }
                    }
                }
            }, TaggerConstants.MediumDelay, this.Listener.BeginAsyncOperation("RefreshTaskList"));
        }

        internal void TestOnly_Enable()
        {
            if (!ErrorListInstalled)
            {
                return;
            }

            _todoListProvider.TodoListUpdated += OnTodoListUpdated;
        }
    }
}
