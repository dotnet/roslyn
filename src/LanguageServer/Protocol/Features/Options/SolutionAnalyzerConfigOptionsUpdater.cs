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
[ExportEventListener(WellKnownEventListeners.Workspace,
    [WorkspaceKind.Host, WorkspaceKind.Interactive, WorkspaceKind.SemanticSearch, WorkspaceKind.MetadataAsSource, WorkspaceKind.MiscellaneousFiles, WorkspaceKind.Debugger, WorkspaceKind.Preview]), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SolutionAnalyzerConfigOptionsUpdater(IGlobalOptionService globalOptions) : IEventListener<object>, IEventListenerStoppable
{
    public void StartListening(Workspace workspace, object serviceOpt)
        => globalOptions.AddOptionChangedHandler(workspace, GlobalOptionsChanged);

    public void StopListening(Workspace workspace)
        => globalOptions.RemoveOptionChangedHandler(workspace, GlobalOptionsChanged);

    private void GlobalOptionsChanged(object sender, object target, OptionChangedEventArgs args)
    {
        Debug.Assert(target is Workspace);

        try
        {
            if (!args.ChangedOptions.Any(static o => o.key.Option.Definition.IsEditorConfigOption))
            {
                return;
            }

            _ = ((Workspace)target).SetCurrentSolution(UpdateOptions, changeKind: WorkspaceChangeKind.SolutionChanged);

            Solution UpdateOptions(Solution oldSolution)
            {
                var oldFallbackOptions = oldSolution.FallbackAnalyzerOptions;
                var newFallbackOptions = oldFallbackOptions;

                foreach (var (language, languageOptions) in oldFallbackOptions)
                {
                    ImmutableDictionary<string, string>.Builder? lazyBuilder = null;

                    foreach (var (key, value) in args.ChangedOptions)
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

                return oldSolution.WithFallbackAnalyzerOptions(newFallbackOptions);
            }
        }
        catch (Exception e) when (FatalError.ReportAndPropagate(e, ErrorSeverity.Diagnostic))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
