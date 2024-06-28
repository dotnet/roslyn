﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

/// <summary>
/// Keeps <see cref="SolutionState.FallbackAnalyzerOptions"/> up-to-date with global option values maintained by <see cref="IGlobalOptionService"/>.
/// </summary>
[Export]
[ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host, WorkspaceKind.Interactive, WorkspaceKind.SemanticSearch), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SolutionAnalyzerConfigOptionsUpdater(
    EditorConfigOptionsEnumerator optionsEnumerator,
    IGlobalOptionService globalOptions) : IEventListener<object>, IEventListenerStoppable
{
    /// <summary>
    /// We need to update the current solution snapshot based on two sources of events:
    /// 1) global options service (global option changed)
    /// 2) the workspace (project added)
    /// 
    /// To keep the set of analyzer fallback options on the solution snapshot consistent and not miss any option 
    /// we serialize the updates using a lock.
    /// </summary>
    private sealed class WorkspaceUpdater(Workspace workspace, EditorConfigOptionsEnumerator optionsEnumerator, IGlobalOptionService globalOptions)
    {
        // Set of languages (keys of the dictionary) in the current snapshot that have their fallback options initialized from global options.
        // The values are ignored.
        private ImmutableDictionary<string, int> _initializedLanguages = ImmutableDictionary<string, int>.Empty;

        // Ensures we only process one update at a time:
        private readonly object _updateLock = new();

        public void WorkspaceChanged(object? sender, WorkspaceChangeEventArgs args)
        {
            try
            {
                switch (args.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                    case WorkspaceChangeKind.SolutionChanged:
                    case WorkspaceChangeKind.SolutionCleared:
                    case WorkspaceChangeKind.SolutionReloaded:
                    case WorkspaceChangeKind.SolutionRemoved:
                    case WorkspaceChangeKind.ProjectAdded:
                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                    case WorkspaceChangeKind.ProjectRemoved:
                        break;

                    default:
                        return;
                }

                if (args.OldSolution.SolutionState.ProjectCountByLanguage.KeysEqual(args.NewSolution.SolutionState.ProjectCountByLanguage))
                {
                    return;
                }

                UpdateSolution(
                    transformation: solution => InitializeLanguages(solution, _initializedLanguages),
                    onAfterUpdate: solution => _initializedLanguages = solution.SolutionState.ProjectCountByLanguage);
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.Diagnostic))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        public void GlobalOptionsChanged(object? sender, OptionChangedEventArgs args)
        {
            try
            {
                if (!args.ChangedOptions.Any(static o => o.key.Option.Definition.IsEditorConfigOption))
                {
                    return;
                }

                UpdateSolution(
                    transformation: solution => UpdateOptions(solution, args, _initializedLanguages),
                    onAfterUpdate: null);
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.Diagnostic))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private void UpdateSolution(Func<Solution, Solution> transformation, Action<Solution>? onAfterUpdate)
        {
            var lockTaken = false;
            try
            {
                // If another update is in progress wait until it completes.
                Monitor.Enter(_updateLock, ref lockTaken);

                workspace.SetCurrentSolution(
                    transformation,
                    changeKind: WorkspaceChangeKind.SolutionChanged,
                    onAfterUpdate: (_, newSolution) =>
                    {
                        onAfterUpdate?.Invoke(newSolution);

                        // unlock before workspace events are triggered:
                        if (lockTaken)
                        {
                            Monitor.Exit(_updateLock);
                            lockTaken = false;
                        }
                    });
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(_updateLock);
                }
            }
        }

        private Solution InitializeLanguages(Solution solution, ImmutableDictionary<string, int> initializedLanguages)
        {
            var oldFallbackOptions = solution.FallbackAnalyzerOptions;
            var newFallbackOptions = oldFallbackOptions;

            // Clear out languages that are no longer present in the solution.
            // If we didn't, the workspace might clear the solution (which removes the fallback options)
            // and we would never re-initialize them from global options.
            foreach (var (language, _) in initializedLanguages)
            {
                if (!solution.SolutionState.ProjectCountByLanguage.ContainsKey(language))
                {
                    newFallbackOptions = newFallbackOptions.Remove(language);
                }
            }

            // Update solution snapshot to include options for newly added languages:
            foreach (var (language, _) in solution.SolutionState.ProjectCountByLanguage)
            {
                if (initializedLanguages.ContainsKey(language))
                {
                    continue;
                }

                if (newFallbackOptions.ContainsKey(language))
                {
                    continue;
                }

                var builder = ImmutableDictionary.CreateBuilder<string, string>(AnalyzerConfigOptions.KeyComparer);

                var optionDefinitions = optionsEnumerator.GetOptions(language);

                foreach (var (_, options) in optionDefinitions)
                {
                    foreach (var option in options)
                    {
                        var value = globalOptions.GetOption<object>(new OptionKey2(option, option.IsPerLanguage ? language : null));

                        var configName = option.Definition.ConfigName;
                        var configValue = option.Definition.Serializer.Serialize(value);

                        builder.Add(configName, configValue);
                    }
                }

                newFallbackOptions = newFallbackOptions.Add(
                    language,
                    StructuredAnalyzerConfigOptions.Create(new DictionaryAnalyzerConfigOptions(builder.ToImmutable())));
            }

            return solution.WithFallbackAnalyzerOptions(newFallbackOptions);
        }

        private static Solution UpdateOptions(Solution solution, OptionChangedEventArgs optionsUpdate, ImmutableDictionary<string, int> initializedLanguages)
        {
            var oldFallbackOptions = solution.FallbackAnalyzerOptions;
            var newFallbackOptions = oldFallbackOptions;

            foreach (var (language, languageOptions) in oldFallbackOptions)
            {
                // Only update options for already initialized languages:
                if (!initializedLanguages.ContainsKey(language))
                {
                    continue;
                }

                ImmutableDictionary<string, string>.Builder? lazyBuilder = null;

                foreach (var (key, value) in optionsUpdate.ChangedOptions)
                {
                    if (!key.Option.Definition.IsEditorConfigOption)
                    {
                        continue;
                    }

                    if (key.Language != null && key.Language != language)
                    {
                        continue;
                    }

                    lazyBuilder ??= ImmutableDictionary.CreateBuilder<string, string>(AnalyzerConfigOptions.KeyComparer);

                    // copy existing option values:
                    foreach (var oldKey in languageOptions.Keys)
                    {
                        if (languageOptions.TryGetValue(oldKey, out var oldValue))
                        {
                            lazyBuilder.Add(oldKey, oldValue);
                        }
                    }

                    // update changed values:
                    var configName = key.Option.Definition.ConfigName;
                    var configValue = key.Option.Definition.Serializer.Serialize(value);
                    lazyBuilder[configName] = configValue;
                }

                if (lazyBuilder != null)
                {
                    newFallbackOptions = newFallbackOptions.SetItem(
                        language,
                        StructuredAnalyzerConfigOptions.Create(new DictionaryAnalyzerConfigOptions(lazyBuilder.ToImmutable())));
                }
            }

            return solution.WithFallbackAnalyzerOptions(newFallbackOptions);
        }
    }

    private ImmutableDictionary<Workspace, WorkspaceUpdater> _workspaceUpdaters = ImmutableDictionary<Workspace, WorkspaceUpdater>.Empty;

    public void StartListening(Workspace workspace, object serviceOpt)
    {
        var updater = new WorkspaceUpdater(workspace, optionsEnumerator, globalOptions);
        Contract.ThrowIfFalse(ImmutableInterlocked.TryAdd(ref _workspaceUpdaters, workspace, updater));

        globalOptions.AddOptionChangedHandler(workspace, updater.GlobalOptionsChanged);
        workspace.WorkspaceChanged += updater.WorkspaceChanged;
    }

    public void StopListening(Workspace workspace)
    {
        Contract.ThrowIfFalse(ImmutableInterlocked.TryRemove(ref _workspaceUpdaters, workspace, out var updater));

        globalOptions.RemoveOptionChangedHandler(workspace, updater.GlobalOptionsChanged);
        workspace.WorkspaceChanged -= updater.WorkspaceChanged;
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(SolutionAnalyzerConfigOptionsUpdater instance)
    {
        internal bool HasWorkspaceUpdaters => !instance._workspaceUpdaters.IsEmpty;
    }
}
