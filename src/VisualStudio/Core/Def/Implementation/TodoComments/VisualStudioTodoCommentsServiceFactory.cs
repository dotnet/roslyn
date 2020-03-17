// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TodoComments
{
    [Export(typeof(ITodoListProvider))]
    [ExportWorkspaceServiceFactory(typeof(IVisualStudioTodoCommentsService), ServiceLayer.Host), Shared]
    internal class VisualStudioTodoCommentsServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly EventListenerTracker<ITodoListProvider> _eventListenerTracker;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioTodoCommentsServiceFactory(
            IThreadingContext threadingContext,
            [ImportMany]IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            _threadingContext = threadingContext;
            _eventListenerTracker = new EventListenerTracker<ITodoListProvider>(eventListeners, WellKnownEventListeners.TodoListProvider);
        }

        public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
        {
            if (!(workspaceServices.Workspace is VisualStudioWorkspaceImpl workspace))
                return null;

            return new VisualStudioTodoCommentsService(
                workspace, _threadingContext, _eventListenerTracker);
        }
    }
}
