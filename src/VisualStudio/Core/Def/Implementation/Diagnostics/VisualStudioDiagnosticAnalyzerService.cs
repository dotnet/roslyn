﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
        private readonly IDiagnosticAnalyzerService _diagnosticService;
        private readonly IThreadingContext _threadingContext;
        private readonly IVsHierarchyItemManager _vsHierarchyItemManager;
        private readonly IAsynchronousOperationListener _listener;
        private readonly HostDiagnosticUpdateSource _hostDiagnosticUpdateSource;

        private IServiceProvider? _serviceProvider;

        [ImportingConstructor]
        public VisualStudioDiagnosticAnalyzerService(
            VisualStudioWorkspace workspace,
            IDiagnosticAnalyzerService diagnosticService,
            IThreadingContext threadingContext,
            IVsHierarchyItemManager vsHierarchyItemManager,
            IAsynchronousOperationListenerProvider listenerProvider,
            HostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _workspace = workspace;
            _diagnosticService = diagnosticService;
            _threadingContext = threadingContext;
            _vsHierarchyItemManager = vsHierarchyItemManager;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;
            _listener = listenerProvider.GetListener(FeatureAttribute.DiagnosticService);
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            // Hook up the "Run Code Analysis" menu command for CPS based managed projects.
            var menuCommandService = (IMenuCommandService)_serviceProvider.GetService(typeof(IMenuCommandService));
            if (menuCommandService != null)
            {
                AddCommand(menuCommandService, RunCodeAnalysisForSelectedProjectCommandId, VSConstants.VSStd2K, OnRunCodeAnalysisForSelectedProject, OnRunCodeAnalysisForSelectedProjectStatus);
                AddCommand(menuCommandService, ID.RoslynCommands.RunCodeAnalysisForProject, Guids.RoslynGroupId, OnRunCodeAnalysisForSelectedProject, OnRunCodeAnalysisForSelectedProjectStatus);
            }

            return;

            // Local functions
            static OleMenuCommand AddCommand(
                IMenuCommandService menuCommandService,
                int commandId,
                Guid commandGroup,
                EventHandler invokeHandler,
                EventHandler beforeQueryStatus)
            {
                var commandIdWithGroupId = new CommandID(commandGroup, commandId);
                var command = new OleMenuCommand(invokeHandler, delegate { }, beforeQueryStatus, commandIdWithGroupId);
                menuCommandService.AddCommand(command);
                return command;
            }
        }

        public IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(IVsHierarchy? hierarchy)
        {
            if (hierarchy == null)
            {
                return Transform(_diagnosticService.AnalyzerInfoCache.GetDiagnosticDescriptorsPerReference());
            }

            // Analyzers are only supported for C# and VB currently.
            var projectsWithHierarchy = _workspace.CurrentSolution.Projects
                .Where(p => p.Language == LanguageNames.CSharp || p.Language == LanguageNames.VisualBasic)
                .Where(p => _workspace.GetHierarchy(p.Id) == hierarchy);

            if (projectsWithHierarchy.Count() <= 1)
            {
                var project = projectsWithHierarchy.FirstOrDefault();
                if (project == null)
                {
                    return Transform(_diagnosticService.AnalyzerInfoCache.GetDiagnosticDescriptorsPerReference());
                }
                else
                {
                    return Transform(_diagnosticService.AnalyzerInfoCache.GetDiagnosticDescriptorsPerReference(project));
                }
            }
            else
            {
                // Multiple workspace projects map to the same hierarchy, return a union of descriptors for all projects.
                // For example, this can happen for web projects where we create on the fly projects for aspx files.
                var descriptorsMap = ImmutableDictionary.CreateBuilder<string, IEnumerable<DiagnosticDescriptor>>();
                foreach (var project in projectsWithHierarchy)
                {
                    var descriptorsPerReference = _diagnosticService.AnalyzerInfoCache.GetDiagnosticDescriptorsPerReference(project);
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

        private IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> Transform(
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
            var visible = TryGetSelectedProjectHierarchy(out var hierarchy) &&
                hierarchy.IsCapabilityMatch("CPS") &&
                hierarchy.IsCapabilityMatch(".NET");
            var enabled = false;

            if (visible)
            {
                if (command.CommandID.ID == RunCodeAnalysisForSelectedProjectCommandId &&
                    hierarchy.TryGetProject(out var project))
                {
                    // Change to show the name of the project as part of the menu item display text.
                    command.Text = string.Format(ServicesVSResources.Run_Code_Analysis_on_0, project.Name);
                }

                enabled = !IsBuildActive();
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
            if (TryGetSelectedProjectHierarchy(out var hierarchy))
            {
                RunAnalyzers(hierarchy);
            }
        }

        public void RunAnalyzers(IVsHierarchy? hierarchy)
        {
            var project = GetProject(hierarchy);
            Solution solution = _workspace.CurrentSolution;
            string? projectOrSolutionName = project?.Name ?? PathUtilities.GetFileName(solution.FilePath);

            // Add a message to VS status bar that we are running code analysis.
            var statusMessage = projectOrSolutionName != null
                ? string.Format(ServicesVSResources.Running_code_analysis_for_0, projectOrSolutionName)
                : ServicesVSResources.Running_code_analysis_for_Solution;
            var statusBar = _serviceProvider?.GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            statusBar?.SetText(statusMessage);

            // Force complete analyzer execution in background.
            var asyncToken = _listener.BeginAsyncOperation($"{nameof(VisualStudioDiagnosticAnalyzerService)}_{nameof(RunAnalyzers)}");
            Task.Run(async () =>
            {
                await _diagnosticService.ForceAnalyzeAsync(solution, project?.Id, CancellationToken.None).ConfigureAwait(false);

                // If user has disabled live analyzer execution for any project(s), i.e. set RunAnalyzersDuringLiveAnalysis = false,
                // then ForceAnalyzeAsync will not cause analyzers to execute.
                // We explicitly fetch diagnostics for such projects and report these as "Host" diagnostics.
                HandleProjectsWithDisabledAnalysis();

                // Add a message to VS status bar that we completed executing code analysis.
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                var notificationMesage = projectOrSolutionName != null
                    ? string.Format(ServicesVSResources.Code_analysis_completed_for_0, projectOrSolutionName)
                    : ServicesVSResources.Code_analysis_completed_for_Solution;
                statusBar?.SetText(notificationMesage);
            }).CompletesAsyncOperation(asyncToken);

            return;

            void HandleProjectsWithDisabledAnalysis()
            {
                // First clear all special host diagostics for all involved projects.
                var projects = project != null ? SpecializedCollections.SingletonEnumerable(project) : solution.Projects;
                foreach (var project in projects)
                {
                    _hostDiagnosticUpdateSource.ClearDiagnosticsForProject(project.Id, key: this);
                }

                // Now compute the new host diagostics for all projects with disabled analysis.
                var projectsWithDisabledAnalysis = projects.Where(p => !p.State.RunAnalyzers).ToImmutableArrayOrEmpty();
                if (!projectsWithDisabledAnalysis.IsEmpty)
                {
                    // Compute diagnostics by overidding project's RunCodeAnalysis flag to true.
                    var tasks = new System.Threading.Tasks.Task<ImmutableArray<DiagnosticData>>[projectsWithDisabledAnalysis.Length];
                    for (var index = 0; index < projectsWithDisabledAnalysis.Length; index++)
                    {
                        var project = projectsWithDisabledAnalysis[index];
                        project = project.Solution.WithRunAnalyzers(project.Id, runAnalyzers: true).GetProject(project.Id)!;
                        tasks[index] = Task.Run(
                            () => _diagnosticService.GetDiagnosticsAsync(project.Solution, project.Id));
                    }

                    Task.WhenAll(tasks).Wait();

                    // Report new host diagostics.
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

        private bool TryGetSelectedProjectHierarchy([NotNullWhen(returnValue: true)] out IVsHierarchy? hierarchy)
        {
            hierarchy = null;

            // Get the DTE service and make sure there is an open solution
            if (!(_serviceProvider?.GetService(typeof(EnvDTE.DTE)) is EnvDTE.DTE dte) ||
                dte.Solution == null)
            {
                return false;
            }

            var selectionHierarchy = IntPtr.Zero;
            var selectionContainer = IntPtr.Zero;

            // Get the current selection in the shell
            if (_serviceProvider.GetService(typeof(SVsShellMonitorSelection)) is IVsMonitorSelection monitorSelection)
            {
                try
                {
                    monitorSelection.GetCurrentSelection(out selectionHierarchy, out var itemId, out var multiSelect, out selectionContainer);
                    if (selectionHierarchy != IntPtr.Zero)
                    {
                        hierarchy = Marshal.GetObjectForIUnknown(selectionHierarchy) as IVsHierarchy;
                        Debug.Assert(hierarchy != null);
                        return hierarchy != null;
                    }
                }
                catch (Exception)
                {
                    // If anything went wrong, just ignore it
                }
                finally
                {
                    // Make sure we release the COM pointers in any case
                    if (selectionHierarchy != IntPtr.Zero)
                    {
                        Marshal.Release(selectionHierarchy);
                    }

                    if (selectionContainer != IntPtr.Zero)
                    {
                        Marshal.Release(selectionContainer);
                    }
                }
            }

            return false;
        }

        private bool IsBuildActive()
        {
            // Using KnownUIContexts is faster in case when SBM's package was not loaded yet
            if (KnownUIContexts.SolutionBuildingContext != null)
            {
                return KnownUIContexts.SolutionBuildingContext.IsActive;
            }
            else
            {
                // Unlikely case that above service is not available, let's try Solution Build Manager
                if (_serviceProvider?.GetService(typeof(SVsSolutionBuildManager)) is IVsSolutionBuildManager buildManager)
                {
                    buildManager.QueryBuildManagerBusy(out var buildBusy);
                    return buildBusy != 0;
                }
                else
                {
                    Debug.Fail("Unable to determine whether build is active or not");
                    return true;
                }
            }
        }
    }
}
