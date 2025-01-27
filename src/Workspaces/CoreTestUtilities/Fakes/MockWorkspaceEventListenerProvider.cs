// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities;

// mock default workspace event listener so that we don't try to enable workspace listeners implicitly
[Export(typeof(MockWorkspaceEventListenerProvider))]
[ExportWorkspaceServiceFactory(typeof(IWorkspaceEventListenerService), ServiceLayer.Test), Shared, PartNotDiscoverable]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, true)]
internal sealed class MockWorkspaceEventListenerProvider() : IWorkspaceServiceFactory
{
    public IEnumerable<IEventListener<object>>? EventListeners;

    public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
        => EventListeners != null ? new DefaultWorkspaceEventListenerServiceFactory.Service(workspaceServices.Workspace, EventListeners) : null;
}
