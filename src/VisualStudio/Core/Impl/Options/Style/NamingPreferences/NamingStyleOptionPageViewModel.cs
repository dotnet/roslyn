using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style
{
    internal class NamingStyleOptionPageViewModel : AbstractNotifyPropertyChanged
    {
        public string ManageSpecificationsButtonText => ServicesVSResources.Manage_specifications;
        public string ManageStylesButtonText => ServicesVSResources.Manage_styles;

        private readonly EnforcementLevel[] _notifications = new[]
        {
            new EnforcementLevel(DiagnosticSeverity.Hidden),
            new EnforcementLevel(DiagnosticSeverity.Info),
            new EnforcementLevel(DiagnosticSeverity.Warning),
            new EnforcementLevel(DiagnosticSeverity.Error),
        };

        public ObservableCollection<NamingRuleViewModel> CodeStyleItems { get; set; }
        public ObservableCollection<SymbolSpecification> Specifications { get; set; }
        public ObservableCollection<NamingStyle> NamingStyles { get; set; }

        public NamingStyleOptionPageViewModel(SerializableNamingStylePreferencesInfo info)
        {
            var viewModels = new List<NamingRuleViewModel>();
            foreach (var namingRule in info.NamingRules)
            {
                var viewModel = new NamingRuleViewModel();

                viewModel.NamingStyles = new ObservableCollection<NamingStyle>(info.NamingStyles);
                viewModel.Specifications = new ObservableCollection<SymbolSpecification>(info.SymbolSpecifications);
                viewModel.NotificationPreferences = new List<EnforcementLevel>(_notifications);

                viewModel.SelectedSpecification = viewModel.Specifications.Single(s => s.ID == namingRule.SymbolSpecificationID);
                viewModel.SelectedStyle= viewModel.NamingStyles.Single(s => s.ID == namingRule.NamingStyleID);
                viewModel.SelectedNotificationPreference = viewModel.NotificationPreferences.Single(n => n.Name == new EnforcementLevel(namingRule.EnforcementLevel).Name);
                
                viewModels.Add(viewModel);
            }

            CodeStyleItems = new ObservableCollection<NamingRuleViewModel>(viewModels);
            Specifications = new ObservableCollection<SymbolSpecification>(info.SymbolSpecifications);
            NamingStyles = new ObservableCollection<NamingStyle>(info.NamingStyles);

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
                n.SymbolKindList.Where(s => s.IsChecked).Select(k => k.CreateSymbolKindOrTypeKind()).ToList(),
                n.AccessibilityList.Where(s => s.IsChecked).Select(a => new SymbolSpecification.AccessibilityKind(a._accessibility)).ToList(),
                n.ModifierList.Where(s => s.IsChecked).Select(m => new SymbolSpecification.ModifierKind(m._modifier)).ToList()));

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
            for (int i = 0; i < CodeStyleItems.Count; i++)
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
            var namingStyles = viewModel.Items.Cast<NamingStyleViewModel>().Select(n => new NamingStyle
            {
                ID = n.ID,
                Name = n.ItemName,
                Prefix = n.RequiredPrefix,
                Suffix = n.RequiredSuffix,
                WordSeparator = n.WordSeparator,
                CapitalizationScheme = n.CapitalizationSchemes[n.CapitalizationSchemeIndex].Capitalization
            });

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
                NamingStyles = new ObservableCollection<NamingStyle>();
                NotificationPreferences = new List<EnforcementLevel>();
            }

            private SymbolSpecification _selectedSpecification;
            private NamingStyle _selectedNamingStyle;
            private EnforcementLevel _selectedNotification;

            public ObservableCollection<SymbolSpecification> Specifications { get; set; }
            public ObservableCollection<NamingStyle> NamingStyles { get; set; }
            public IEnumerable<EnforcementLevel> NotificationPreferences { get; set; }

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

            public NamingStyle SelectedStyle
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
            public EnforcementLevel SelectedNotificationPreference
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

            public bool IsComplete()
            {
                return SelectedSpecification != null && SelectedStyle != null && SelectedNotificationPreference != null;
            }
        }
    }
}
