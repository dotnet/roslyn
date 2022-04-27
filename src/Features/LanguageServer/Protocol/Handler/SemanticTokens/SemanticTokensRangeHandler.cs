// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Api;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Computes the semantic tokens for a given range.
    /// </summary>
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(SemanticTokensRangeHandler)), Shared]
    [Method(Methods.TextDocumentSemanticTokensRangeName)]
    internal class SemanticTokensRangeHandler : AbstractStatelessRequestHandler<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>, IDisposable
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly IAsynchronousOperationListener _asyncListener;

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        private readonly object _gate = new();
        private readonly Dictionary<ProjectId, CompilationAvailableEventSource> _projectIdToEventSource = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangeHandler(
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider asynchronousOperationListenerProvider)
        {
            _globalOptions = globalOptions;
            _asyncListener = asynchronousOperationListenerProvider.GetListener(FeatureAttribute.Classification);
        }

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.SemanticTokensRangeParams request)
        {
            Contract.ThrowIfNull(request.TextDocument);
            return request.TextDocument;
        }

        public override async Task<LSP.SemanticTokens> HandleRequestAsync(
            LSP.SemanticTokensRangeParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
            Contract.ThrowIfNull(context.Document, "Document is null.");

            var project = context.Document.Project;
            var options = _globalOptions.GetClassificationOptions(project.Language);

            // The results from the range handler should not be cached since we don't want to cache
            // partial token results. In addition, a range request is only ever called with a whole
            // document request, so caching range results is unnecessary since the whole document
            // handler will cache the results anyway.
            var tokensData = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                context.Document,
                SemanticTokensHelpers.TokenTypeToIndex,
                request.Range,
                options,
                includeSyntacticClassifications: context.Document.IsRazorDocument(),
                cancellationToken).ConfigureAwait(false);

            lock (_gate)
            {
                if (!_projectIdToEventSource.TryGetValue(project.Id, out var eventSource))
                {
                    eventSource = new CompilationAvailableEventSource(_asyncListener);
                    _projectIdToEventSource.Add(project.Id, eventSource);

                    eventSource.OnCompilationAvailable += OnCompilationAvailable;
                }

                // Kick off work to have this project be sync'ed over to OOP so it's ready in the future for an upcoming 
                eventSource.EnsureCompilationAvailability(project);
            }

            return new LSP.SemanticTokens { Data = tokensData };
        }

        private void OnCompilationAvailable()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            ImmutableArray<CompilationAvailableEventSource> eventSources;
            lock (_gate)
            {
                eventSources = _projectIdToEventSource.Values.ToImmutableArray();
                _projectIdToEventSource.Clear();
            }

            foreach (var eventSource in eventSources)
            {
                eventSource.OnCompilationAvailable -= OnCompilationAvailable;
                eventSource.Dispose();
            }
        }
    }
}
