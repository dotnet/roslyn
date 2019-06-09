// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    [Guid(Guids.CSharpOptionPageNamingStyleIdString)]
    internal class NamingStylesOptionPage : AbstractOptionPage
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
}
