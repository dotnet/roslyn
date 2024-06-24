// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

internal sealed partial class SolutionAnalyzerConfigOptionsUpdater
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

                var newSolution = UpdateSolution(transformation: solution => InitializeLanguages(solution, _initializedLanguages));
                _initializedLanguages = newSolution.SolutionState.ProjectCountByLanguage;
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

                _ = UpdateSolution(transformation: solution => UpdateOptions(solution, args, _initializedLanguages));
            }
            catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.Diagnostic))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private Solution UpdateSolution(Func<Solution, Solution> transformation)
        {
            var lockTaken = false;
            try
            {
                // If another update is in progress wait until it completes.
                Monitor.Enter(_updateLock, ref lockTaken);

                var (_, newSolution) = workspace.SetCurrentSolution(
                    transformation,
                    changeKind: (_, _) => (WorkspaceChangeKind.SolutionChanged, projectId: null, documentId: null),
                    onAfterUpdate: (_, newSolution) =>
                    {
                        // unlock before workspace events are triggered:
                        if (lockTaken)
                        {
                            Monitor.Exit(_updateLock);
                            lockTaken = false;
                        }
                    });

                return newSolution;
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
}
