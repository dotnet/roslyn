// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater
{
    internal abstract class SettingsUpdaterBase<TOption, TValue> : ISettingUpdater<TOption, TValue>
    {
        private readonly List<(TOption option, TValue value)> _queue = new();
        private readonly SemaphoreSlim _guard = new(1);
        private readonly Workspace _workspace;
        private readonly string _editorconfigPath;

        protected abstract Task<SourceText?> GetNewTextAsync(AnalyzerConfigDocument analyzerConfigDocument, IReadOnlyList<(TOption option, TValue value)> settingsToUpdate, CancellationToken token);

        protected SettingsUpdaterBase(Workspace workspace, string editorconfigPath)
        {
            _workspace = workspace;
            _editorconfigPath = editorconfigPath;
        }

        public async Task<bool> QueueUpdateAsync(TOption setting, TValue value)
        {
            using (await _guard.DisposableWaitAsync().ConfigureAwait(false))
            {
                _queue.Add((setting, value));
            }

            return true;
        }

        public async Task<IReadOnlyList<TextChange>?> GetChangedEditorConfigAsync(CancellationToken token)
        {
            var solution = _workspace.CurrentSolution;
            var analyzerConfigDocument = solution.Projects
                .SelectMany(p => p.AnalyzerConfigDocuments)
                .FirstOrDefault(d => d.FilePath == _editorconfigPath);

            if (analyzerConfigDocument is null)
                return null;

            var originalText = await analyzerConfigDocument.GetTextAsync(token).ConfigureAwait(false);
            using (await _guard.DisposableWaitAsync(token).ConfigureAwait(false))
            {
                var newText = await GetNewTextAsync(analyzerConfigDocument, _queue, token).ConfigureAwait(false);
                if (newText is null || newText.Equals(originalText))
                {
                    _queue.Clear();
                    return null;
                }
                else
                {
                    _queue.Clear();
                    return newText.GetTextChanges(originalText);
                }
            }
        }
    }
}
