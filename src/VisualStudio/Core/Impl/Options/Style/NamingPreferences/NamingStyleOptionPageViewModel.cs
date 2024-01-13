// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.NamingStyles;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style
{
    internal class NamingStyleOptionPageViewModel : AbstractNotifyPropertyChanged
    {
        public string ManageSpecificationsButtonText => ServicesVSResources.Manage_specifications;
        public string ManageStylesButtonText => ServicesVSResources.Manage_naming_styles;

        private readonly NotificationOptionViewModel[] _notifications =
        [
            new NotificationOptionViewModel(NotificationOption2.Silent, KnownMonikers.None),
            new NotificationOptionViewModel(NotificationOption2.Suggestion, KnownMonikers.StatusInformation),
            new NotificationOptionViewModel(NotificationOption2.Warning, KnownMonikers.StatusWarning),
            new NotificationOptionViewModel(NotificationOption2.Error, KnownMonikers.StatusError)
        ];

        public string CodeStyleMembersAutomationText => ServicesVSResources.Naming_rules;

        public ObservableCollection<NamingRuleViewModel> CodeStyleItems { get; set; }
        public ObservableCollection<SymbolSpecification> Specifications { get; set; }
        public ObservableCollection<MutableNamingStyle> NamingStyles { get; set; }

        public NamingStyleOptionPageViewModel(NamingStylePreferences info)
        {
            var viewModels = new List<NamingRuleViewModel>();
            foreach (var namingRule in info.NamingRules)
            {
                var viewModel = new NamingRuleViewModel()
                {
                    NamingStyles = new ObservableCollection<MutableNamingStyle>(info.NamingStyles.Select(n => new MutableNamingStyle(n))),
                    Specifications = new ObservableCollection<SymbolSpecification>(info.SymbolSpecifications),
                    NotificationPreferences = new List<NotificationOptionViewModel>(_notifications)
                };

                viewModel.SelectedSpecification = viewModel.Specifications.Single(s => s.ID == namingRule.SymbolSpecificationID);
                viewModel.SelectedStyle = viewModel.NamingStyles.Single(s => s.ID == namingRule.NamingStyleID);
                viewModel.SelectedNotificationPreference = viewModel.NotificationPreferences.Single(n => n.Notification.Severity == namingRule.EnforcementLevel);

                viewModels.Add(viewModel);
            }

            CodeStyleItems = new ObservableCollection<NamingRuleViewModel>(viewModels);
            Specifications = new ObservableCollection<SymbolSpecification>(info.SymbolSpecifications);
            NamingStyles = new ObservableCollection<MutableNamingStyle>(info.NamingStyles.Select(n => new MutableNamingStyle(n)));

            SetMoveArrowStatuses();
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get
            {
                return _selectedIndex;
            }

            set
            {
                if (value == _selectedIndex)
                {
                    return;
                }

                _selectedIndex = value;
            }
        }

        internal void AddItem(NamingRuleViewModel namingRuleViewModel)
        {
            CodeStyleItems.Add(namingRuleViewModel);
            SetMoveArrowStatuses();
        }

        internal void RemoveItem(NamingRuleViewModel namingRuleViewModel)
        {
            CodeStyleItems.Remove(namingRuleViewModel);
            SetMoveArrowStatuses();
        }

        internal void UpdateSpecificationList(ManageSymbolSpecificationsDialogViewModel viewModel)
        {
            var symbolSpecifications = viewModel.Items.Cast<SymbolSpecificationViewModel>().Select(n => new SymbolSpecification(
                n.ID,
                n.ItemName,
                n.SymbolKindList.Where(s => s.IsChecked).Select(k => k.CreateSymbolOrTypeOrMethodKind()).ToImmutableArray(),
                n.AccessibilityList.Where(s => s.IsChecked).Select(a => a._accessibility).ToImmutableArray(),
                n.ModifierList.Where(s => s.IsChecked).Select(m => new SymbolSpecification.ModifierKind(m._modifier)).ToImmutableArray()));

            Specifications.Clear();
            foreach (var specification in symbolSpecifications)
            {
                Specifications.Add(specification);
            }

            // The existing rules have had their Specifications pulled out from underneath them, so
            // this goes through and resets them.

            foreach (var rule in CodeStyleItems)
            {
                var selectedSpecification = rule.SelectedSpecification;

                rule.Specifications.Clear();
                foreach (var specification in symbolSpecifications)
                {
                    rule.Specifications.Add(specification);
                }

                // Set the SelectedSpecification to null and then back to the actual selected 
                // specification to trigger the INotifyPropertyChanged event.

                rule.SelectedSpecification = null;

                if (selectedSpecification != null)
                {
                    rule.SelectedSpecification = rule.Specifications.Single(s => s.ID == selectedSpecification.ID);
                }
            }
        }

        internal void MoveItem(int oldSelectedIndex, int newSelectedIndex)
        {
            CodeStyleItems.Move(oldSelectedIndex, newSelectedIndex);
            SetMoveArrowStatuses();
        }

        private void SetMoveArrowStatuses()
        {
            for (var i = 0; i < CodeStyleItems.Count; i++)
            {
                CodeStyleItems[i].CanMoveUp = true;
                CodeStyleItems[i].CanMoveDown = true;

                if (i == 0)
                {
                    CodeStyleItems[i].CanMoveUp = false;
                }

                if (i == CodeStyleItems.Count - 1)
                {
                    CodeStyleItems[i].CanMoveDown = false;
                }
            }
        }

        internal void UpdateStyleList(ManageNamingStylesDialogViewModel viewModel)
        {
            var namingStyles = viewModel.Items.Cast<NamingStyleViewModel>().Select(n => new MutableNamingStyle(
                new NamingStyle(
                    id: n.ID,
                    name: n.ItemName,
                    prefix: n.RequiredPrefix,
                    suffix: n.RequiredSuffix,
                    wordSeparator: n.WordSeparator,
                    capitalizationScheme: n.CapitalizationSchemes[n.CapitalizationSchemeIndex].Capitalization)));

            NamingStyles.Clear();
            foreach (var style in namingStyles)
            {
                NamingStyles.Add(style);
            }

            // The existing rules have had their Styles pulled out from underneath them, so
            // this goes through and resets them.

            foreach (var rule in CodeStyleItems)
            {
                var selectedStyle = rule.SelectedStyle;

                rule.NamingStyles.Clear();
                foreach (var style in namingStyles)
                {
                    rule.NamingStyles.Add(style);
                }

                // Set the SelectedStyle to null and then back to the actual selected 
                // style to trigger the INotifyPropertyChanged event.

                rule.SelectedStyle = null;

                if (selectedStyle != null)
                {
                    rule.SelectedStyle = rule.NamingStyles.Single(n => n.ID == selectedStyle.ID);
                }
            }
        }

        internal class NamingRuleViewModel : AbstractNotifyPropertyChanged
        {
            public NamingRuleViewModel()
            {
                Specifications = new ObservableCollection<SymbolSpecification>();
                NamingStyles = new ObservableCollection<MutableNamingStyle>();
                NotificationPreferences = new List<NotificationOptionViewModel>();
            }

            private SymbolSpecification _selectedSpecification;
            private MutableNamingStyle _selectedNamingStyle;
            private NotificationOptionViewModel _selectedNotification;

            public ObservableCollection<SymbolSpecification> Specifications { get; set; }
            public ObservableCollection<MutableNamingStyle> NamingStyles { get; set; }
            public IEnumerable<NotificationOptionViewModel> NotificationPreferences { get; set; }

            public SymbolSpecification SelectedSpecification
            {
                get
                {
                    return _selectedSpecification;
                }
                set
                {
                    SetProperty(ref _selectedSpecification, value);
                }
            }

            public MutableNamingStyle SelectedStyle
            {
                get
                {
                    return _selectedNamingStyle;
                }
                set
                {
                    SetProperty(ref _selectedNamingStyle, value);
                }
            }
            public NotificationOptionViewModel SelectedNotificationPreference
            {
                get
                {
                    return _selectedNotification;
                }

                set
                {
                    SetProperty(ref _selectedNotification, value);
                }
            }

            private bool _canMoveUp;
            public bool CanMoveUp
            {
                get
                {
                    return _canMoveUp;
                }

                set
                {
                    SetProperty(ref _canMoveUp, value);
                }
            }

            private bool _canMoveDown;
            public bool CanMoveDown
            {
                get
                {
                    return _canMoveDown;
                }

                set
                {
                    SetProperty(ref _canMoveDown, value);
                }
            }

            public string MoveUpAutomationText => ServicesVSResources.Move_up;
            public string MoveDownAutomationText => ServicesVSResources.Move_down;

            public string RemoveAutomationText => ServicesVSResources.Remove;

            public bool IsComplete()
                => SelectedSpecification != null && SelectedStyle != null && SelectedNotificationPreference != null;

            // For screen readers
            public override string ToString() => ServicesVSResources.Naming_Rule;
        }
    }
}
