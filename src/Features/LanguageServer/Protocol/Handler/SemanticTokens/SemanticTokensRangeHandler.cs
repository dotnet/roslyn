// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
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
    internal class SemanticTokensRangeHandler : AbstractStatelessRequestHandler<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>
    {
        private readonly IGlobalOptionService _globalOptions;
        private readonly Dictionary<ProjectId, Task<Compilation?>> _projectIdToCompilation = new();

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangeHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
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

            // Razor uses isFinalized to determine whether to cache tokens. We should be able to
            // remove it altogether once Roslyn implements workspace/semanticTokens/refresh:
            // https://github.com/dotnet/roslyn/issues/60441
            var isFinalized = !context.Document.IsRazorDocument() ||
                await IsDataFinalizedAsync(project, cancellationToken).ConfigureAwait(false);

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

            return new RoslynSemanticTokens { Data = tokensData, IsFinalized = isFinalized };
        }

        private async Task<bool> IsDataFinalizedAsync(Project project, CancellationToken cancellationToken)
        {
            // If the project's compilation isn't yet available, kick off a task in the background to
            // hopefully make it available faster since we'll need it later to compute tokens.
            if (!_projectIdToCompilation.ContainsKey(project.Id))
            {
                var compilationTask = project.GetCompilationAsync(cancellationToken);
                _projectIdToCompilation.Add(project.Id, compilationTask);
            }

            // We use a combination of IsFullyLoaded + the completed project compilation as the metric
            // for isFinalized. It may not be completely accurate but this is only a a temporary fix until
            // workspace/semanticTokens/refresh is implemented.
            var isFinalized = false;
            var workspaceStatusService = project.Solution.Workspace.Services.GetRequiredService<IWorkspaceStatusService>();
            var isFullyLoaded = await workspaceStatusService.IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (isFullyLoaded)
            {
                Contract.ThrowIfFalse(_projectIdToCompilation.TryGetValue(project.Id, out var compilationTask));
                if (compilationTask.IsCompleted)
                {
                    isFinalized = true;

                    // We don't want to hang on to the compilation since this can be very expensive,
                    // but we do want to mark the compilation as being successfully retrieved.
                    _projectIdToCompilation[project.Id] = Task.FromResult<Compilation?>(null);
                }
            }

            return isFinalized;
        }
    }
}
