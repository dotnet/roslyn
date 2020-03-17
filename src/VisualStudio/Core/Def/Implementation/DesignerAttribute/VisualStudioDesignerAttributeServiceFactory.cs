// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DesignerAttribute
{
    [ExportWorkspaceServiceFactory(typeof(IDesignerAttributeService), ServiceLayer.Host), Shared]
    internal class VisualStudioDesignerAttributeServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioDesignerAttributeServiceFactory(
            IThreadingContext threadingContext,
            Shell.SVsServiceProvider serviceProvider)
        {
            _threadingContext = threadingContext;
            _serviceProvider = serviceProvider;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (!(workspaceServices.Workspace is VisualStudioWorkspaceImpl workspace))
                return null;

            return new VisualStudioDesignerAttributeService(
                workspace, _threadingContext, _serviceProvider);
        }
    }
}
