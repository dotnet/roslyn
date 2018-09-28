using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal abstract partial class VisualStudioWorkspaceImpl
    {
        /// <summary>
        /// The class that's created once the <see cref="VisualStudioWorkspace"/> is finally
        /// getting content inside of it. We have various bits of the implementation
        /// of this workspace that need to start up on the UI thread, but we cannot
        /// guarantee which thread will create the <see cref="VisualStudioWorkspace"/>,
        /// since it could be MEF imported on any thread. This class holds all that "real" state
        /// which can't be touched during construction or in any codepath that
        /// might run before a project is added.
        /// </summary>
        internal class DeferredInitializationState : ForegroundThreadAffinitizedObject
        {
            public VisualStudioProjectTracker ProjectTracker { get; }
            public IServiceProvider ServiceProvider { get; }
            public IVsUIShellOpenDocument ShellOpenDocumentService { get; }

            public DeferredInitializationState(VisualStudioWorkspaceImpl workspace, IServiceProvider serviceProvider)
                : base(assertIsForeground: true)
            {
                ServiceProvider = serviceProvider;
                ShellOpenDocumentService = (IVsUIShellOpenDocument)serviceProvider.GetService(typeof(SVsUIShellOpenDocument));
                ProjectTracker = new VisualStudioProjectTracker(serviceProvider, workspace);

                // Ensure the document tracking service is initialized on the UI thread
                var documentTrackingService = (VisualStudioDocumentTrackingService)workspace.Services.GetService<IDocumentTrackingService>();
                var documentProvider = new DocumentProvider(ProjectTracker, serviceProvider, documentTrackingService);
                var metadataReferenceProvider = workspace.Services.GetService<VisualStudioMetadataReferenceManager>();
                var ruleSetFileProvider = workspace.Services.GetService<VisualStudioRuleSetManager>();
                ProjectTracker.InitializeProviders(documentProvider, metadataReferenceProvider, ruleSetFileProvider);

                var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
                var saveEventsService = componentModel.GetService<SaveEventsService>();
                saveEventsService.StartSendingSaveEvents();

                VisualStudioProjectCacheHostServiceFactory.ConnectProjectCacheServiceToDocumentTracking(workspace.Services, (ProjectCacheService)workspace.CurrentSolution.Services.CacheService);

                // Ensure the options factory services are initialized on the UI thread
                workspace.Services.GetService<IOptionService>();
            }
        }

        internal string GetProjectDisplayName(Project project)
        {
            var hierarchy = this.GetHierarchy(project.Id);
            if (hierarchy != null)
            {
                var solution = (IVsSolution3)DeferredState.ServiceProvider.GetService(typeof(SVsSolution));
                if (solution != null)
                {
                    if (ErrorHandler.Succeeded(solution.GetUniqueUINameOfProject(hierarchy, out string name)) && name != null)
                    {
                        return name;
                    }
                }
            }

            return project.Name;
        }
    }
}
