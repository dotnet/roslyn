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

            public DeferredInitializationState(IThreadingContext threadingContext, VisualStudioWorkspaceImpl workspace)
                : base(threadingContext, assertIsForeground: false)
            {
                ProjectTracker = new VisualStudioProjectTracker(workspace, threadingContext);

                // TODO: fix this up
                /*

                VisualStudioProjectCacheHostServiceFactory.ConnectProjectCacheServiceToDocumentTracking(workspace.Services, (ProjectCacheService)workspace.CurrentSolution.Services.CacheService);
                */
            }
        }
    }
}
