// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectTelemetry
{
    [ExportWorkspaceServiceFactory(typeof(IProjectTelemetryService), ServiceLayer.Host), Shared]
    internal class VisualStudioProjectTelemetryServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioProjectTelemetryServiceFactory(IThreadingContext threadingContext)
            => _threadingContext = threadingContext;

        public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
        {
            if (!(workspaceServices.Workspace is VisualStudioWorkspaceImpl workspace))
                return null;

            return new VisualStudioProjectTelemetryService(workspace, _threadingContext);
        }
    }
}
