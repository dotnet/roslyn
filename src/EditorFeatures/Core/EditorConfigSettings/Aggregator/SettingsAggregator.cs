// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings;

internal partial class SettingsAggregator : ISettingsAggregator
{
    private readonly Workspace _workspace;
    private readonly ISettingsProviderFactory<AnalyzerSetting> _analyzerProvider;
    private ISettingsProviderFactory<Setting> _whitespaceProvider;
    private ISettingsProviderFactory<NamingStyleSetting> _namingStyleProvider;
    private ISettingsProviderFactory<CodeStyleSetting> _codeStyleProvider;

    public SettingsAggregator(Workspace workspace)
    {
        _workspace = workspace;
        _workspace.WorkspaceChanged += UpdateProviders;
        _whitespaceProvider = GetOptionsProviderFactory<Setting>(_workspace);
        _codeStyleProvider = GetOptionsProviderFactory<CodeStyleSetting>(_workspace);
        _namingStyleProvider = GetOptionsProviderFactory<NamingStyleSetting>(_workspace);
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
                _whitespaceProvider = GetOptionsProviderFactory<Setting>(_workspace);
                _codeStyleProvider = GetOptionsProviderFactory<CodeStyleSetting>(_workspace);
                _namingStyleProvider = GetOptionsProviderFactory<NamingStyleSetting>(_workspace);
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

        if (typeof(TData) == typeof(Setting))
        {
            return (ISettingsProvider<TData>)_whitespaceProvider.GetForFile(fileName);
        }

        if (typeof(TData) == typeof(NamingStyleSetting))
        {
            return (ISettingsProvider<TData>)_namingStyleProvider.GetForFile(fileName);
        }

        if (typeof(TData) == typeof(CodeStyleSetting))
        {
            return (ISettingsProvider<TData>)_codeStyleProvider.GetForFile(fileName);
        }

        return null;
    }

    private static ISettingsProviderFactory<T> GetOptionsProviderFactory<T>(Workspace workspace)
    {
        using var providers = TemporaryArray<ISettingsProviderFactory<T>>.Empty;

        var commonProvider = workspace.Services.GetRequiredService<IWorkspaceSettingsProviderFactory<T>>();
        providers.Add(commonProvider);

        var projectCountByLanguage = workspace.CurrentSolution.SolutionState.ProjectCountByLanguage;

        TryAddProviderForLanguage(LanguageNames.CSharp);
        TryAddProviderForLanguage(LanguageNames.VisualBasic);

        return new CombinedOptionsProviderFactory<T>(providers.ToImmutableAndClear());

        void TryAddProviderForLanguage(string language)
        {
            if (projectCountByLanguage.ContainsKey(language))
            {
                var provider = workspace.Services.GetLanguageServices(language).GetService<ILanguageSettingsProviderFactory<T>>();
                if (provider != null)
                    providers.Add(provider);
            }
        }
    }
}
