// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Notifies EnC service of host workspace events.
/// </summary>
[ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
[Export(typeof(PdbMatchingSourceTextProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class PdbMatchingSourceTextProvider() : IEventListener, IPdbMatchingSourceTextProvider
{
    private readonly object _guard = new();

    private bool _isActive;
    private int _baselineSolutionContentVersion;
    private readonly Dictionary<string, (DocumentState state, int solutionVersion)> _documentsWithChangedLoaderByPath = [];
    private WorkspaceEventRegistration? _workspaceChangedDisposer;

    public void StartListening(Workspace workspace)
    {
        Debug.Assert(_workspaceChangedDisposer == null);

        _workspaceChangedDisposer = workspace.RegisterWorkspaceChangedHandler(WorkspaceChanged);
    }

    public void StopListening(Workspace workspace)
    {
        _workspaceChangedDisposer?.Dispose();
        _workspaceChangedDisposer = null;
    }

    private void WorkspaceChanged(WorkspaceChangeEventArgs e)
    {
        if (!_isActive)
        {
            // Not capturing document states because debugging session isn't active.
            return;
        }

        if (e.DocumentId == null)
        {
            return;
        }

        var oldDocument = e.OldSolution.GetDocument(e.DocumentId);
        if (oldDocument == null)
        {
            // document added
            return;
        }

        var newDocument = e.NewSolution.GetDocument(e.DocumentId);
        if (newDocument == null)
        {
            // document removed
            return;
        }

        if (!oldDocument.State.SupportsEditAndContinue())
        {
            return;
        }

        Contract.ThrowIfNull(oldDocument.FilePath);

        // When a document is open its loader transitions from file-based loader to text buffer based.
        // The file checksum is no longer available from the latter, so capture it at this moment.
        if (oldDocument.State.TextAndVersionSource.CanReloadText && !newDocument.State.TextAndVersionSource.CanReloadText)
        {
            var oldSolutionVersion = oldDocument.Project.Solution.SolutionStateContentVersion;

            lock (_guard)
            {
                // ignore updates to a document that we have already seen this session:
                if (_isActive && oldSolutionVersion >= _baselineSolutionContentVersion && !_documentsWithChangedLoaderByPath.ContainsKey(oldDocument.FilePath))
                {
                    _documentsWithChangedLoaderByPath.Add(oldDocument.FilePath, (oldDocument.DocumentState, oldSolutionVersion));
                }
            }
        }
    }

    /// <summary>
    /// Establish a baseline snapshot. The listener will ignore all document snapshots that are older.
    /// </summary>
    public void SetBaseline(Solution solution)
    {
        lock (_guard)
        {
            _baselineSolutionContentVersion = solution.SolutionStateContentVersion;
        }
    }

    public void Activate()
    {
        lock (_guard)
        {
            _isActive = true;
        }
    }

    public void Deactivate()
    {
        lock (_guard)
        {
            _isActive = false;
            _documentsWithChangedLoaderByPath.Clear();
        }
    }

    public async ValueTask<string?> TryGetMatchingSourceTextAsync(string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
    {
        DocumentState? state;
        lock (_guard)
        {
            if (!_documentsWithChangedLoaderByPath.TryGetValue(filePath, out var stateAndVersion))
            {
                return null;
            }

            state = stateAndVersion.state;
        }

        if (state.LoadTextOptions.ChecksumAlgorithm != checksumAlgorithm)
        {
            return null;
        }

        var text = await state.GetTextAsync(cancellationToken).ConfigureAwait(false);
        if (!text.GetChecksum().SequenceEqual(requiredChecksum))
        {
            return null;
        }

        return text.ToString();
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly PdbMatchingSourceTextProvider _instance;

        internal TestAccessor(PdbMatchingSourceTextProvider instance)
            => _instance = instance;

        public ImmutableDictionary<string, (DocumentState state, int solutionVersion)> GetDocumentsWithChangedLoaderByPath()
        {
            lock (_instance._guard)
            {
                return _instance._documentsWithChangedLoaderByPath.ToImmutableDictionary();
            }
        }
    }
}
