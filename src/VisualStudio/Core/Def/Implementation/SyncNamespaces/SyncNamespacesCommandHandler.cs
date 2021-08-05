// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Design;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SyncNamespaces;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SyncNamespaces
{
    [Export(typeof(SyncNamespacesCommandHandler)), Shared]
    internal sealed class SyncNamespacesCommandHandler
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly IUIThreadOperationExecutor _threadOperationExecutor;
        private IServiceProvider? _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SyncNamespacesCommandHandler(
            IUIThreadOperationExecutor threadOperationExecutor,
            VisualStudioWorkspace workspace)
        {
            _threadOperationExecutor = threadOperationExecutor;
            _workspace = workspace;
        }

        public void Initialize(IServiceProvider serviceProvider)
        {
            Contract.ThrowIfNull(serviceProvider);

            _serviceProvider = serviceProvider;

            // Hook up the "Remove Unused References" menu command for CPS based managed projects.
            var menuCommandService = (IMenuCommandService)_serviceProvider.GetService(typeof(IMenuCommandService));
            if (menuCommandService != null)
            {
                VisualStudioCommandHandlerHelpers.AddCommand(menuCommandService, ID.RoslynCommands.SyncNamespaces, Guids.RoslynGroupId, OnSyncNamespacesForSelectedProject, OnSyncNamespacesForSelectedProjectStatus);
            }
        }

        private void OnSyncNamespacesForSelectedProjectStatus(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;

            var visible = false;
            var enabled = false;

            if (VisualStudioCommandHandlerHelpers.TryGetSelectedProjectHierarchy(_serviceProvider, out var projectHierarchy))
            {
                // Is a project node. Are we C# project node?
                visible = projectHierarchy.IsCapabilityMatch(".NET")
                    && GetProjectsForHierarchy(projectHierarchy)
                        .All(project => project.Language.Equals(LanguageNames.CSharp, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                // Is a solution node. Do we contain any C# projects?
                visible = _workspace.CurrentSolution.Projects
                    .Any(project => project.Language.Equals(LanguageNames.CSharp, StringComparison.OrdinalIgnoreCase));
            }

            enabled = visible && !VisualStudioCommandHandlerHelpers.IsBuildActive();

            if (command.Visible != visible)
            {
                command.Visible = visible;
            }

            if (command.Enabled != enabled)
            {
                command.Enabled = enabled;
            }
        }

        private void OnSyncNamespacesForSelectedProject(object sender, EventArgs args)
        {
            if (VisualStudioCommandHandlerHelpers.TryGetSelectedProjectHierarchy(_serviceProvider, out var projectHierarchy))
            {
                var projects = GetProjectsForHierarchy(projectHierarchy);

                SyncNamespaces(projects);
            }
            else
            {
                var projects = _workspace.CurrentSolution.Projects
                    .Where(project => project.Language.Equals(LanguageNames.CSharp, StringComparison.OrdinalIgnoreCase))
                    .ToImmutableArray();

                if (_workspace.CurrentSolution.ProjectIds.Count != projects.Length)
                {
                    // Some projects are not C# projects.
                }

                SyncNamespaces(projects);
            }
        }

        private ImmutableArray<Project> GetProjectsForHierarchy(Shell.Interop.IVsHierarchy projectHierarchy)
        {
            var projectFilePath = projectHierarchy.TryGetProjectFilePath();

            var solution = _workspace.CurrentSolution;
            return solution.Projects
                .Where(project => project.FilePath?.Equals(projectFilePath, StringComparison.OrdinalIgnoreCase) == true)
                .ToImmutableArrayOrEmpty();
        }

        private void SyncNamespaces(ImmutableArray<Project> projects)
        {
            if (projects.IsEmpty)
            {
                return;
            }

            var syncService = projects[0].GetRequiredLanguageService<ISyncNamespacesService>();

            Solution? solution = null;
            var status = _threadOperationExecutor.Execute(ServicesVSResources.Sync_Namespaces, ServicesVSResources.Updating_namspaces, allowCancellation: true, showProgress: true, (operationContext) =>
            {
                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    solution = await syncService.SyncNamespacesAsync(projects, operationContext.UserCancellationToken).ConfigureAwait(false);
                });
            });

            if (status != UIThreadOperationStatus.Canceled && solution is not null)
            {
                _workspace.TryApplyChanges(solution);
            }
        }
    }
}
