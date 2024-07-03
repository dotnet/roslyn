// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.DataProvider;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings;

internal partial class SettingsAggregator : ISettingsAggregator
{
    private readonly Workspace _workspace;
    private readonly ISettingsProviderFactory<AnalyzerSetting> _analyzerProvider;
    private readonly AsyncBatchingWorkQueue _workQueue;

    private ISettingsProviderFactory<Setting> _whitespaceProvider;
    private ISettingsProviderFactory<NamingStyleSetting> _namingStyleProvider;
    private ISettingsProviderFactory<CodeStyleSetting> _codeStyleProvider;

    public SettingsAggregator(
        Workspace workspace,
        IThreadingContext threadingContext,
        IAsynchronousOperationListener listener)
    {
        _workspace = workspace;
        _workspace.WorkspaceChanged += UpdateProviders;

        // 
        var currentSolution = _workspace.CurrentSolution.SolutionState;
        UpdateProviders(currentSolution);

        // TODO(cyrusn): Why do we not update this as well inside UpdateProviders when we hear about a workspace event?
        _analyzerProvider = GetOptionsProviderFactory<AnalyzerSetting>(currentSolution);

        // Batch these up so that we don't do a lot of expensive work when hearing a flurry of workspace events.
        _workQueue = new AsyncBatchingWorkQueue(
            TimeSpan.FromSeconds(1),
            UpdateProvidersAsync,
            listener,
            threadingContext.DisposalToken);
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
                _workQueue.AddWork();
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

    private ValueTask UpdateProvidersAsync(CancellationToken cancellationToken)
    {
        UpdateProviders(_workspace.CurrentSolution.SolutionState);
        return ValueTaskFactory.CompletedTask;
    }

    [MemberNotNull(nameof(_whitespaceProvider))]
    [MemberNotNull(nameof(_codeStyleProvider))]
    [MemberNotNull(nameof(_namingStyleProvider))]
    private void UpdateProviders(SolutionState solution)
    {
        _whitespaceProvider = GetOptionsProviderFactory<Setting>(solution);
        _codeStyleProvider = GetOptionsProviderFactory<CodeStyleSetting>(solution);
        _namingStyleProvider = GetOptionsProviderFactory<NamingStyleSetting>(solution);
    }

    private static ISettingsProviderFactory<T> GetOptionsProviderFactory<T>(SolutionState solution)
    {
        using var providers = TemporaryArray<ISettingsProviderFactory<T>>.Empty;

        var commonProvider = solution.Services.GetRequiredService<IWorkspaceSettingsProviderFactory<T>>();
        providers.Add(commonProvider);

        var projectCountByLanguage = solution.ProjectCountByLanguage;

        TryAddProviderForLanguage(LanguageNames.CSharp);
        TryAddProviderForLanguage(LanguageNames.VisualBasic);

        return new CombinedOptionsProviderFactory<T>(providers.ToImmutableAndClear());

        void TryAddProviderForLanguage(string language)
        {
            if (projectCountByLanguage.ContainsKey(language))
            {
                var provider = solution.Services.GetLanguageServices(language).GetService<ILanguageSettingsProviderFactory<T>>();
                if (provider != null)
                    providers.Add(provider);
            }
        }
    }
}
