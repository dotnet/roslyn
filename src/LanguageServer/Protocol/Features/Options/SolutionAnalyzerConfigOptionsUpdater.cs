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
/// Whenever editorconfig options stored in <see cref="IGlobalOptionService"/> change we apply these changes to all registered workspaces of given kinds,
/// such that the latest solution snapshot of each workspace includes the latest snapshot of editorconfig options for all languages present in the solution.
/// </summary>
[Export]
[ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host, WorkspaceKind.Interactive, WorkspaceKind.SemanticSearch), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class SolutionAnalyzerConfigOptionsUpdater(
    EditorConfigOptionsEnumerator optionsEnumerator,
    IGlobalOptionService globalOptions) : IEventListener<object>, IEventListenerStoppable
{
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
