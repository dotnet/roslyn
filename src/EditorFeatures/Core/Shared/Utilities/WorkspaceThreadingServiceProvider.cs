// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    [ExportWorkspaceService(typeof(IWorkspaceThreadingServiceProvider)), Shared]
    internal sealed class WorkspaceThreadingServiceProvider : IWorkspaceThreadingServiceProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceThreadingServiceProvider(
            IWorkspaceThreadingService service)
        {
            Service = service;
        }

        public IWorkspaceThreadingService Service { get; }
    }
}
