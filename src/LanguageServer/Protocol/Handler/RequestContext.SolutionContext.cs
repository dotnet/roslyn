// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal readonly partial struct RequestContext
{
    private sealed class SolutionContext
    {
        private readonly object _gate = new();
        private readonly LspWorkspaceManager _lspWorkspaceManager;
        private readonly TextDocumentIdentifier? _textDocumentIdentifier;
        private readonly ImmutableDictionary<DocumentUri, TrackedDocumentInfo> _trackedDocuments;
        private readonly Task _projectLoadTask;
        private readonly ILspLogger _logger;
        private readonly string _method;
        private readonly bool _mutatesSolutionState;

        private (Workspace Workspace, Solution Solution, TextDocument? Document) _initialValue;
        private Task<(Workspace Workspace, Solution Solution, TextDocument? Document)>? _resolutionTask;
        private bool _isCleared;

        public SolutionContext(
            Workspace workspace,
            Solution solution,
            TextDocument? document,
            LspWorkspaceManager lspWorkspaceManager,
            TextDocumentIdentifier? textDocumentIdentifier,
            ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
            Task projectLoadTask,
            ILspLogger logger,
            string method,
            bool mutatesSolutionState)
        {
            _initialValue = (workspace, solution, document);
            _lspWorkspaceManager = lspWorkspaceManager;
            _textDocumentIdentifier = textDocumentIdentifier;
            _trackedDocuments = trackedDocuments;
            _projectLoadTask = projectLoadTask;
            _logger = logger;
            _method = method;
            _mutatesSolutionState = mutatesSolutionState;
        }

        public (Workspace Workspace, Solution Solution, TextDocument? Document) GetInitialValue()
        {
            lock (_gate)
            {
                if (_isCleared)
                    throw new InvalidOperationException();

                return _initialValue;
            }
        }

        public async ValueTask<(Workspace Workspace, Solution Solution, TextDocument? Document)> GetValueAsync(
            CancellationToken cancellationToken)
        {
            Task<(Workspace Workspace, Solution Solution, TextDocument? Document)> resolutionTask;
            lock (_gate)
            {
                if (_isCleared)
                    throw new InvalidOperationException();

                if (_mutatesSolutionState)
                    return _initialValue;

                resolutionTask = _resolutionTask ??= ResolveAsync(_initialValue);
            }

            var resolvedValue = await resolutionTask.WithCancellation(cancellationToken).ConfigureAwait(false);
            lock (_gate)
            {
                if (_isCleared)
                    throw new InvalidOperationException();

                return resolvedValue;
            }
        }

        private async Task<(Workspace Workspace, Solution Solution, TextDocument? Document)> ResolveAsync(
            (Workspace Workspace, Solution Solution, TextDocument? Document) initialValue)
        {
            try
            {
                try
                {
                    await _projectLoadTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return initialValue;
                }

                if (_textDocumentIdentifier is not null)
                {
                    var documentContext = await _lspWorkspaceManager.GetUncachedLspDocumentInfoAsync(
                        _textDocumentIdentifier, _trackedDocuments, cancellationToken: CancellationToken.None).ConfigureAwait(false);
                    if (documentContext is { Workspace: not null, Solution: not null })
                        return (documentContext.Workspace, documentContext.Solution, documentContext.Document);

                    if (initialValue.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
                        return initialValue;
                }

                var solutionContext = await _lspWorkspaceManager.GetUncachedLspSolutionInfoAsync(
                    _trackedDocuments, cancellationToken: CancellationToken.None).ConfigureAwait(false);
                if (solutionContext is { Workspace: not null, Solution: not null })
                    return (solutionContext.Workspace, solutionContext.Solution, Document: null);
            }
            catch (Exception exception) when (FatalError.ReportAndCatch(exception))
            {
                _logger.LogException(exception);
                _logger.LogWarning($"Could not refresh solution context after project loading on {_method}.");
            }

            return initialValue;
        }

        public void Clear()
        {
            lock (_gate)
            {
                _initialValue = default;
                _resolutionTask = null;
                _isCleared = true;
            }
        }
    }
}
