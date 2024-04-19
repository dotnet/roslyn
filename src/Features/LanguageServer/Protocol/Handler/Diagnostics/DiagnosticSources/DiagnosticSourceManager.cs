// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [Export(typeof(DiagnosticSourceManager)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class DiagnosticSourceManager([ImportMany] Lazy<IEnumerable<IDiagnosticSourceProvider>> sourceProviders) : IDiagnosticSourceManager
    {
        private ImmutableDictionary<string, IDiagnosticSourceProvider>? _documentProviders;
        private ImmutableDictionary<string, IDiagnosticSourceProvider>? _workspaceProviders;

        /// <inheritdoc />
        public IEnumerable<string> GetSourceNames(bool isDocument)
        {
            EnsureInitialized();
            return (isDocument ? _documentProviders : _workspaceProviders)!.Keys;
        }

        /// <inheritdoc />
        public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, string sourceName, bool isDocument, CancellationToken cancellationToken)
        {
            EnsureInitialized();
            var providersDictionary = isDocument ? _documentProviders : _workspaceProviders;
            if (providersDictionary!.TryGetValue(sourceName, out var provider))
                return provider.CreateDiagnosticSourcesAsync(context, cancellationToken);

            return new([]);
        }

        private void EnsureInitialized()
        {
            if (_documentProviders == null || _workspaceProviders == null)
            {
                var documentProviders = sourceProviders.Value
                    .Where(p => p.IsDocument)
                    .ToImmutableDictionary(kvp => kvp.Name, kvp => kvp);
                Interlocked.CompareExchange(ref _documentProviders, documentProviders, null);

                var workspaceProviders = sourceProviders.Value
                    .Where(p => !p.IsDocument)
                    .ToImmutableDictionary(kvp => kvp.Name, kvp => kvp);
                Interlocked.CompareExchange(ref _workspaceProviders, workspaceProviders, null);
            }
        }
    }
}
