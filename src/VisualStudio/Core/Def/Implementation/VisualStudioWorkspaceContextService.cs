// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

[ExportWorkspaceService(typeof(IWorkspaceContextService), ServiceLayer.Host), Shared]
internal sealed class VisualStudioWorkspaceContextService : IWorkspaceContextService
{
    // UI context defined by Live Share when connected as a guest in a Live Share session
    // https://devdiv.visualstudio.com/DevDiv/_git/Cascade?path=%2Fsrc%2FVS%2FContracts%2FGuidList.cs&version=GBmain&line=32&lineEnd=33&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
    private static readonly Guid s_liveShareGuestUIContextGuid = Guid.Parse("fd93f3eb-60da-49cd-af15-acda729e357e");

    private readonly IGlobalOptionService _globalOptions;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public VisualStudioWorkspaceContextService(IGlobalOptionService globalOptions)
    {
        _globalOptions = globalOptions;
    }

    public bool IsCloudEnvironmentClient()
        => UIContext.FromUIContextGuid(VSConstants.UICONTEXT.CloudEnvironmentConnected_guid).IsActive;

    public bool IsInLspEditorContext()
        => IsLiveShareGuest() || IsCloudEnvironmentClient() || _globalOptions.GetOption(LspOptionsStorage.LspEditorFeatureFlag);

    /// <summary>
    /// Checks if the VS instance is running as a Live Share guest session.
    /// </summary>
    private static bool IsLiveShareGuest()
        => UIContext.FromUIContextGuid(s_liveShareGuestUIContextGuid).IsActive;
}
