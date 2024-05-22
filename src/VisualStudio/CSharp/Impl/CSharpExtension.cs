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

    [VisualStudioContribution]
    public static CommandGroupConfiguration ProjectCommandGroupWithPlacement
        => new(GroupPlacement.VsctParent(new Guid(VSConstants.CMDSETID.ShellMainMenu_string), VsMenus.IDM_VS_CTXT_PROJNODE, priority: 0x0400))
        {
            Children = new[]
            {
                GroupChild.Command<ResetInteractiveWindowFromProjectCommand>()
            }
        };
}
