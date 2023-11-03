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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Extensions;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
        public readonly IGlobalOptionService GlobalOptions;

        protected abstract void UpdateOptions(TieredAnalyzerConfigOptions options, ImmutableArray<Project> projectsInScope);

        protected SettingsProviderBase(string fileName, TOptionsUpdater settingsUpdater, Workspace workspace, IGlobalOptionService globalOptions)
        {
            FileName = fileName;
            SettingsUpdater = settingsUpdater;
            Workspace = workspace;
            GlobalOptions = globalOptions;
        }

        protected void Update()
        {
            var givenFolder = new DirectoryInfo(FileName).Parent;
            if (givenFolder is null)
            {
                return;
            }

            var solution = Workspace.CurrentSolution;
            var projects = solution.GetProjectsUnderEditorConfigFile(FileName);
            var project = projects.FirstOrDefault();
            if (project is null)
            {
                // no .NET projects in the solution
                return;
            }

            var configFileDirectoryOptions = project.State.GetAnalyzerOptionsForPath(givenFolder.FullName, CancellationToken.None);
            var projectDirectoryOptions = project.GetAnalyzerConfigOptions();

            // TODO: Support for multiple languages https://github.com/dotnet/roslyn/issues/65859
            var options = new TieredAnalyzerConfigOptions(
                new CombinedAnalyzerConfigOptions(configFileDirectoryOptions, projectDirectoryOptions),
                GlobalOptions,
                language: LanguageNames.CSharp,
                editorConfigFileName: FileName);

            UpdateOptions(options, projects);
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

        private sealed class CombinedAnalyzerConfigOptions(AnalyzerConfigData fileDirectoryConfigData, AnalyzerConfigData? projectDirectoryConfigData) : StructuredAnalyzerConfigOptions
        {
            private readonly AnalyzerConfigData _fileDirectoryConfigData = fileDirectoryConfigData;
            private readonly AnalyzerConfigData? _projectDirectoryConfigData = projectDirectoryConfigData;

            public override NamingStylePreferences GetNamingStylePreferences()
            {
                var preferences = _fileDirectoryConfigData.ConfigOptions.GetNamingStylePreferences();
                if (preferences.IsEmpty && _projectDirectoryConfigData.HasValue)
                {
                    preferences = _projectDirectoryConfigData.Value.ConfigOptions.GetNamingStylePreferences();
                }

                return preferences;
            }

            public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            {
                if (_fileDirectoryConfigData.ConfigOptions.TryGetValue(key, out value))
                {
                    return true;
                }

                if (!_projectDirectoryConfigData.HasValue)
                {
                    value = null;
                    return false;
                }

                if (_projectDirectoryConfigData.Value.AnalyzerOptions.TryGetValue(key, out value))
                {
                    return true;
                }

                var diagnosticKey = "dotnet_diagnostic.(?<key>.*).severity";
                var match = Regex.Match(key, diagnosticKey);
                if (match.Success && match.Groups["key"].Value is string isolatedKey &&
                    _projectDirectoryConfigData.Value.TreeOptions.TryGetValue(isolatedKey, out var severity))
                {
                    value = severity.ToEditorConfigString();
                    return true;
                }

                value = null;
                return false;
            }

            public override IEnumerable<string> Keys
            {
                get
                {
                    foreach (var key in _fileDirectoryConfigData.ConfigOptions.Keys)
                        yield return key;

                    if (!_projectDirectoryConfigData.HasValue)
                        yield break;

                    foreach (var key in _projectDirectoryConfigData.Value.AnalyzerOptions.Keys)
                    {
                        if (!_fileDirectoryConfigData.ConfigOptions.TryGetValue(key, out _))
                            yield return key;
                    }

                    foreach (var (key, severity) in _projectDirectoryConfigData.Value.TreeOptions)
                    {
                        var diagnosticKey = "dotnet_diagnostic." + key + ".severity";
                        if (!_fileDirectoryConfigData.ConfigOptions.TryGetValue(diagnosticKey, out _) &&
                            !_projectDirectoryConfigData.Value.AnalyzerOptions.TryGetKey(diagnosticKey, out _))
                        {
                            yield return diagnosticKey;
                        }
                    }
                }
            }
        }
    }
}
