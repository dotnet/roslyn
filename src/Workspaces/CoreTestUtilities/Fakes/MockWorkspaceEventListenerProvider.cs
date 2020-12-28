// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    // mock default workspace event listener so that we don't try to enable solution crawler and etc implicitly
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceEventListenerService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    internal sealed class MockWorkspaceEventListenerProvider : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, true)]
        public MockWorkspaceEventListenerProvider()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => null;
    }
}
