// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveMembersToType
{
    /// <summary>
    /// Interaction logic for MoveMembersToTypeDialog.xaml
    /// </summary>
    internal partial class MoveMembersToTypeDialog : DialogWindow
    {
        private readonly MoveMembersToTypeViewModel _viewModel;

        public string MoveMembersToTypeDialogTitle => ServicesVSResources.Move_members_to_type;
        public string NamespaceLabelText => ServicesVSResources.Type_Name;
        public string OK => ServicesVSResources.OK;
        public string Cancel => ServicesVSResources.Cancel;

        internal MoveMembersToTypeDialog(MoveMembersToTypeViewModel viewModel)
            : base()
        {
            _viewModel = viewModel;

            // Set focus to first tab control when the window is loaded
            Loaded += (s, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

            InitializeComponent();
            DataContext = viewModel;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        internal TestAccessor GetTestAccessor() => new(this);

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.CanSubmit)
            {
                DialogResult = true;
            }
        }

        internal readonly struct TestAccessor
        {
            private readonly MoveToNamespaceDialog _dialog;
            public TestAccessor(MoveToNamespaceDialog dialog)
                => _dialog = dialog;

            public Button OKButton => _dialog.OKButton;
            public Button CancelButton => _dialog.CancelButton;
            public ComboBox NamespaceBox => _dialog.NamespaceBox;

        }
    }
}
