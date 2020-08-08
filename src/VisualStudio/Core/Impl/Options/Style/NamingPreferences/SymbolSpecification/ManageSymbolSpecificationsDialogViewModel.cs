// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    internal class ManageSymbolSpecificationsDialogViewModel : AbstractNotifyPropertyChanged, IManageNamingStylesInfoDialogViewModel
    {
        private readonly INotificationService _notificationService;

        public ObservableCollection<INamingStylesInfoDialogViewModel> Items { get; set; }
        public string LanguageName { get; private set; }

        public string DialogTitle => ServicesVSResources.Manage_specifications;

        public ManageSymbolSpecificationsDialogViewModel(
            ObservableCollection<SymbolSpecification> specifications,
            List<NamingStyleOptionPageViewModel.NamingRuleViewModel> namingRules,
            string languageName,
            INotificationService notificationService)
        {
            LanguageName = languageName;
            _notificationService = notificationService;

            Items = new ObservableCollection<INamingStylesInfoDialogViewModel>(specifications.Select(specification => new SymbolSpecificationViewModel(
                languageName,
                specification,
                !namingRules.Any(rule => rule.SelectedSpecification?.ID == specification.ID),
                notificationService)));
        }

        internal void AddSymbolSpecification(INamingStylesInfoDialogViewModel _)
        {

        }

        internal void RemoveSymbolSpecification(INamingStylesInfoDialogViewModel symbolSpecification)
            => Items.Remove(symbolSpecification);

        public void AddItem()
        {
            var viewModel = new SymbolSpecificationViewModel(LanguageName, canBeDeleted: true, notificationService: _notificationService);
            var dialog = new SymbolSpecificationDialog(viewModel);
            if (dialog.ShowModal().Value == true)
            {
                Items.Add(viewModel);
            }
        }

        public void RemoveItem(INamingStylesInfoDialogViewModel item)
            => Items.Remove(item);

        public void EditItem(INamingStylesInfoDialogViewModel item)
        {
            var symbolSpecificationViewModel = (SymbolSpecificationViewModel)item;

            var symbolSpecification = ((SymbolSpecificationViewModel)item).GetSymbolSpecification();
            var viewModel = new SymbolSpecificationViewModel(LanguageName, symbolSpecification, symbolSpecificationViewModel.CanBeDeleted, _notificationService);
            var dialog = new SymbolSpecificationDialog(viewModel);
            if (dialog.ShowModal().Value == true)
            {
                symbolSpecificationViewModel.ItemName = viewModel.ItemName;
                symbolSpecificationViewModel.AccessibilityList = viewModel.AccessibilityList;
                symbolSpecificationViewModel.ModifierList = viewModel.ModifierList;
                symbolSpecificationViewModel.SymbolKindList = viewModel.SymbolKindList;
            }
        }
    }
}
