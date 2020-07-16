// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Export(typeof(IDiagnosticUpdateSourceRegistrationService))]
    internal partial class DiagnosticService : IDiagnosticUpdateSourceRegistrationService
    {
        private ImmutableHashSet<IDiagnosticUpdateSource> _updateSources;

        [SuppressMessage("RoslyDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification = "Private constructor used for deterministic field initialization")]
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
                source.DiagnosticsCleared += OnCleared;
            }
        }
    }
}
