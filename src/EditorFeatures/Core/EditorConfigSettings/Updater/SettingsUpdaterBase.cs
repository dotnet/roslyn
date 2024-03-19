// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

internal abstract class SettingsUpdaterBase<TOption, TValue> : ISettingUpdater<TOption, TValue>
{
    private readonly List<(TOption option, TValue value)> _queue = [];
    private readonly SemaphoreSlim _guard = new(1);
    private readonly IAsynchronousOperationListener _listener;
    protected readonly Workspace Workspace;
    protected readonly string EditorconfigPath;

    protected abstract SourceText? GetNewText(SourceText analyzerConfigDocument, IReadOnlyList<(TOption option, TValue value)> settingsToUpdate, CancellationToken token);

    protected SettingsUpdaterBase(Workspace workspace, string editorconfigPath)
    {
        Workspace = workspace;
        _listener = workspace.Services.GetRequiredService<IWorkspaceAsynchronousOperationListenerProvider>().GetListener();
        EditorconfigPath = editorconfigPath;
    }

    public void QueueUpdate(TOption setting, TValue value)
    {
        var token = _listener.BeginAsyncOperation(nameof(QueueUpdate));
        _ = QueueUpdateAsync().CompletesAsyncOperation(token);

        return;

        // local function
        async Task QueueUpdateAsync()
        {
            using (await _guard.DisposableWaitAsync().ConfigureAwait(false))
            {
                _queue.Add((setting, value));
            }
        }
    }

    public async Task<SourceText?> GetChangedEditorConfigAsync(AnalyzerConfigDocument? analyzerConfigDocument, CancellationToken token)
    {
        if (analyzerConfigDocument is null)
            return null;

        var originalText = await analyzerConfigDocument.GetValueTextAsync(token).ConfigureAwait(false);
        using (await _guard.DisposableWaitAsync(token).ConfigureAwait(false))
        {
            var newText = GetNewText(originalText, _queue, token);
            if (newText is null || newText.Equals(originalText))
            {
                _queue.Clear();
                return null;
            }
            else
            {
                _queue.Clear();
                return newText;
            }
        }
    }

    public async Task<IReadOnlyList<TextChange>?> GetChangedEditorConfigAsync(CancellationToken token)
    {
        var solution = Workspace.CurrentSolution;
        var analyzerConfigDocument = solution.Projects
            .SelectMany(p => p.AnalyzerConfigDocuments)
            .FirstOrDefault(d => d.FilePath == EditorconfigPath);
        var newText = await GetChangedEditorConfigAsync(analyzerConfigDocument, token).ConfigureAwait(false);
        if (newText is null)
        {
            return null;
        }

        var originalText = await analyzerConfigDocument!.GetValueTextAsync(token).ConfigureAwait(false);
        return newText.GetTextChanges(originalText);
    }

    public async Task<SourceText?> GetChangedEditorConfigAsync(SourceText originalText, CancellationToken token)
    {
        using (await _guard.DisposableWaitAsync(token).ConfigureAwait(false))
        {
            var newText = GetNewText(originalText, _queue, token);
            if (newText is null || newText.Equals(originalText))
            {
                _queue.Clear();
                return null;
            }
            else
            {
                _queue.Clear();
                return newText;
            }
        }
    }

    public async Task<bool> HasAnyChangesAsync()
    {
        using (await _guard.DisposableWaitAsync().ConfigureAwait(false))
        {
            return _queue.Any();
        }
    }
}
