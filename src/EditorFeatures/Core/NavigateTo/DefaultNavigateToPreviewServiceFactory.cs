// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;

[ExportWorkspaceServiceFactory(typeof(INavigateToPreviewService), ServiceLayer.Editor), Shared]
internal sealed class DefaultNavigateToPreviewServiceFactory : IWorkspaceServiceFactory
{
    private readonly Lazy<INavigateToPreviewService> _singleton =
        new(() => new DefaultNavigateToPreviewService());

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultNavigateToPreviewServiceFactory()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => _singleton.Value;
}
