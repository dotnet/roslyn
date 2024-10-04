// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.NavigateTo;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.NavigateTo;

[ExportWorkspaceServiceFactory(typeof(INavigateToPreviewService), ServiceLayer.Host), Shared]
internal sealed class VisualStudioNavigateToPreviewServiceFactory : IWorkspaceServiceFactory
{
    private readonly Lazy<INavigateToPreviewService> _singleton =
        new(() => new VisualStudioNavigateToPreviewService());

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioNavigateToPreviewServiceFactory()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => _singleton.Value;
}
