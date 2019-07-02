// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

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
        /// along the underlying events, these will be enqueued onto the workspace's eventing queue.
        /// </summary>
        internal class OptionService : IWorkspaceOptionService
        {
            private readonly IGlobalOptionService _globalOptionService;
            private readonly IWorkspaceTaskScheduler _taskQueue;

            /// <summary>
            /// Gate guarding <see cref="_eventHandlers"/> and <see cref="_documentOptionsProviders"/>.
            /// </summary>
            private readonly object _gate = new object();

            private ImmutableArray<EventHandler<OptionChangedEventArgs>> _eventHandlers =
                ImmutableArray<EventHandler<OptionChangedEventArgs>>.Empty;

            private ImmutableArray<IDocumentOptionsProvider> _documentOptionsProviders =
                ImmutableArray<IDocumentOptionsProvider>.Empty;

            public OptionService(
                IGlobalOptionService globalOptionService,
                HostWorkspaceServices workspaceServices)
            {
                _globalOptionService = globalOptionService;

                var workspaceTaskSchedulerFactory = workspaceServices.GetRequiredService<IWorkspaceTaskSchedulerFactory>();
                _taskQueue = workspaceTaskSchedulerFactory.CreateEventingTaskQueue();

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
                _taskQueue.ScheduleTask(() =>
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

            public void RegisterDocumentOptionsProvider(IDocumentOptionsProvider documentOptionsProvider)
            {
                if (documentOptionsProvider == null)
                {
                    throw new ArgumentNullException(nameof(documentOptionsProvider));
                }

                lock (_gate)
                {
                    _documentOptionsProviders = _documentOptionsProviders.Add(documentOptionsProvider);
                }
            }

            public async Task<OptionSet> GetUpdatedOptionSetForDocumentAsync(Document document, OptionSet optionSet, CancellationToken cancellationToken)
            {
                ImmutableArray<IDocumentOptionsProvider> documentOptionsProviders;

                lock (_gate)
                {
                    documentOptionsProviders = _documentOptionsProviders;
                }

                var realizedDocumentOptions = new List<IDocumentOptions>();

                foreach (var provider in documentOptionsProviders)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var documentOption = await provider.GetOptionsForDocumentAsync(document, cancellationToken).ConfigureAwait(false);

                    if (documentOption != null)
                    {
                        realizedDocumentOptions.Add(documentOption);
                    }
                }

                return new DocumentSpecificOptionSet(realizedDocumentOptions, optionSet);
            }

            private class DocumentSpecificOptionSet : OptionSet
            {
                private readonly OptionSet _underlyingOptions;
                private readonly List<IDocumentOptions> _documentOptions;
                private ImmutableDictionary<OptionKey, object> _values;

                public DocumentSpecificOptionSet(List<IDocumentOptions> documentOptions, OptionSet underlyingOptions)
                    : this(documentOptions, underlyingOptions, ImmutableDictionary<OptionKey, object>.Empty)
                {
                }

                public DocumentSpecificOptionSet(List<IDocumentOptions> documentOptions, OptionSet underlyingOptions, ImmutableDictionary<OptionKey, object> values)
                {
                    _documentOptions = documentOptions;
                    _underlyingOptions = underlyingOptions;
                    _values = values;
                }

                [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
                public override object GetOption(OptionKey optionKey)
                {
                    // If we already know the document specific value, we're done
                    if (_values.TryGetValue(optionKey, out var value))
                    {
                        return value;
                    }

                    foreach (var documentOptionSource in _documentOptions)
                    {
                        if (documentOptionSource.TryGetDocumentOption(optionKey, out value))
                        {
                            // Cache and return
                            return ImmutableInterlocked.GetOrAdd(ref _values, optionKey, value);
                        }
                    }

                    // We don't have a document specific value, so forward
                    return _underlyingOptions.GetOption(optionKey);
                }

                public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object value)
                {
                    return new DocumentSpecificOptionSet(_documentOptions, _underlyingOptions, _values.Add(optionAndLanguage, value));
                }

                internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
                {
                    // GetChangedOptions only needs to be supported for OptionSets that need to be compared during application,
                    // but that's already enforced it must be a full WorkspaceOptionSet.
                    throw new NotSupportedException();
                }
            }
        }
    }
}
