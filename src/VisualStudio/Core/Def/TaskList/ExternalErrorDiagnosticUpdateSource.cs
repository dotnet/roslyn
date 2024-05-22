// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.RpcContracts.DiagnosticManagement;
using Microsoft.VisualStudio.RpcContracts.Utilities;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Roslyn.Utilities;
using static Microsoft.ServiceHub.Framework.ServiceBrokerClient;

#pragma warning disable CA1200 // Avoid using cref tags with a prefix

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
/// <summary>
/// Diagnostic source for warnings and errors reported from explicit build command invocations in Visual Studio.
/// VS workspaces calls into us when a build is invoked or completed in Visual Studio.
/// <see cref="ProjectExternalErrorReporter"/> calls into us to clear reported diagnostics or to report new diagnostics during the build.
/// For each of these callbacks, we create/capture the current <see cref="GetBuildInProgressState()"/> and
/// schedule updating/processing this state on a serialized <see cref="_taskQueue"/> in the background.
/// </summary>
[Export(typeof(ExternalErrorDiagnosticUpdateSource))]
internal sealed class ExternalErrorDiagnosticUpdateSource : IDisposable
{
    private readonly Workspace _workspace;
    private readonly IDiagnosticAnalyzerService _diagnosticService;
    private readonly IAsynchronousOperationListener _listener;
    private readonly CancellationToken _disposalToken;
    private readonly IServiceBroker _serviceBroker;

    /// <summary>
    /// Task queue to serialize all the work for errors reported by build.
    /// <see cref="_stateDoNotAccessDirectly"/> represents the state from build errors,
    /// which is built up and processed in serialized fashion on this task queue.
    /// </summary>
    private readonly AsyncBatchingWorkQueue<Func<CancellationToken, Task>> _taskQueue;

    /// <summary>
    /// Holds onto the diagnostic manager service as long as this is alive.
    /// This is important as when the manager service is disposed, the VS client will clear diagnostics from it.
    /// Serial access is guaranteed by the <see cref="_taskQueue"/>
    /// </summary>
    private IDiagnosticManagerService? _diagnosticManagerService;

    // Gate for concurrent access and fields guarded with this gate.
    private readonly object _gate = new();
    private InProgressState? _stateDoNotAccessDirectly;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ExternalErrorDiagnosticUpdateSource(
        VisualStudioWorkspace workspace,
        IDiagnosticAnalyzerService diagnosticService,
        IAsynchronousOperationListenerProvider listenerProvider,
        [Import(typeof(SVsFullAccessServiceBroker))] IServiceBroker serviceBroker,
        IThreadingContext threadingContext)
    {
        _disposalToken = threadingContext.DisposalToken;
        _workspace = workspace;
        _diagnosticService = diagnosticService;
        _listener = listenerProvider.GetListener(FeatureAttribute.ErrorList);

        _serviceBroker = serviceBroker;
        _taskQueue = new AsyncBatchingWorkQueue<Func<CancellationToken, Task>>(
            TimeSpan.Zero,
            processBatchAsync: ProcessTaskQueueItemsAsync,
            _listener,
            _disposalToken
        );
    }

    private async ValueTask ProcessTaskQueueItemsAsync(ImmutableSegmentedList<Func<CancellationToken, Task>> list, CancellationToken cancellationToken)
    {
        foreach (var workItem in list)
            await workItem(cancellationToken).ConfigureAwait(false);
    }

    public DiagnosticAnalyzerInfoCache AnalyzerInfoCache => _diagnosticService.AnalyzerInfoCache;

    public void Dispose()
    {
        lock (_gate)
        {
            // Only called when the MEF catalog is disposed on shutdown.
            _diagnosticManagerService?.Dispose();
        }
    }

    /// <summary>
    /// Returns true if the given <paramref name="id"/> represents an analyzer diagnostic ID that could be reported
    /// for the given <paramref name="projectId"/> during the current build in progress.
    /// This API is only intended to be invoked from <see cref="ProjectExternalErrorReporter"/> while a build is in progress.
    /// </summary>
    public bool IsSupportedDiagnosticId(ProjectId projectId, string id)
        => GetBuildInProgressState()?.IsSupportedDiagnosticId(projectId, id) ?? false;

