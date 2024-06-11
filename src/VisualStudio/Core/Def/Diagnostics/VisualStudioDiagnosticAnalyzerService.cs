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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics
{
    [Export(typeof(IVisualStudioDiagnosticAnalyzerService))]
    internal partial class VisualStudioDiagnosticAnalyzerService : IVisualStudioDiagnosticAnalyzerService
    {
        // "Run Code Analysis on <%ProjectName%>" command for Top level "Build" and "Analyze" menus.
        // The below ID is actually defined as "ECMD_RUNFXCOPSEL" in stdidcmd.h, we're just referencing it here.
        private const int RunCodeAnalysisForSelectedProjectCommandId = 1647;

        private readonly VisualStudioWorkspace _workspace;
        private readonly IVsService<IVsStatusbar> _statusbar;
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IThreadingContext _threadingContext;
        private readonly IVsHierarchyItemManager _vsHierarchyItemManager;
        private readonly IAsynchronousOperationListener _listener;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;
        private readonly IGlobalOptionService _globalOptions;

        private IServiceProvider? _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDiagnosticAnalyzerService(
            VisualStudioWorkspace workspace,
            IVsService<SVsStatusbar, IVsStatusbar> statusbar,
            IDiagnosticAnalyzerService diagnosticService,
            IThreadingContext threadingContext,
            IVsHierarchyItemManager vsHierarchyItemManager,
            IAsynchronousOperationListenerProvider listenerProvider,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            IGlobalOptionService globalOptions)
        {
            _workspace = workspace;
            _statusbar = statusbar;
            _diagnosticService = diagnosticService;
            _threadingContext = threadingContext;
            _vsHierarchyItemManager = vsHierarchyItemManager;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            _listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);
            _globalOptions = globalOptions;
        }

        public async Task InitializeAsync(IAsyncServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            _serviceProvider = (IServiceProvider)serviceProvider;

            // Hook up the "Run Code Analysis" menu command for CPS based managed projects.
            var menuCommandService = await serviceProvider.GetServiceAsync<IMenuCommandService, IMenuCommandService>(_threadingContext.JoinableTaskFactory, throwOnFailure: false).ConfigureAwait(false);
            if (menuCommandService != null)
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, RunCodeAnalysisForSelectedProjectCommandId, VSConstants.VSStd2K, OnRunCodeAnalysisForSelectedProject, OnRunCodeAnalysisForSelectedProjectStatus);
                VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.RunCodeAnalysisForProject, Guids.RoslynGroupId, OnRunCodeAnalysisForSelectedProject, OnRunCodeAnalysisForSelectedProjectStatus);
                VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.AnalysisScopeDefault, Guids.RoslynGroupId, OnSetAnalysisScopeDefault, OnSetAnalysisScopeDefaultStatus);
                VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.AnalysisScopeCurrentDocument, Guids.RoslynGroupId, OnSetAnalysisScopeCurrentDocument, OnSetAnalysisScopeCurrentDocumentStatus);
                VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.AnalysisScopeOpenDocuments, Guids.RoslynGroupId, OnSetAnalysisScopeOpenDocuments, OnSetAnalysisScopeOpenDocumentsStatus);
                VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.AnalysisScopeEntireSolution, Guids.RoslynGroupId, OnSetAnalysisScopeEntireSolution, OnSetAnalysisScopeEntireSolutionStatus);
                VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.AnalysisScopeNone, Guids.RoslynGroupId, OnSetAnalysisScopeNone, OnSetAnalysisScopeNoneStatus);
            }
        }

        public IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(IVsHierarchy? hierarchy)
        {
            var currentSolution = _workspace.CurrentSolution;
            var infoCache = _diagnosticService.AnalyzerInfoCache;
            var hostAnalyzers = currentSolution.State.Analyzers;

            if (hierarchy == null)
            {
                return Transform(hostAnalyzers.GetDiagnosticDescriptorsPerReference(infoCache));
            }

            // Analyzers are only supported for C# and VB currently.
            var projectsWithHierarchy = currentSolution.Projects
                .Where(p => p.Language is LanguageNames.CSharp or LanguageNames.VisualBasic)
                .Where(p => _workspace.GetHierarchy(p.Id) == hierarchy);

            if (projectsWithHierarchy.Count() <= 1)
            {
                var project = projectsWithHierarchy.FirstOrDefault();
                if (project == null)
                {
                    return Transform(hostAnalyzers.GetDiagnosticDescriptorsPerReference(infoCache));
                }
                else
                {
                    return Transform(hostAnalyzers.GetDiagnosticDescriptorsPerReference(infoCache, project));
                }
            }
            else
            {
                // Multiple workspace projects map to the same hierarchy, return a union of descriptors for all projects.
                // For example, this can happen for web projects where we create on the fly projects for aspx files.
                var descriptorsMap = ImmutableDictionary.CreateBuilder<string, IEnumerable<DiagnosticDescriptor>>();
                foreach (var project in projectsWithHierarchy)
                {
                    var descriptorsPerReference = hostAnalyzers.GetDiagnosticDescriptorsPerReference(infoCache, project);
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

        private void OnSetAnalysisScopeDefaultStatus(object sender, EventArgs e)
            => OnSetAnalysisScopeStatus((OleMenuCommand)sender, scope: null);

        private void OnSetAnalysisScopeCurrentDocumentStatus(object sender, EventArgs e)
            => OnSetAnalysisScopeStatus((OleMenuCommand)sender, BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics);

        private void OnSetAnalysisScopeOpenDocumentsStatus(object sender, EventArgs e)
            => OnSetAnalysisScopeStatus((OleMenuCommand)sender, BackgroundAnalysisScope.OpenFiles);

        private void OnSetAnalysisScopeEntireSolutionStatus(object sender, EventArgs e)
            => OnSetAnalysisScopeStatus((OleMenuCommand)sender, BackgroundAnalysisScope.FullSolution);

        private void OnSetAnalysisScopeNoneStatus(object sender, EventArgs e)
            => OnSetAnalysisScopeStatus((OleMenuCommand)sender, BackgroundAnalysisScope.None);

        private void OnSetAnalysisScopeStatus(OleMenuCommand command, BackgroundAnalysisScope? scope)
        {
            // The command is enabled as long as we have a service provider
            if (_serviceProvider is null)
            {
                // Not yet initialized
                command.Enabled = false;
                return;
            }

            command.Enabled = true;

            // The command is checked if RoslynPackage is loaded and the analysis scope for this command matches the
            // value saved for the solution.
            var roslynPackage = _threadingContext.JoinableTaskFactory.Run(() =>
            {
                return RoslynPackage.GetOrLoadAsync(_threadingContext, (IAsyncServiceProvider)_serviceProvider, _threadingContext.DisposalToken).AsTask();
            });

            if (roslynPackage is not null)
            {
                command.Checked = roslynPackage.AnalysisScope == scope;
            }

            // For the specific case of the default analysis scope command, update the command text to show the
            // current effective default in the context of the language(s) used in the solution.
            if (scope is null)
            {
                command.Text = GetBackgroundAnalysisScope(_workspace.CurrentSolution, _globalOptions) switch
                {
                    BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics => ServicesVSResources.Default_Current_Document,
                    BackgroundAnalysisScope.OpenFiles => ServicesVSResources.Default_Open_Documents,
                    BackgroundAnalysisScope.FullSolution => ServicesVSResources.Default_Entire_Solution,
                    BackgroundAnalysisScope.None => ServicesVSResources.Default_None,
                    _ => ServicesVSResources.Default_,
                };
            }

            return;

            // Local functions
            static BackgroundAnalysisScope? GetBackgroundAnalysisScope(Solution solution, IGlobalOptionService globalOptions)
            {
                var csharpAnalysisScope = globalOptions.GetOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp);
                var visualBasicAnalysisScope = globalOptions.GetOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic);

                var containsCSharpProject = solution.Projects.Any(static project => project.Language == LanguageNames.CSharp);
                var containsVisualBasicProject = solution.Projects.Any(static project => project.Language == LanguageNames.VisualBasic);
                if (containsCSharpProject && containsVisualBasicProject)
                {
                    if (csharpAnalysisScope == visualBasicAnalysisScope)
                        return csharpAnalysisScope;
                    else
                        return null;
                }
                else if (containsVisualBasicProject)
                {
                    return visualBasicAnalysisScope;
                }
                else
                {
                    return csharpAnalysisScope;
                }
            }
        }

        private void OnSetAnalysisScopeDefault(object sender, EventArgs args)
            => OnSetAnalysisScope(scope: null);

        private void OnSetAnalysisScopeCurrentDocument(object sender, EventArgs args)
            => OnSetAnalysisScope(BackgroundAnalysisScope.VisibleFilesAndOpenFilesWithPreviouslyReportedDiagnostics);

        private void OnSetAnalysisScopeOpenDocuments(object sender, EventArgs args)
            => OnSetAnalysisScope(BackgroundAnalysisScope.OpenFiles);

        private void OnSetAnalysisScopeEntireSolution(object sender, EventArgs args)
            => OnSetAnalysisScope(BackgroundAnalysisScope.FullSolution);

        private void OnSetAnalysisScopeNone(object sender, EventArgs args)
            => OnSetAnalysisScope(BackgroundAnalysisScope.None);

        private void OnSetAnalysisScope(BackgroundAnalysisScope? scope)
        {
            if (_serviceProvider is null
                || !_serviceProvider.TryGetService<SVsShell, IVsShell>(_threadingContext.JoinableTaskFactory, out var shell))
            {
                return;
            }

            var roslynPackage = _threadingContext.JoinableTaskFactory.Run(() =>
            {
                return RoslynPackage.GetOrLoadAsync(_threadingContext, (IAsyncServiceProvider)_serviceProvider, _threadingContext.DisposalToken).AsTask();
            });

            Assumes.Present(roslynPackage);

            roslynPackage.AnalysisScope = scope;
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
            {
                RunAnalyzers(hierarchy);
            }
        }

        public void RunAnalyzers(IVsHierarchy? hierarchy)
        {
            var project = GetProject(hierarchy);
            var solution = _workspace.CurrentSolution;
            var projectOrSolutionName = project?.Name ?? PathUtilities.GetFileName(solution.FilePath);

            // Handle multi-tfm projects - we want to run code analysis for all tfm flavors of the project.
            ImmutableArray<Project> otherProjectsForMultiTfmProject;
            if (project != null)
            {
                otherProjectsForMultiTfmProject = solution.Projects.Where(
                    p => p != project && p.FilePath == project.FilePath && p.State.NameAndFlavor.name == project.State.NameAndFlavor.name).ToImmutableArray();
                if (!otherProjectsForMultiTfmProject.IsEmpty)
                    projectOrSolutionName = project.State.NameAndFlavor.name;
            }
            else
            {
                otherProjectsForMultiTfmProject = ImmutableArray<Project>.Empty;
            }

            bool isAnalysisDisabled;
            if (project != null)
            {
                isAnalysisDisabled = _globalOptions.IsAnalysisDisabled(project.Language);
            }
            else
            {
                isAnalysisDisabled = true;
                foreach (var language in solution.Projects.Select(p => p.Language).Distinct())
                {
                    isAnalysisDisabled = isAnalysisDisabled && _globalOptions.IsAnalysisDisabled(language);
                }
            }

            // Force complete analyzer execution in background.
            _threadingContext.JoinableTaskFactory.RunAsync(async () =>
            {
                using var asyncToken = _listener.BeginAsyncOperation($"{nameof(VisualStudioDiagnosticAnalyzerService)}_{nameof(RunAnalyzers)}");

                // Add a message to VS status bar that we are running code analysis.
                var statusBar = await _statusbar.GetValueOrNullAsync().ConfigureAwait(true);
                var totalProjectCount = project != null ? (1 + otherProjectsForMultiTfmProject.Length) : solution.ProjectIds.Count;
                using var statusBarUpdater = statusBar != null
                    ? new StatusBarUpdater(statusBar, _threadingContext, projectOrSolutionName, (uint)totalProjectCount)
                    : null;

                await TaskScheduler.Default;

                var onProjectAnalyzed = statusBarUpdater != null ? statusBarUpdater.OnProjectAnalyzed : (Action<Project>)((Project _) => { });
                await _diagnosticService.ForceAnalyzeAsync(solution, onProjectAnalyzed, project?.Id, CancellationToken.None).ConfigureAwait(false);

                foreach (var otherProject in otherProjectsForMultiTfmProject)
                    await _diagnosticService.ForceAnalyzeAsync(solution, onProjectAnalyzed, otherProject.Id, CancellationToken.None).ConfigureAwait(false);

                // If user has disabled live analyzer execution for any project(s), i.e. set RunAnalyzersDuringLiveAnalysis = false,
                // then ForceAnalyzeAsync will not cause analyzers to execute.
                // We explicitly fetch diagnostics for such projects and report these as "Host" diagnostics.
                HandleProjectsWithDisabledAnalysis();
            });

            return;

            void HandleProjectsWithDisabledAnalysis()
            {
                RoslynDebug.Assert(solution != null);

                // First clear all special host diagostics for all involved projects.
                var projects = project != null ? otherProjectsForMultiTfmProject.Add(project) : solution.Projects;
                foreach (var project in projects)
                {
                    _hostDiagnosticUpdateSource.ClearDiagnosticsForProject(project.Id, key: this);
                }

                // Now compute the new host diagostics for all projects with disabled analysis.
                var projectsWithDisabledAnalysis = isAnalysisDisabled
                    ? projects.ToImmutableArray()
                    : projects.Where(p => !p.State.RunAnalyzers).ToImmutableArrayOrEmpty();
                if (!projectsWithDisabledAnalysis.IsEmpty)
                {
                    // Compute diagnostics by overriding project's RunCodeAnalysis flag to true.
                    var tasks = new System.Threading.Tasks.Task<ImmutableArray<DiagnosticData>>[projectsWithDisabledAnalysis.Length];
                    for (var index = 0; index < projectsWithDisabledAnalysis.Length; index++)
                    {
                        var project = projectsWithDisabledAnalysis[index];
                        project = project.Solution.WithRunAnalyzers(project.Id, runAnalyzers: true).GetProject(project.Id)!;
                        tasks[index] = Task.Run(
                            () => _diagnosticService.GetDiagnosticsAsync(project.Solution, project.Id, documentId: null,
                                        includeSuppressedDiagnostics: false, includeNonLocalDocumentDiagnostics: true, CancellationToken.None));
                    }

                    Task.WhenAll(tasks).Wait();

                    // Report new host diagnostics.
                    for (var index = 0; index < projectsWithDisabledAnalysis.Length; index++)
                    {
                        var project = projectsWithDisabledAnalysis[index];
                        var diagnostics = tasks[index].Result;
                        _hostDiagnosticUpdateSource.UpdateDiagnosticsForProject(project.Id, key: this, diagnostics);
                    }
                }
            }
        }

        private Project? GetProject(IVsHierarchy? hierarchy)
        {
            if (hierarchy != null)
            {
                var projectMap = _workspace.Services.GetRequiredService<IHierarchyItemToProjectIdMap>();
                var projectHierarchyItem = _vsHierarchyItemManager.GetHierarchyItem(hierarchy, VSConstants.VSITEMID_ROOT);
                if (projectMap.TryGetProjectId(projectHierarchyItem, targetFrameworkMoniker: null, out var projectId))
                {
                    return _workspace.CurrentSolution.GetProject(projectId);
                }
            }

            return null;
        }

        private sealed class StatusBarUpdater : IDisposable
        {
            private readonly IVsStatusbar _statusBar;
            private readonly IThreadingContext _threadingContext;
            private readonly uint _totalProjectCount;
            private readonly string _statusMessageWhileRunning;
            private readonly string _statusMesageOnCompleted;
            private readonly string _statusMesageOnTerminated;
            private readonly Timer _timer;

            private int _analyzedProjectCount;
            private bool _disposed;
            private uint _statusBarCookie;

            public StatusBarUpdater(IVsStatusbar statusBar, IThreadingContext threadingContext, string? projectOrSolutionName, uint totalProjectCount)
            {
                threadingContext.ThrowIfNotOnUIThread();
                _statusBar = statusBar;
                _threadingContext = threadingContext;
                _totalProjectCount = totalProjectCount;

                _statusMessageWhileRunning = projectOrSolutionName != null
                    ? string.Format(ServicesVSResources.Running_code_analysis_for_0, projectOrSolutionName)
                    : ServicesVSResources.Running_code_analysis_for_Solution;
                _statusMesageOnCompleted = projectOrSolutionName != null
                    ? string.Format(ServicesVSResources.Code_analysis_completed_for_0, projectOrSolutionName)
                    : ServicesVSResources.Code_analysis_completed_for_Solution;
                _statusMesageOnTerminated = projectOrSolutionName != null
                    ? string.Format(ServicesVSResources.Code_analysis_terminated_before_completion_for_0, projectOrSolutionName)
                    : ServicesVSResources.Code_analysis_terminated_before_completion_for_Solution;

                // Set the initial status bar progress and text.
                _statusBar.Progress(ref _statusBarCookie, fInProgress: 1, _statusMessageWhileRunning, nComplete: 0, nTotal: totalProjectCount);
                _statusBar.SetText(_statusMessageWhileRunning);

                // Create a timer to periodically update the status message while running analysis.
                _timer = new Timer(new TimerCallback(UpdateStatusOnTimer), new AutoResetEvent(false),
                    dueTime: TimeSpan.FromSeconds(5), period: TimeSpan.FromSeconds(5));
            }

            internal void OnProjectAnalyzed(Project _)
            {
                Interlocked.Increment(ref _analyzedProjectCount);
                UpdateStatusCore();
            }

            // Add a message to VS status bar that we are running code analysis.
            private void UpdateStatusOnTimer(object state)
                => UpdateStatusCore();

            public void Dispose()
            {
                _timer.Dispose();
                _disposed = true;
                UpdateStatusCore();
            }

            private void UpdateStatusCore()
            {
                _threadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    string message;
                    int fInProgress;
                    var analyzedProjectCount = (uint)_analyzedProjectCount;
                    if (analyzedProjectCount == _totalProjectCount)
                    {
                        message = _statusMesageOnCompleted;
                        fInProgress = 0;
                    }
                    else if (_disposed)
                    {
                        message = _statusMesageOnTerminated;
                        fInProgress = 0;
                    }
                    else
                    {
                        message = _statusMessageWhileRunning;
                        fInProgress = 1;
                    }

                    // Update the status bar progress and text.
                    _statusBar.Progress(ref _statusBarCookie, fInProgress, message, analyzedProjectCount, _totalProjectCount);
                    _statusBar.SetText(message);
                });
            }
        }
    }
}
