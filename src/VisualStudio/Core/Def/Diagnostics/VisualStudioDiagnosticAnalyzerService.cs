// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;

[Export(typeof(IVisualStudioDiagnosticAnalyzerService))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class VisualStudioDiagnosticAnalyzerService(
    VisualStudioWorkspace workspace,
    IVsService<SVsStatusbar, IVsStatusbar> statusbar,
    IThreadingContext threadingContext,
    IVsHierarchyItemManager vsHierarchyItemManager,
    IAsynchronousOperationListenerProvider listenerProvider) : IVisualStudioDiagnosticAnalyzerService
{
    // "Run Code Analysis on <%ProjectName%>" command for Top level "Build" and "Analyze" menus.
    // The below ID is actually defined as "ECMD_RUNFXCOPSEL" in stdidcmd.h, we're just referencing it here.
    private const int RunCodeAnalysisForSelectedProjectCommandId = 1647;

    private readonly VisualStudioWorkspace _workspace = workspace;
    private readonly IVsService<IVsStatusbar> _statusbar = statusbar;
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IVsHierarchyItemManager _vsHierarchyItemManager = vsHierarchyItemManager;
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);
    private readonly ICodeAnalysisDiagnosticAnalyzerService _codeAnalysisService = workspace.Services.GetRequiredService<ICodeAnalysisDiagnosticAnalyzerService>();

    private readonly CancellationSeries _cancellationSeries = new(threadingContext.DisposalToken);

    private IServiceProvider? _serviceProvider;

    public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        _serviceProvider = (IServiceProvider)serviceProvider;

        // Hook up the "Run Code Analysis" menu command for CPS based managed projects.
        var menuCommandService = await serviceProvider.GetServiceAsync<IMenuCommandService, IMenuCommandService>(throwOnFailure: false, cancellationToken).ConfigureAwait(false);
        if (menuCommandService != null)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, RunCodeAnalysisForSelectedProjectCommandId, VSConstants.VSStd2K, OnRunCodeAnalysisForSelectedProject, OnRunCodeAnalysisForSelectedProjectStatus);
            VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.RunCodeAnalysisForProject, Guids.RoslynGroupId, OnRunCodeAnalysisForSelectedProject, OnRunCodeAnalysisForSelectedProjectStatus);
        }
    }

    public async Task<IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>>> GetAllDiagnosticDescriptorsAsync(
        IVsHierarchy? hierarchy,
        CancellationToken cancellationToken)
    {
        var currentSolution = _workspace.CurrentSolution;
        var hostAnalyzers = currentSolution.SolutionState.Analyzers;
        var diagnosticService = currentSolution.Services.GetRequiredService<IDiagnosticAnalyzerService>();

        if (hierarchy == null)
        {
            return Transform(
                await diagnosticService.GetDiagnosticDescriptorsPerReferenceAsync(
                    currentSolution, cancellationToken).ConfigureAwait(false));
        }

        // Analyzers are only supported for C# and VB currently.
        var projectsWithHierarchy = currentSolution.Projects
            .Where(p => p.Language is LanguageNames.CSharp or LanguageNames.VisualBasic)
            .Where(p => _workspace.GetHierarchy(p.Id) == hierarchy);

        if (projectsWithHierarchy.Count() <= 1)
        {
            var project = projectsWithHierarchy.FirstOrDefault();
            return project == null
                ? Transform(await diagnosticService.GetDiagnosticDescriptorsPerReferenceAsync(currentSolution, cancellationToken).ConfigureAwait(false))
                : Transform(await diagnosticService.GetDiagnosticDescriptorsPerReferenceAsync(project, cancellationToken).ConfigureAwait(false));
        }
        else
        {
            // Multiple workspace projects map to the same hierarchy, return a union of descriptors for all projects.
            // For example, this can happen for web projects where we create on the fly projects for aspx files.
            var descriptorsMap = ImmutableDictionary.CreateBuilder<string, IEnumerable<DiagnosticDescriptor>>();
            foreach (var project in projectsWithHierarchy)
            {
                var descriptorsPerReference = await diagnosticService.GetDiagnosticDescriptorsPerReferenceAsync(
                    project, cancellationToken).ConfigureAwait(false);
                foreach (var (displayName, descriptors) in descriptorsPerReference)
                {
                    if (descriptorsMap.TryGetValue(displayName, out var existingDescriptors))
                    {
                        descriptorsMap[displayName] = existingDescriptors.Concat(descriptors).Distinct();
                    }
                    else
                    {
                        descriptorsMap[displayName] = descriptors;
                    }
                }
            }

            return descriptorsMap.ToImmutable();
        }
    }

    private static IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> Transform(
        ImmutableDictionary<string, ImmutableArray<DiagnosticDescriptor>> map)
    {
        // unfortunately, we had to do this since ruleset editor and us are set to use this signature
        return map.ToDictionary(kv => kv.Key, kv => (IEnumerable<DiagnosticDescriptor>)kv.Value);
    }

    private void OnRunCodeAnalysisForSelectedProjectStatus(object sender, EventArgs e)
    {
        var command = (OleMenuCommand)sender;

        // We hook up the "Run Code Analysis" menu commands for CPS based managed projects.
        // These commands are already hooked up for csproj based projects in StanCore, but those will eventually go away.
        var visible = VisualStudioCommandHandlerHelpers.TryGetSelectedProjectHierarchy(_serviceProvider, out var hierarchy) &&
            hierarchy.IsCapabilityMatch("CPS") &&
            hierarchy.IsCapabilityMatch(".NET");
        var enabled = false;

        if (visible)
        {
            if (command.CommandID.ID == RunCodeAnalysisForSelectedProjectCommandId &&
                hierarchy!.TryGetProject(out var project))
            {
                // Change to show the name of the project as part of the menu item display text.
                command.Text = string.Format(ServicesVSResources.Run_Code_Analysis_on_0, project.Name);
            }

            enabled = !VisualStudioCommandHandlerHelpers.IsBuildActive();
        }

        if (command.Visible != visible)
        {
            command.Visible = visible;
        }

        if (command.Enabled != enabled)
        {
            command.Enabled = enabled;
        }
    }

    private void OnRunCodeAnalysisForSelectedProject(object sender, EventArgs args)
    {
        if (VisualStudioCommandHandlerHelpers.TryGetSelectedProjectHierarchy(_serviceProvider, out var hierarchy))
            RunAnalyzers(hierarchy);
    }

    public void RunAnalyzers(IVsHierarchy? hierarchy)
    {
        // If a new command comes in to run analyzers again, cancel any existing operation in progress and start a new one.
        var cancellationToken = _cancellationSeries.CreateNext();

        var solution = _workspace.CurrentSolution;
        var project = GetProject(solution, hierarchy);

        // 1. If we were given no specific project to analyze, then analyze all projects in the solution.
        // 2. If we were given a specific project to analyze, then analyze all TFM flavors of it.
        var projectsToAnalyze = project is null
            ? [.. solution.Projects]
            : solution.Projects.WhereAsArray(
                static (otherProject, project) => otherProject.FilePath == project.FilePath && otherProject.State.NameAndFlavor.name == project.State.NameAndFlavor.name,
                project);

        // Pick an appropriate name for any of those cases above for reporting progress. Either the solution name if
        // we're analyzing the whole solution, or the project name (with TFM) if we only have a single project, or the
        // project name (without TFM) if we're analyzing all flavors of it.
        var progressName = project is null
            ? PathUtilities.GetFileName(solution.FilePath) ?? FeaturesResources.Solution
            : projectsToAnalyze.Length == 1 ? project.Name : project.State.NameAndFlavor.name ?? project.Name;

        _threadingContext.JoinableTaskFactory.RunAsync(async () =>
        {
            try
            {
                using var asyncToken = _listener.BeginAsyncOperation($"{nameof(VisualStudioDiagnosticAnalyzerService)}_{nameof(RunAnalyzers)}");

                // Add a message to VS status bar that we are running code analysis.
                using var statusBarUpdater = new StatusBarUpdater(
                    this, await _statusbar.GetValueOrNullAsync(cancellationToken).ConfigureAwait(true),
                    progressName, totalProjectCount: projectsToAnalyze.Length, cancellationToken);

                await Parallel.ForEachAsync(
                    projectsToAnalyze,
                    cancellationToken,
                    async (project, cancellationToken) =>
                    {
                        await _codeAnalysisService.RunAnalysisAsync(project, cancellationToken).ConfigureAwait(false);
                        statusBarUpdater.OnAfterProjectAnalyzed();
                    }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
            }
        });
    }

    private Project? GetProject(Solution solution, IVsHierarchy? hierarchy)
    {
        if (hierarchy != null)
        {
            var projectMap = _workspace.Services.GetRequiredService<IHierarchyItemToProjectIdMap>();
            var projectHierarchyItem = _vsHierarchyItemManager.GetHierarchyItem(hierarchy, VSConstants.VSITEMID_ROOT);
            if (projectMap.TryGetProjectId(projectHierarchyItem, targetFrameworkMoniker: null, out var projectId))
                return solution.GetProject(projectId);
        }

        return null;
    }

    private sealed class StatusBarUpdater : IDisposable
    {
        /// <summary>
        /// Queue to batch up work.  Only created if we have an actual status bar in VS to push the progress updates to.
        /// </summary>
        private readonly AsyncBatchingWorkQueue? _progressTracker;

        private bool _disposed;
        private int _completedProjects;
        private int _inProgress = 1;

        public StatusBarUpdater(
            VisualStudioDiagnosticAnalyzerService service,
            IVsStatusbar? statusBar,
            string progressName,
            int totalProjectCount,
            CancellationToken cancellationToken)
        {
            if (statusBar is null)
                return;

            var threadingContext = service._threadingContext;

            var statusMessageWhileRunning = string.Format(ServicesVSResources.Running_code_analysis_for_0, progressName);
            var statusMessageOnCompleted = string.Format(ServicesVSResources.Code_analysis_completed_for_0, progressName);
            var statusMessageOnTerminated = string.Format(ServicesVSResources.Code_analysis_terminated_before_completion_for_0, progressName);

            // Set the initial status bar progress and text.

            uint statusBarCookie = 0;
            UpdateStatusBar();

            _progressTracker = new(
                DelayTimeSpan.Medium,
                async cancellationToken =>
                {
                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    UpdateStatusBar();
                },
                service._listener,
                cancellationToken);

            return;

            void UpdateStatusBar()
            {
                threadingContext.ThrowIfNotOnUIThread();

                // Once we've transitioned to the completed state, we never want to update again.
                if (_inProgress == 0)
                    return;

                var analyzedProjectCount = _completedProjects;
                var disposed = _disposed;

                var inProgress = analyzedProjectCount < totalProjectCount && !disposed ? 1 : 0;
                var message =
                    analyzedProjectCount == totalProjectCount ? statusMessageOnCompleted :
                    disposed ? statusMessageOnTerminated : statusMessageWhileRunning;

                statusBar.Progress(
                    ref statusBarCookie,
                    fInProgress: inProgress,
                    message,
                    (uint)analyzedProjectCount,
                    (uint)totalProjectCount);
                statusBar.SetText(message);

                _inProgress = inProgress;
            }
        }

        public void OnAfterProjectAnalyzed()
        {
            Interlocked.Increment(ref _completedProjects);
            _progressTracker?.AddWork();
        }

        public void Dispose()
        {
            _disposed = true;
            _progressTracker?.AddWork();
        }
    }
}
