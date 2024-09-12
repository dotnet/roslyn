// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.Interactive
{
    [ExportWorkspaceServiceFactory(typeof(IDocumentNavigationService), [WorkspaceKind.Interactive]), Shared]
    internal sealed class InteractiveDocumentNavigationServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IDocumentNavigationService _singleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public InteractiveDocumentNavigationServiceFactory(IThreadingContext threadingContext)
            => _singleton = new InteractiveDocumentNavigationService(threadingContext);

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _singleton;
    }
}
