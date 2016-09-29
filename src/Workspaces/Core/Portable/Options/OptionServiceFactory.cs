// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Options
{
    [ExportWorkspaceServiceFactory(typeof(IOptionService)), Shared]
    internal class OptionServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IGlobalOptionService _globalOptionService;

        [ImportingConstructor]
        public OptionServiceFactory(IGlobalOptionService globalOptionService)
        {
            _globalOptionService = globalOptionService;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new OptionService(_globalOptionService, workspaceServices);
        }

        /// <summary>
        /// Wraps an underlying <see cref="IGlobalOptionService"/> and exposes its data to workspace
        /// clients.  Also takes the <see cref="IGlobalOptionService.OptionChanged"/> notifications
        /// and forwards them along using the same <see cref="IWorkspaceTaskScheduler"/> used by the
        /// <see cref="Workspace"/> this is connected to.  i.e. instead of synchronously just passing
        /// along the underlying events, these will be enqueued onto the workspaces eventing queue.
        /// </summary>
        // Internal for testing purposes.
        internal class OptionService : IWorkspaceOptionService
        {
            private readonly IGlobalOptionService _globalOptionService;

            // Can be null during testing.
            private readonly IWorkspaceTaskScheduler _taskQueue;

            private readonly object _gate = new object();
            private ImmutableArray<EventHandler<OptionChangedEventArgs>> _eventHandlers =
                ImmutableArray<EventHandler<OptionChangedEventArgs>>.Empty;

            public OptionService(
                IGlobalOptionService globalOptionService,
                HostWorkspaceServices workspaceServices)
            {
                _globalOptionService = globalOptionService;

                var workspaceTaskSchedulerFactory = workspaceServices?.GetRequiredService<IWorkspaceTaskSchedulerFactory>();
                _taskQueue = workspaceTaskSchedulerFactory?.CreateTaskQueue();

                _globalOptionService.OptionChanged += OnGlobalOptionServiceOptionChanged;
            }

            public void OnWorkspaceDisposed(Workspace workspace)
            {
                // Disconnect us from the underlying global service.  That way it doesn't 
                // keep us around (and all the event handlers we're holding onto) forever.
                _globalOptionService.OptionChanged -= OnGlobalOptionServiceOptionChanged;
            }

            private void OnGlobalOptionServiceOptionChanged(object sender, OptionChangedEventArgs e)
            {
                _taskQueue?.ScheduleTask(() =>
                {
                    // Ensure we grab the event handlers inside the scheduled task to prevent a race of people unsubscribing
                    // but getting the event later on the UI thread
                    var eventHandlers = GetEventHandlers();
                    foreach (var handler in eventHandlers)
                    {
                        handler(this, e);
                    }
                }, "OptionsService.SetOptions");
            }

            private ImmutableArray<EventHandler<OptionChangedEventArgs>> GetEventHandlers()
            {
                lock (_gate)
                {
                    return _eventHandlers;
                }
            }

            public event EventHandler<OptionChangedEventArgs> OptionChanged
            {
                add
                {
                    lock (_gate)
                    {
                        _eventHandlers = _eventHandlers.Add(value);
                    }
                }

                remove
                {
                    lock (_gate)
                    {
                        _eventHandlers = _eventHandlers.Remove(value);
                    }
                }
            }

            public OptionSet GetOptions()
            {
                return new WorkspaceOptionSet(this);
            }

            // Simple forwarding functions.
            public object GetOption(OptionKey optionKey) => _globalOptionService.GetOption(optionKey);
            public T GetOption<T>(Option<T> option) => _globalOptionService.GetOption(option);
            public T GetOption<T>(PerLanguageOption<T> option, string languageName) => _globalOptionService.GetOption(option, languageName);
            public IEnumerable<IOption> GetRegisteredOptions() => _globalOptionService.GetRegisteredOptions();
            public void SetOptions(OptionSet optionSet) => _globalOptionService.SetOptions(optionSet);
        }
    }
}