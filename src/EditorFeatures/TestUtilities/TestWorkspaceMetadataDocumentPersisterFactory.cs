// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MetadataAsSource;

namespace Microsoft.CodeAnalysis.Editor.UnitTests;

/// <summary>
/// Test only persister to allow tests to use virtual files instead of file system metadata files.
/// The host will configure the kind of persister it wants, but we want tests to test both.
/// </summary>
[ExportWorkspaceServiceFactory(typeof(IMetadataDocumentPersister), ServiceLayer.Test), Shared, PartNotDiscoverable]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class TestWorkspaceMetadataDocumentPersisterFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        return new WorkspaceMetadataDocumentPersister(workspaceServices.Workspace);
    }
}
