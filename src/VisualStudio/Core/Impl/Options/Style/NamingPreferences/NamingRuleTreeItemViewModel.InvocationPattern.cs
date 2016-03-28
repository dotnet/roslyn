// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Internal.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options.Style.NamingPreferences
{
    partial class NamingRuleTreeItemViewModel : IInvocationPattern
    {
        public bool CanPreview => false;

        public IInvocationController InvocationController => NamingRuleInvocationController.Instance;

        private class NamingRuleInvocationController : IInvocationController
        {
            private static NamingRuleInvocationController s_instance;
            public static NamingRuleInvocationController Instance
            {
                get { return s_instance ?? (s_instance = new NamingRuleInvocationController()); }
            }

            public bool Invoke(IEnumerable<object> items, InputSource inputSource, bool preview)
            {
                NamingRuleTreeItemViewModel[] selectedRules = items.OfType<NamingRuleTreeItemViewModel>().ToArray();

                // When a single Naming Rule is invoked using the mouse, expanding/collapse it in the tree
                if (selectedRules.Length == 1)
                {
                    var selectedRule = selectedRules[0];
                    if (selectedRule.NamingStylesViewModel == null)
                    {
                        return false;
                    }

                    var viewModel = new NamingRuleDialogViewModel(
                        selectedRule._title,
                        selectedRule.symbolSpec,
                        selectedRule.NamingStylesViewModel.SymbolSpecificationList,
                        selectedRule.namingStyle,
                        selectedRule.NamingStylesViewModel.NamingStyleList,
                        selectedRule.parent,
                        selectedRule.NamingStylesViewModel.CreateAllowableParentList(selectedRule),
                        selectedRule.EnforcementLevel,
                        selectedRule.NamingStylesViewModel.notificationService);
                    var dialog = new NamingRuleDialog(viewModel, selectedRule.NamingStylesViewModel, selectedRule.NamingStylesViewModel.categories, selectedRule.NamingStylesViewModel.notificationService);
                    var result = dialog.ShowModal();
                    if (result == true)
                    {
                        selectedRule.namingStyle = viewModel.NamingStyleList.GetItemAt(viewModel.NamingStyleIndex) as NamingStyleViewModel;
                        selectedRule.symbolSpec = viewModel.SymbolSpecificationList.GetItemAt(viewModel.SelectedSymbolSpecificationIndex) as SymbolSpecificationViewModel;
                        selectedRule.Title = viewModel.Title;
                        selectedRule.EnforcementLevel = viewModel.EnforcementLevelsList[viewModel.EnforcementLevelIndex];
                        selectedRule.NotifyPropertyChanged(nameof(selectedRule.Text));

                        if (viewModel.ParentRuleIndex == 0)
                        {
                            if (selectedRule.Parent != selectedRule.NamingStylesViewModel.rootNamingRule)
                            {
                                selectedRule.Parent.Children.Remove(selectedRule);
                                selectedRule.NamingStylesViewModel.rootNamingRule.Children.Add(selectedRule);
                            }
                        }
                        else
                        {
                            var newParent = viewModel.ParentRuleList.GetItemAt(viewModel.ParentRuleIndex) as NamingRuleTreeItemViewModel;
                            if (newParent != selectedRule.Parent)
                            {
                                selectedRule.Parent.Children.Remove(selectedRule);
                                newParent.Children.Add(selectedRule);
                            }
                        }
                    }

                    return true;
                }

                return false;
            }
        }
    }
}
