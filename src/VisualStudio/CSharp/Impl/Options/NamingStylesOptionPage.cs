// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options;

[Guid(Guids.CSharpOptionPageNamingStyleIdString)]
internal sealed class NamingStylesOptionPage : AbstractOptionPage
{
    private NamingStyleOptionPageControl _grid;
    private INotificationService _notificationService;

    protected override AbstractOptionPageControl CreateOptionPage(IServiceProvider serviceProvider, OptionStore optionStore)
    {
        var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
        var workspace = componentModel.GetService<VisualStudioWorkspace>();
        _notificationService = workspace.Services.GetService<INotificationService>();

        _grid = new NamingStyleOptionPageControl(optionStore, _notificationService, LanguageNames.CSharp);
        return _grid;
    }

    protected override void OnDeactivate(CancelEventArgs e)
    {
        if (_grid.ContainsErrors())
        {
            e.Cancel = true;
            _notificationService.SendNotification(ServicesVSResources.Some_naming_rules_are_incomplete_Please_complete_or_remove_them);
        }

        base.OnDeactivate(e);
    }
}
