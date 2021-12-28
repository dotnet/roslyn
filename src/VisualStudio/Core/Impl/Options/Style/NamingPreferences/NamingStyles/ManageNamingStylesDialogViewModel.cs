// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    internal class ManageNamingStylesDialogViewModel : AbstractNotifyPropertyChanged, IManageNamingStylesInfoDialogViewModel
    {
        private readonly INotificationService _notificationService;

        public ObservableCollection<INamingStylesInfoDialogViewModel> Items { get; set; }

        public string DialogTitle => ServicesVSResources.Manage_naming_styles;

        public ManageNamingStylesDialogViewModel(
            ObservableCollection<MutableNamingStyle> namingStyles,
            List<NamingStyleOptionPageViewModel.NamingRuleViewModel> namingRules,
            INotificationService notificationService)
        {
            _notificationService = notificationService;

            Items = new ObservableCollection<INamingStylesInfoDialogViewModel>(namingStyles.Select(style => new NamingStyleViewModel(
                style.Clone(),
                !namingRules.Any(rule => rule.SelectedStyle?.ID == style.ID),
                notificationService)));
        }

        internal void RemoveNamingStyle(NamingStyleViewModel namingStyle)
            => Items.Remove(namingStyle);

        public void AddItem()
        {
            var style = new MutableNamingStyle();
            var viewModel = new NamingStyleViewModel(style, canBeDeleted: true, notificationService: _notificationService);
            var dialog = new NamingStyleDialog(viewModel);

            if (dialog.ShowModal().Value == true)
            {
                Items.Add(viewModel);
            }
        }

        public void RemoveItem(INamingStylesInfoDialogViewModel item)
            => Items.Remove(item);

        public void EditItem(INamingStylesInfoDialogViewModel item)
        {
            var context = (NamingStyleViewModel)item;

            var style = context.GetNamingStyle();
            var viewModel = new NamingStyleViewModel(style, context.CanBeDeleted, notificationService: _notificationService);
            var dialog = new NamingStyleDialog(viewModel);

            if (dialog.ShowModal().Value == true)
            {
                context.ItemName = viewModel.ItemName;
                context.RequiredPrefix = viewModel.RequiredPrefix;
                context.RequiredSuffix = viewModel.RequiredSuffix;
                context.WordSeparator = viewModel.WordSeparator;
                context.CapitalizationSchemeIndex = viewModel.CapitalizationSchemeIndex;
            }
        }
    }
}
