// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.  

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PullMemberUp
{
    /// <summary>
    /// Interaction logic for PullhMemberUpDialogxaml.xaml
    /// </summary>
    internal partial class PullMemberUpDialogxaml : DialogWindow
    {
        public string OK => ServicesVSResources.OK;

        public string Cancel => ServicesVSResources.Cancel;

        // TODO: Add this to resources mananger
        public string PullMembersUpTitle => "Pull Up Members";

        private PullMemberUpViewModel ViewModel { get; }

        internal PullMemberUpDialogxaml(PullMemberUpViewModel pullMemberUpViewModel)
        {
            ViewModel = pullMemberUpViewModel;
            DataContext = ViewModel;
            InitializeComponent();
        }

        private void TargetMembersContainer_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TargetMembersContainer.SelectedItem is MemberSymbolViewModelGraphNode memberGraphNode)
            {
                ViewModel.SelectedTarget = memberGraphNode;
                if (memberGraphNode.MemberSymbolViewModel.MemberSymbol is INamedTypeSymbol interfaceSymbol && interfaceSymbol.TypeKind == TypeKind.Interface)
                {
                    DisableFieldCheckBox();
                    DisableAbstractBox();
                }
                else
                {
                    EnableFieldChekcBox();
                    EnableAbstractBox();
                }
            }
        }

        private void DisableAbstractBox()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                member.IsAbstractSelectable = false;
                member.IsAbstract = false;
            }
        }

        private void EnableAbstractBox()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.Kind != SymbolKind.Field && !member.MemberSymbol.IsAbstract)
                {
                    member.IsAbstractSelectable = true;
                }
            }
        }

        private void DisableFieldCheckBox()
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.Kind == SymbolKind.Field)
                {
                    member.IsChecked = false;
                    member.IsSelectable = false;
                }
            }
        }

        private void EnableFieldChekcBox()
        {
           foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.Kind == SymbolKind.Field)
                {
                    member.IsChecked = false;
                    member.IsSelectable = true;
                }
            }
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedMembers = ViewModel.SelectedMembersContainer.
                Where(memberSymbolView => memberSymbolView.IsChecked).
                Select(memberSymbolView => memberSymbolView.MemberSymbol);
            if (ViewModel.SelectedTarget != null && selectedMembers.Count() != 0)
            {
                DialogResult = true;
            }
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void SelecDependentsButton_Click(object sender, RoutedEventArgs e)
        {
            var checkedMembers = ViewModel.SelectedMembersContainer.
                Where(member => member.IsChecked &&
                      member.MemberSymbol.Kind != SymbolKind.Field &&
                      member.MemberSymbol.Kind != SymbolKind.Event);
            
            foreach (var member in checkedMembers)
            {
                var dependents = ViewModel.LazyDependentsMap[member.MemberSymbol].Value;
                SelectSymbols(dependents);
            }
        }

        private void SelectSymbols(IEnumerable<ISymbol> members)
        {
            foreach (var member in members)
            {
                // TODO: create a hash map to do the mapping
                var index = ViewModel.SelectedMembersContainer.Select(symbolView => symbolView.MemberSymbol).ToList().IndexOf(member);
                ViewModel.SelectedMembersContainer[index].IsChecked = true;
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.IsSelectable)
                {
                    member.IsChecked = true;
                }
            }
        }

        private void SelectPublic_Click(object sender, RoutedEventArgs e)
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                if (member.MemberSymbol.DeclaredAccessibility == Accessibility.Public)
                {
                    member.IsChecked = true;
                }
            }
        }

        private void DeselectedAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var member in ViewModel.SelectedMembersContainer)
            {
                member.IsChecked = false;
            }
        }
    }
}
