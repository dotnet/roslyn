// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Utilities;
using static Microsoft.CodeAnalysis.ProjectState;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider
{
    internal abstract class SettingsProviderBase<TData, TOptionsUpdater, TOption, TValue> : ISettingsProvider<TData>
        where TOptionsUpdater : ISettingUpdater<TOption, TValue>
    {
        private readonly List<TData> _snapshot = new();
        private static readonly object s_gate = new();
        private ISettingsEditorViewModel? _viewModel;
        protected readonly string FileName;
        protected readonly TOptionsUpdater SettingsUpdater;
        protected readonly Workspace Workspace;

        protected abstract void UpdateOptions(AnalyzerConfigOptions editorConfigOptions, OptionSet visualStudioOptions);

        protected SettingsProviderBase(string fileName, TOptionsUpdater settingsUpdater, Workspace workspace)
        {
            FileName = fileName;
            SettingsUpdater = settingsUpdater;
            Workspace = workspace;
        }

        protected void Update()
        {
            var givenFolder = new DirectoryInfo(FileName).Parent;
            var solution = Workspace.CurrentSolution;
            var projects = solution.GetProjectsUnderEditorConfigFile(FileName);
            var project = projects.FirstOrDefault();
            if (project is null)
            {
                // no .NET projects in the solution
                return;
            }

            var configOptionsProvider = new ProjectAnalyzerConfigOptionsProvider(project.State);
            var workspaceOptions = configOptionsProvider.GetOptionsForSourcePath(givenFolder.FullName);
            var result = project.GetAnalyzerConfigOptions();
            var options = new CombinedAnalyzerConfigOptions(workspaceOptions, result);
            UpdateOptions(options, Workspace.Options);
        }

        public async Task<SourceText> GetChangedEditorConfigAsync(SourceText sourceText)
        {
            if (!await SettingsUpdater.HasAnyChangesAsync().ConfigureAwait(false))
            {
                return sourceText;
            }

            var text = await SettingsUpdater.GetChangedEditorConfigAsync(sourceText, default).ConfigureAwait(false);
            return text is not null ? text : sourceText;
        }

        public ImmutableArray<TData> GetCurrentDataSnapshot()
        {
            lock (s_gate)
            {
                return _snapshot.ToImmutableArray();
            }
        }

        protected void AddRange(IEnumerable<TData> items)
        {
            lock (s_gate)
            {
                _snapshot.AddRange(items);
            }

            _viewModel?.NotifyOfUpdate();
        }

        public void RegisterViewModel(ISettingsEditorViewModel viewModel)
            => _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        private sealed class CombinedAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private readonly AnalyzerConfigOptions _workspaceOptions;
            private readonly AnalyzerConfigOptionsResult? _result;

            public CombinedAnalyzerConfigOptions(AnalyzerConfigOptions workspaceOptions, AnalyzerConfigOptionsResult? result)
            {
                _workspaceOptions = workspaceOptions;
                _result = result;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                if (_workspaceOptions.TryGetValue(key, out value))
                {
                    return true;
                }

                if (!_result.HasValue)
                {
                    value = null;
                    return false;
                }

                if (_result.Value.AnalyzerOptions.TryGetValue(key, out value))
                {
                    return true;
                }

                var diagnosticKey = "dotnet_diagnostic.(?<key>.*).severity";
                var match = Regex.Match(key, diagnosticKey);
                if (match.Success && match.Groups["key"].Value is string isolatedKey &&
                    _result.Value.TreeOptions.TryGetValue(isolatedKey, out var severity))
                {
                    value = severity.ToEditorConfigString();
                    return true;
                }

                value = null;
                return false;
            }
        }
    }
}
