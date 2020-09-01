// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OptionServiceFactory(IGlobalOptionService globalOptionService)
            => _globalOptionService = globalOptionService;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new OptionService(_globalOptionService, workspaceServices);

        /// <summary>
        /// Wraps an underlying <see cref="IGlobalOptionService"/> and exposes its data to workspace
        /// clients.  Also takes the <see cref="IGlobalOptionService.OptionChanged"/> notifications
        /// and forwards them along using the same <see cref="TaskQueue"/> used by the
        /// <see cref="Workspace"/> this is connected to.  i.e. instead of synchronously just passing
        /// along the underlying events, these will be enqueued onto the workspace's eventing queue.
        /// </summary>
        internal sealed class OptionService : IWorkspaceOptionService
        {
            private readonly IGlobalOptionService _globalOptionService;
            private readonly TaskQueue _taskQueue;

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

                var schedulerProvider = workspaceServices.GetRequiredService<ITaskSchedulerProvider>();
                var listenerProvider = workspaceServices.GetRequiredService<IWorkspaceAsynchronousOperationListenerProvider>();
                _taskQueue = new TaskQueue(listenerProvider.GetListener(), schedulerProvider.CurrentContextScheduler);

                _globalOptionService.OptionChanged += OnGlobalOptionServiceOptionChanged;
            }

            public void OnWorkspaceDisposed(Workspace workspace)
            {
                // Disconnect us from the underlying global service.  That way it doesn't 
                // keep us around (and all the event handlers we're holding onto) forever.
                _globalOptionService.OptionChanged -= OnGlobalOptionServiceOptionChanged;
            }

            private void OnGlobalOptionServiceOptionChanged(object? sender, OptionChangedEventArgs e)
            {
                _taskQueue.ScheduleTask(nameof(OptionService) + "." + nameof(OnGlobalOptionServiceOptionChanged), () =>
                {
                    // Ensure we grab the event handlers inside the scheduled task to prevent a race of people unsubscribing
                    // but getting the event later on the UI thread
                    var eventHandlers = GetEventHandlers();
                    foreach (var handler in eventHandlers)
                    {
                        handler(this, e);
                    }
                }, CancellationToken.None);
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

            // Simple forwarding functions.
            public SerializableOptionSet GetOptions() => GetSerializableOptionsSnapshot(ImmutableHashSet<string>.Empty);
            public SerializableOptionSet GetSerializableOptionsSnapshot(ImmutableHashSet<string> languages) => _globalOptionService.GetSerializableOptionsSnapshot(languages, this);
            public object? GetOption(OptionKey optionKey) => _globalOptionService.GetOption(optionKey);
            public T? GetOption<T>(Option<T> option) => _globalOptionService.GetOption(option);
            public T? GetOption<T>(Option2<T> option) => _globalOptionService.GetOption(option);
            public T? GetOption<T>(PerLanguageOption<T> option, string? languageName) => _globalOptionService.GetOption(option, languageName);
            public T? GetOption<T>(PerLanguageOption2<T> option, string? languageName) => _globalOptionService.GetOption(option, languageName);
            public IEnumerable<IOption> GetRegisteredOptions() => _globalOptionService.GetRegisteredOptions();
            public bool TryMapEditorConfigKeyToOption(string key, string? language, [NotNullWhen(true)] out IEditorConfigStorageLocation2? storageLocation, out OptionKey optionKey) => _globalOptionService.TryMapEditorConfigKeyToOption(key, language, out storageLocation, out optionKey);
            public ImmutableHashSet<IOption> GetRegisteredSerializableOptions(ImmutableHashSet<string> languages) => _globalOptionService.GetRegisteredSerializableOptions(languages);
            public void SetOptions(OptionSet optionSet) => _globalOptionService.SetOptions(optionSet);
            public void RegisterWorkspace(Workspace workspace) => _globalOptionService.RegisterWorkspace(workspace);
            public void UnregisterWorkspace(Workspace workspace) => _globalOptionService.UnregisterWorkspace(workspace);

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
                private ImmutableDictionary<OptionKey, object?> _values;

                public DocumentSpecificOptionSet(List<IDocumentOptions> documentOptions, OptionSet underlyingOptions)
                    : this(documentOptions, underlyingOptions, ImmutableDictionary<OptionKey, object?>.Empty)
                {
                }

                public DocumentSpecificOptionSet(List<IDocumentOptions> documentOptions, OptionSet underlyingOptions, ImmutableDictionary<OptionKey, object?> values)
                {
                    _documentOptions = documentOptions;
                    _underlyingOptions = underlyingOptions;
                    _values = values;
                }

                [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/30819", AllowLocks = false)]
                private protected override object? GetOptionCore(OptionKey optionKey)
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

                public override OptionSet WithChangedOption(OptionKey optionAndLanguage, object? value)
                    => new DocumentSpecificOptionSet(_documentOptions, _underlyingOptions, _values.SetItem(optionAndLanguage, value));

                internal override IEnumerable<OptionKey> GetChangedOptions(OptionSet optionSet)
                {
                    // GetChangedOptions only needs to be supported for OptionSets that need to be compared during application,
                    // but that's already enforced it must be a full SerializableOptionSet.
                    throw new NotSupportedException();
                }
            }
        }
    }
}
