﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings
{
    internal partial class SettingsAggregator : ISettingsAggregator
    {
        private readonly Workspace _workspace;
        private readonly ISettingsProviderFactory<AnalyzerSetting> _analyzerProvider;
        private ISettingsProviderFactory<FormattingSetting> _formattingProvider;
        private ISettingsProviderFactory<CodeStyleSetting> _codeStyleProvider;

        public SettingsAggregator(Workspace workspace)
        {
            _workspace = workspace;
            _workspace.WorkspaceChanged += UpdateProviders;
            _formattingProvider = GetOptionsProviderFactory<FormattingSetting>(_workspace);
            _codeStyleProvider = GetOptionsProviderFactory<CodeStyleSetting>(_workspace);
            _analyzerProvider = GetOptionsProviderFactory<AnalyzerSetting>(_workspace);
        }

        private void UpdateProviders(object? sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionChanged:
                case WorkspaceChangeKind.SolutionAdded:
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionReloaded:
                case WorkspaceChangeKind.ProjectAdded:
                case WorkspaceChangeKind.ProjectRemoved:
                case WorkspaceChangeKind.ProjectChanged:
                    _formattingProvider = GetOptionsProviderFactory<FormattingSetting>(_workspace);
                    _codeStyleProvider = GetOptionsProviderFactory<CodeStyleSetting>(_workspace);
                    break;
                default:
                    break;
            }
        }

        public ISettingsProvider<TData>? GetSettingsProvider<TData>(string fileName)
        {
            if (typeof(TData) == typeof(AnalyzerSetting))
            {
                return (ISettingsProvider<TData>)_analyzerProvider.GetForFile(fileName);
            }

            if (typeof(TData) == typeof(FormattingSetting))
            {
                return (ISettingsProvider<TData>)_formattingProvider.GetForFile(fileName);
            }

            if (typeof(TData) == typeof(CodeStyleSetting))
            {
                return (ISettingsProvider<TData>)_codeStyleProvider.GetForFile(fileName);
            }

            return null;
        }

        private static ISettingsProviderFactory<T> GetOptionsProviderFactory<T>(Workspace workspace)
        {
            var providers = new List<ISettingsProviderFactory<T>>();
            var commonProvider = workspace.Services.GetRequiredService<IWorkspaceSettingsProviderFactory<T>>();
            providers.Add(commonProvider);
            var solution = workspace.CurrentSolution;
            var supportsCSharp = solution.Projects.Any(p => p.Language.Equals(LanguageNames.CSharp, StringComparison.OrdinalIgnoreCase));
            var supportsVisualBasic = solution.Projects.Any(p => p.Language.Equals(LanguageNames.VisualBasic, StringComparison.OrdinalIgnoreCase));
            if (supportsCSharp)
            {
                TryAddProviderForLanguage(LanguageNames.CSharp, workspace, providers);
            }

            if (supportsVisualBasic)
            {
                TryAddProviderForLanguage(LanguageNames.VisualBasic, workspace, providers);
            }

            return new CombinedOptionsProviderFactory<T>(providers.ToImmutableArray());

            static void TryAddProviderForLanguage(string language, Workspace workspace, List<ISettingsProviderFactory<T>> providers)
            {
                var provider = workspace.Services.GetLanguageServices(language).GetService<ILanguageSettingsProviderFactory<T>>();
                if (provider is not null)
                {
                    providers.Add(provider);
                }
            }
        }
    }
}
