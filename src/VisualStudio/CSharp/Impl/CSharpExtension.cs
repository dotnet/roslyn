// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Commands;
using Microsoft.VisualStudio.LanguageServices.CSharp.Interactive;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices;

[VisualStudioContribution]
internal sealed class CSharpExtension : Extension
{
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        RequiresInProcessHosting = true,
    };

    /// <summary>
    /// Can't reference non-constant <see cref="Shell.VsMenus.guidSHLMainMenu"/> member from command configuration:
    /// </summary>    
    private static readonly Guid s_guidSHLMainMenu = new(0xd309f791, 0x903f, 0x11d0, 0x9e, 0xfc, 0x00, 0xa0, 0xc9, 0x11, 0x00, 0x4f);

    [VisualStudioContribution]
    public static CommandGroupConfiguration ProjectCommandGroupWithPlacement
        => new(GroupPlacement.VsctParent(s_guidSHLMainMenu, VsMenus.IDM_VS_CTXT_PROJNODE, 0x0400))
        {
            Children = new[]
            {
                GroupChild.Command<ResetInteractiveWindowFromProjectCommand>()
            }
        };
}
