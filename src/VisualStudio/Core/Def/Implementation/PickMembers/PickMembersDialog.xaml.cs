﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.PickMembers
{
    /// <summary>
    /// Interaction logic for ExtractInterfaceDialog.xaml
    /// </summary>
    internal partial class PickMembersDialog : DialogWindow
    {
        private readonly PickMembersDialogViewModel _viewModel;

        // Expose localized strings for binding
        public string PickMembersDialogTitle => ServicesVSResources.Pick_members;
        public string PickMembersTitle { get; }

        public string SelectAll => ServicesVSResources.Select_All;
        public string DeselectAll => ServicesVSResources.Deselect_All;
        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;

        internal PickMembersDialog(PickMembersDialogViewModel viewModel, string title)
        {
            PickMembersTitle = title;
            _viewModel = viewModel;
            SetCommandBindings();

            InitializeComponent();
            DataContext = viewModel;
        }

        private void SetCommandBindings()
        {
            CommandBindings.Add(new CommandBinding(
                new RoutedCommand(
                    "SelectAllClickCommand",
                    typeof(PickMembersDialog),
                    new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.S, ModifierKeys.Alt) })),
                Select_All_Click));

            CommandBindings.Add(new CommandBinding(
                new RoutedCommand(
                    "DeselectAllClickCommand",
                    typeof(PickMembersDialog),
                    new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.D, ModifierKeys.Alt) })),
                Deselect_All_Click));
        }

        private void OK_Click(object sender, RoutedEventArgs e)
            => DialogResult = true;

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void Select_All_Click(object sender, RoutedEventArgs e)
            => _viewModel.SelectAll();

        private void Deselect_All_Click(object sender, RoutedEventArgs e)
            => _viewModel.DeselectAll();

        private void MoveUp_Click(object sender, EventArgs e)
        {
            int oldSelectedIndex = Members.SelectedIndex;
            if (_viewModel.CanMoveUp && oldSelectedIndex >= 0)
            {
                _viewModel.MoveUp();
                Members.Items.Refresh();
                Members.SelectedIndex = oldSelectedIndex - 1;
            }

            SetFocusToSelectedRow();
        }

        private void MoveDown_Click(object sender, EventArgs e)
        {
            int oldSelectedIndex = Members.SelectedIndex;
            if (_viewModel.CanMoveDown && oldSelectedIndex >= 0)
            {
                _viewModel.MoveDown();
                Members.Items.Refresh();
                Members.SelectedIndex = oldSelectedIndex + 1;
            }

            SetFocusToSelectedRow();
        }

        private void SetFocusToSelectedRow()
        {
            if (Members.SelectedIndex >= 0)
            {
                var row = Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) as ListViewItem;
                if (row == null)
                {
                    Members.ScrollIntoView(Members.SelectedItem);
                    row = Members.ItemContainerGenerator.ContainerFromIndex(Members.SelectedIndex) as ListViewItem;
                }

                row?.Focus();
            }
        }

        private void OnListViewPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
                ToggleCheckSelection();
                e.Handled = true;
            }
        }

        private void OnListViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                ToggleCheckSelection();
                e.Handled = true;
            }
        }

        private void ToggleCheckSelection()
        {
            var selectedItems = Members.SelectedItems.OfType<PickMembersDialogViewModel.MemberSymbolViewModel>().ToArray();
            var allChecked = selectedItems.All(m => m.IsChecked);
            foreach (var item in selectedItems)
            {
                item.IsChecked = !allChecked;
            }
        }
    }
}
