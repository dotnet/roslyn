// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [Export(typeof(DiagnosticSourceManager)), Shared]
    internal class DiagnosticSourceManager : IDiagnosticSourceManager
    {
        private readonly Lazy<IEnumerable<IDiagnosticSourceProvider>> _sources;
        private ImmutableDictionary<string, ImmutableArray<IDiagnosticSourceProvider>>? _documentSources;
        private ImmutableDictionary<string, ImmutableArray<IDiagnosticSourceProvider>>? _workspaceSources;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticSourceManager([ImportMany] Lazy<IEnumerable<IDiagnosticSourceProvider>> sources)
        {
            _sources = sources;
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSourceNames(bool isDocument)
        {
            EnsureInitialized();
            return (isDocument ? _documentSources : _workspaceSources)!.Keys;
        }

        /// <inheritdoc />
        public async ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, string sourceName, bool isDocument, CancellationToken cancellationToken)
        {
            EnsureInitialized();
            var providersDictionary = isDocument ? _documentSources : _workspaceSources;
            if (!providersDictionary!.TryGetValue(sourceName, out var providers))
            {
                return [];
            }

            using var _ = ArrayBuilder<IDiagnosticSource>.GetInstance(out var builder);
            foreach (var provider in providers)
            {
                var sources = await provider.CreateDiagnosticSourcesAsync(context, sourceName, cancellationToken).ConfigureAwait(false);
                builder.AddRange(sources);
            }

            return builder.ToImmutableAndClear();
        }

        private void EnsureInitialized()
        {
            if (_documentSources == null || _workspaceSources == null)
            {
                Dictionary<string, IReadOnlyList<IDiagnosticSourceProvider>> documentSources = new();
                Dictionary<string, IReadOnlyList<IDiagnosticSourceProvider>> workspaceSources = new();
                foreach (var source in _sources.Value)
                {
                    var attribute = source.GetType().GetCustomAttributes<ExportDiagnosticSourceProviderAttribute>(inherit: false).FirstOrDefault();
                    if (attribute != null)
                    {
                        var scopedSources = source.IsDocument ? documentSources : workspaceSources;
                        foreach (var sourceName in source.SourceNames)
                        {
                            if (!scopedSources.TryGetValue(sourceName, out var sources))
                            {
                                sources = new List<IDiagnosticSourceProvider>();
                                scopedSources[sourceName] = sources;
                            }
                            ((List<IDiagnosticSourceProvider>)sources).Add(source);
                        }
                    }
                }

                var immutableSources = documentSources.ToImmutableDictionary(entry => entry.Key, entry => entry.Value.ToImmutableArray());
                Interlocked.CompareExchange(ref _documentSources, immutableSources, null);
                immutableSources = workspaceSources.ToImmutableDictionary(entry => entry.Key, entry => entry.Value.ToImmutableArray());
                Interlocked.CompareExchange(ref _workspaceSources, immutableSources, null);
            }
        }
    }
}
