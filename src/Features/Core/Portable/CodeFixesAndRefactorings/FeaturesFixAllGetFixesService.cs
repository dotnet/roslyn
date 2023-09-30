// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Suggestions;

[ExportWorkspaceServiceFactory(typeof(IFixAllGetFixesService), ServiceLayer.Host), Shared]
internal sealed class FeaturesFixAllGetFixesService : AbstractFixAllGetFixesService, IWorkspaceServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FeaturesFixAllGetFixesService()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => this;
}
