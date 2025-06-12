// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.MetadataAsSource;

//[ExportWorkspaceServiceFactory(typeof(IMetadataDocumentPersister), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class WorkspaceMetadataDocumentPersisterFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        return new WorkspaceMetadataDocumentPersister(workspaceServices.Workspace);
    }
}
