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
    [Export(typeof(IDiagnosticSourceManager)), Shared]
    internal class DiagnosticSourceManager : IDiagnosticSourceManager
    {
        private readonly Lazy<ImmutableDictionary<string, IDiagnosticSourceProvider>> _documentProviders;
        private readonly Lazy<ImmutableDictionary<string, IDiagnosticSourceProvider>> _workspaceProviders;
        private readonly Lazy<IEnumerable<IDiagnosticSourceProvider>> sourceProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DiagnosticSourceManager(/*[ImportMany] Lazy<IEnumerable<IDiagnosticSourceProvider>> sourceProviders*/)
        {
            sourceProviders = new(() => new List<IDiagnosticSourceProvider>());
            _documentProviders = new(() => sourceProviders.Value
                    .Where(p => p.IsDocument)
                    .ToImmutableDictionary(kvp => kvp.Name, kvp => kvp));

            _workspaceProviders = new(() => sourceProviders.Value
                    .Where(p => !p.IsDocument)
                    .ToImmutableDictionary(kvp => kvp.Name, kvp => kvp));
        }

        /// <inheritdoc />
        public IEnumerable<string> GetSourceNames(bool isDocument)
            => (isDocument ? _documentProviders : _workspaceProviders).Value.Keys;

        /// <inheritdoc />
        public ValueTask<ImmutableArray<IDiagnosticSource>> CreateDiagnosticSourcesAsync(RequestContext context, string sourceName, bool isDocument, CancellationToken cancellationToken)
        {
            var providersDictionary = isDocument ? _documentProviders : _workspaceProviders;
            if (providersDictionary.Value.TryGetValue(sourceName, out var provider))
                return provider.CreateDiagnosticSourcesAsync(context, cancellationToken);

            return new([]);
        }
    }
}
