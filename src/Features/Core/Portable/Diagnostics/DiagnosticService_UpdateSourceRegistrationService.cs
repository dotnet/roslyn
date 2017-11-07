// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticUpdateSourceRegistrationService))]
    internal partial class DiagnosticService : IDiagnosticUpdateSourceRegistrationService
    {
        private ImmutableHashSet<IDiagnosticUpdateSource> _updateSources;

        public DiagnosticService()
        {
            // we use registry service rather than doing MEF import since MEF import method can have race issue where
            // update source gets created before aggregator - diagnostic service - is created and we will lose events fired before
            // the aggregator is created.
            _updateSources = ImmutableHashSet<IDiagnosticUpdateSource>.Empty;
        }

        public void Register(IDiagnosticUpdateSource source)
        {
            lock (_gate)
            {
                if (_updateSources.Contains(source))
                {
                    return;
                }

                _updateSources = _updateSources.Add(source);
                source.DiagnosticsUpdated += OnDiagnosticsUpdated;
            }
        }

        public void Shutdown(IDiagnosticUpdateSource source, Workspace workspace)
        {
            // we use event queue to preserve order between events. listener
            // will get all diagnostics changed async events before Shutdown happened.
            var eventToken = _listener.BeginAsyncOperation(nameof(Shutdown));
            _eventQueue.ScheduleTask(() =>
            {
                lock (_gate)
                {
                    if (!_map.TryGetValue(source, out var workspaceMap))
                    {
                        return;
                    }

                    // remove all data about the workspace from the given source
                    workspaceMap.Remove(workspace);
                }
            }).CompletesAsyncOperation(eventToken);
        }
    }
}