    public void ClearErrors(ProjectId projectId)
    {
        // Clear the previous errors associated with the project.
        _taskQueue.AddWork(async cancellationToken =>
        {
            await ClearPreviousAsync(projectId: projectId, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Called serially in response to the sln build UI context.
    /// </summary>
    internal void OnSolutionBuildStarted()
    {
        _ = GetOrCreateInProgressState();

        _taskQueue.AddWork(async cancellationToken =>
        {
            await ClearPreviousAsync(projectId: null, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>
    /// Called serially in response to the sln build UI context completing.
    /// </summary>
    internal void OnSolutionBuildCompleted()
    {
        _ = ClearInProgressState();
    }

    public void AddNewErrors(ProjectId projectId, Guid projectHierarchyGuid, ImmutableArray<DiagnosticData> diagnostics)
    {
        Debug.Assert(diagnostics.All(d => d.IsBuildDiagnostic()));

        // Capture state that will be processed in background thread.
        var state = GetOrCreateInProgressState();

        _taskQueue.AddWork(async cancellationToken =>
        {
            await ProcessDiagnosticsReportAsync(projectId, projectHierarchyGuid, diagnostics, state, cancellationToken).ConfigureAwait(false);
        });
    }

    private async Task ClearPreviousAsync(ProjectId? projectId, CancellationToken cancellationToken)
    {
        var diagnosticManagerService = await GetOrCreateDiagnosticManagerAsync(cancellationToken).ConfigureAwait(false);

        if (projectId is not null)
        {
            await diagnosticManagerService.ClearDiagnosticsAsync(projectId.Id.ToString(), cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await diagnosticManagerService.ClearAllDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask ProcessDiagnosticsReportAsync(ProjectId projectId, Guid projectHierarchyGuid, ImmutableArray<DiagnosticData> diagnostics, InProgressState state, CancellationToken cancellationToken)
    {
        var diagnosticManagerService = await GetOrCreateDiagnosticManagerAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<DiagnosticCollection>.GetInstance(out var collections);
        // The client API asks us to pass in diagnostics grouped by the file they are in.
        // Note - linked file diagnostics will be 'duplicated' for each project - the document collection
        // will contain a separate diagnostic for each project the file is linked to (with the corresponding project field set).
        var groupedDiagnostics = diagnostics.GroupBy(d => d.DataLocation.UnmappedFileSpan.Path);
        foreach (var group in groupedDiagnostics)
        {
            var path = group.Key;
            var pathAsUri = ProtocolConversions.CreateAbsoluteUri(path);

            var convertedDiagnostics = group.Select(d => CreateDiagnostic(projectId, projectHierarchyGuid, d, state.Solution)).ToImmutableArray();
            if (convertedDiagnostics.Any())
            {
                var collection = new DiagnosticCollection(pathAsUri, documentVersionNumber: -1, diagnostics: convertedDiagnostics);
                collections.Add(collection);
            }
        }

        if (collections.Any())
        {
            // Report with projectId so we can clear individual project errors.
            await diagnosticManagerService.AppendDiagnosticsAsync(projectId.Id.ToString(), collections.ToImmutable(), cancellationToken).ConfigureAwait(false);
        }
    }

    private static Microsoft.VisualStudio.RpcContracts.DiagnosticManagement.Diagnostic? CreateDiagnostic(ProjectId projectId, Guid projectHierarchyGuid, DiagnosticData diagnostic, Solution solution)
    {
        var project = GetProjectIdentifier(solution.GetProject(projectId), projectHierarchyGuid);
        ImmutableArray<ProjectIdentifier> projects = project is not null ? [project.Value] : [];

        var range = GetRange(diagnostic);
        var description = string.IsNullOrEmpty(diagnostic.Description) ? null : diagnostic.Description;
        return new Microsoft.VisualStudio.RpcContracts.DiagnosticManagement.Diagnostic(
            message: diagnostic.Message ?? string.Empty,
            code: diagnostic.Id,
            severity: GetSeverity(diagnostic.Severity),
            range: GetRange(diagnostic),
            tags: RpcContracts.DiagnosticManagement.DiagnosticTags.BuildError,
            relatedInformation: null,
            expandedMessage: description,
            // Intentionally the same as diagnosticType, matches what we used to report.
            source: diagnostic.Category,
            helpLink: diagnostic.HelpLink,
            diagnosticType: diagnostic.Category,
            projects: projects,
            identifier: (diagnostic.Id, diagnostic.DataLocation.UnmappedFileSpan.Path, range, diagnostic.Message).GetHashCode().ToString(),
            outputId: null);
    }

    private static RpcContracts.Utilities.ProjectIdentifier? GetProjectIdentifier(Project? project, Guid projectHierarchyGuid)
    {
        if (project is null)
        {
            // It is possible (but unlikely) that the solution snapshot we saved at the start of the build
            // does not contain the projectId against which the build is reporting diagnostics due to the inherent race in invoking build.
            return null;
        }

        return new RpcContracts.Utilities.ProjectIdentifier(
            name: project.Name,
            identifier: projectHierarchyGuid.ToString());
    }

    private static RpcContracts.DiagnosticManagement.DiagnosticSeverity GetSeverity(CodeAnalysis.DiagnosticSeverity severity)
    {
        return severity switch
        {
            CodeAnalysis.DiagnosticSeverity.Hidden => RpcContracts.DiagnosticManagement.DiagnosticSeverity.Hint,
            CodeAnalysis.DiagnosticSeverity.Info => RpcContracts.DiagnosticManagement.DiagnosticSeverity.Information,
            CodeAnalysis.DiagnosticSeverity.Warning => RpcContracts.DiagnosticManagement.DiagnosticSeverity.Warning,
            CodeAnalysis.DiagnosticSeverity.Error => RpcContracts.DiagnosticManagement.DiagnosticSeverity.Error,
            _ => throw ExceptionUtilities.UnexpectedValue(severity),
        };
    }

    private static RpcContracts.Utilities.Range GetRange(DiagnosticData diagnostic)
    {
        // Caller always created DiagnosticData with unmapped information.
        var startPosition = diagnostic.DataLocation.UnmappedFileSpan.StartLinePosition;
        var endPosition = diagnostic.DataLocation.UnmappedFileSpan.EndLinePosition;
        return new RpcContracts.Utilities.Range(startPosition.Line, startPosition.Character, endPosition.Line, endPosition.Character);
    }

    /// <summary>
    /// Creates or gets the existing <see cref="IDiagnosticManagerService"/>
    /// It is important that this is created only once as the client will remove our errors
    /// when the instance of the brokered service is disposed of.
    /// 
    /// Serial access to this is guaranteed as all calls run inside the <see cref="_taskQueue"/>
    /// </summary>
    private async Task<IDiagnosticManagerService> GetOrCreateDiagnosticManagerAsync(CancellationToken cancellationToken)
    {
        if (_diagnosticManagerService == null)
        {
            _diagnosticManagerService = await _serviceBroker.GetProxyAsync<IDiagnosticManagerService>(
                VisualStudioServices.VS2019_7.DiagnosticManagerService,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            Contract.ThrowIfNull(_diagnosticManagerService, $"Unable to acquire {nameof(IDiagnosticManagerService)}");
        }

        return _diagnosticManagerService;
    }

    private InProgressState? GetBuildInProgressState()
    {
        lock (_gate)
        {
            return _stateDoNotAccessDirectly;
        }
    }

    private InProgressState? ClearInProgressState()
    {
        lock (_gate)
        {
            var state = _stateDoNotAccessDirectly;

            _stateDoNotAccessDirectly = null;
            return state;
        }
    }

    private InProgressState GetOrCreateInProgressState()
    {
        lock (_gate)
        {
            if (_stateDoNotAccessDirectly == null)
            {
                // We take current snapshot of solution when the state is first created. and through out this code, we use this snapshot.
                // Since we have no idea what actual snapshot of solution the out of proc build has picked up, it doesn't remove the race we can have
                // between build and diagnostic service, but this at least make us to consistent inside of our code.
                _stateDoNotAccessDirectly = new InProgressState(this, _workspace.CurrentSolution);
            }

            return _stateDoNotAccessDirectly;
        }
    }

    private sealed class InProgressState
    {
        private readonly ExternalErrorDiagnosticUpdateSource _owner;

        /// <summary>
        /// Map from project ID to all the possible analyzer diagnostic IDs that can be reported in the project.
        /// </summary>
        /// <remarks>
        /// This map may be accessed concurrently, so needs to ensure thread safety by using locks.
        /// </remarks>
        private readonly Dictionary<ProjectId, ImmutableHashSet<string>> _allDiagnosticIdMap = [];

        public InProgressState(ExternalErrorDiagnosticUpdateSource owner, Solution solution)
        {
            _owner = owner;
            Solution = solution;
        }

        public Solution Solution { get; }

        public bool IsSupportedDiagnosticId(ProjectId projectId, string id)
            => GetOrCreateSupportedDiagnosticIds(projectId).Contains(id);

        private static ImmutableHashSet<string> GetOrCreateDiagnosticIds(
            ProjectId projectId,
            Dictionary<ProjectId, ImmutableHashSet<string>> diagnosticIdMap,
            Func<ImmutableHashSet<string>> computeDiagnosticIds)
        {
            lock (diagnosticIdMap)
            {
                if (diagnosticIdMap.TryGetValue(projectId, out var ids))
                {
                    return ids;
                }
            }

            var computedIds = computeDiagnosticIds();

            lock (diagnosticIdMap)
            {
                diagnosticIdMap[projectId] = computedIds;
                return computedIds;
            }
        }

        private ImmutableHashSet<string> GetOrCreateSupportedDiagnosticIds(ProjectId projectId)
        {
            return GetOrCreateDiagnosticIds(projectId, _allDiagnosticIdMap, ComputeSupportedDiagnosticIds);

            ImmutableHashSet<string> ComputeSupportedDiagnosticIds()
            {
                var project = Solution.GetProject(projectId);
                if (project == null)
                {
                    // projectId no longer exist
                    return ImmutableHashSet<string>.Empty;
                }

                // set ids set
                var builder = ImmutableHashSet.CreateBuilder<string>();
                var descriptorMap = Solution.SolutionState.Analyzers.GetDiagnosticDescriptorsPerReference(_owner.AnalyzerInfoCache, project);
                builder.UnionWith(descriptorMap.Values.SelectMany(v => v.Select(d => d.Id)));

                return builder.ToImmutable();
            }
        }
    }
}
