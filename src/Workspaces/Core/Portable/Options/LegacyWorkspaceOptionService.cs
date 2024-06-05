// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable LAYERING_IGlobalOptionService

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Options;

[Export(typeof(ILegacyGlobalOptionService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LegacyGlobalOptionService(IGlobalOptionService globalOptionService) : ILegacyGlobalOptionService
{
    [ExportWorkspaceService(typeof(ILegacyWorkspaceOptionService)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class WorkspaceService(ILegacyGlobalOptionService legacyGlobalOptions) : ILegacyWorkspaceOptionService
    {
        public ILegacyGlobalOptionService LegacyGlobalOptions { get; } = legacyGlobalOptions;
    }

    public IGlobalOptionService GlobalOptions { get; } = globalOptionService;

    // access is interlocked
    private ImmutableArray<WeakReference<Workspace>> _registeredWorkspaces = [];

    /// <summary>
    /// Stores options that are not defined by Roslyn and do not implement <see cref="IOption2"/>.
    /// </summary>
    private ImmutableDictionary<OptionKey, object?> _currentExternallyDefinedOptionValues = ImmutableDictionary.Create<OptionKey, object?>();

    public object? GetExternallyDefinedOption(OptionKey key)
    {
        Debug.Assert(key.Option is not IOption2);
        return _currentExternallyDefinedOptionValues.TryGetValue(key, out var value) ? value : key.Option.DefaultValue;
    }

    /// <summary>
    /// Sets values of options that may be stored in <see cref="Solution.Options"/> (public options).
    /// Clears <see cref="LegacySolutionOptionSet"/> of registered workspaces so that next time
    /// <see cref="Solution.Options"/> are queried for the options new values are fetched from 
    /// <see cref="GlobalOptionService"/>.
    /// </summary>
    public void SetOptions(
        ImmutableArray<KeyValuePair<OptionKey2, object?>> internallyDefinedOptions,
        ImmutableArray<KeyValuePair<OptionKey, object?>> externallyDefinedOptions)
    {
        // all values in internally defined options have internal representation:
        Debug.Assert(internallyDefinedOptions.All(entry => OptionSet.IsInternalOptionValue(entry.Value)));

        var anyExternallyDefinedOptionChanged = false;
        foreach (var (optionKey, value) in externallyDefinedOptions)
        {
            if (Equals(value, GetExternallyDefinedOption(optionKey)))
            {
                continue;
            }

            anyExternallyDefinedOptionChanged = true;

            ImmutableInterlocked.Update(
                ref _currentExternallyDefinedOptionValues,
                static (options, arg) => options.SetItem(arg.optionKey, arg.value),
                (optionKey, value));
        }

        // Update workspaces even when value of public internally defined options have not actually changed.
        // This is necessary since these options may have been changed previously directly via IGlobalOptionService,
        // without updating the workspaces and thus the values stored in IGlobalOptionService may not match the values
        // stored on current solution snapshots.
        // 
        // Updating workspaces more often than strictly needed is not a functional issue -
        // it's just adding a bit of extra overhead since the options need to be re-read from global options.
        if (!internallyDefinedOptions.IsEmpty || anyExternallyDefinedOptionChanged)
        {
            UpdateRegisteredWorkspaces();
        }

#pragma warning disable RS0030 // Do not use banned APIs (IGlobalOptionService)
        // Update global options after updating registered workspaces,
        // so that the handler of the changed event has access to the updated values through the current solution.
        GlobalOptions.SetGlobalOptions(internallyDefinedOptions);
#pragma warning restore
    }

    public void UpdateRegisteredWorkspaces()
    {
        // Ensure that the Workspace's CurrentSolution snapshot is updated with new options for all registered workspaces
        // prior to raising option changed event handlers.
        foreach (var weakWorkspace in _registeredWorkspaces)
        {
            if (!weakWorkspace.TryGetTarget(out var workspace))
                continue;

            workspace.UpdateCurrentSolutionOnOptionsChanged();
        }
    }

    public void RegisterWorkspace(Workspace workspace)
    {
        ImmutableInterlocked.Update(
            ref _registeredWorkspaces,
            static (workspaces, workspace) =>
            {
                return workspaces
                    .RemoveAll(static weakWorkspace => !weakWorkspace.TryGetTarget(out _))
                    .Add(new WeakReference<Workspace>(workspace));
            },
            workspace);
    }

    public void UnregisterWorkspace(Workspace workspace)
    {
        ImmutableInterlocked.Update(
            ref _registeredWorkspaces,
            static (workspaces, workspace) =>
            {
                return workspaces.WhereAsArray(
                    static (weakWorkspace, workspaceToRemove) => weakWorkspace.TryGetTarget(out var workspace) && workspace != workspaceToRemove,
                    workspace);
            },
            workspace);
    }
}
