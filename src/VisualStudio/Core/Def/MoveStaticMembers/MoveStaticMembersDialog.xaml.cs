// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveStaticMembers;

/// <summary>
/// Interaction logic for MoveMembersToTypeDialog.xaml
/// </summary>
internal partial class MoveStaticMembersDialog : DialogWindow
{
    public string MoveStaticMembersDialogTitle => ServicesVSResources.Move_static_members_to_another_type_colon;
    public string DestinationLabelText => ServicesVSResources.Type_Name;
    public string OK => ServicesVSResources.OK;
    public string Cancel => ServicesVSResources.Cancel;
    public string SelectMembers => ServicesVSResources.Select_members_colon;

    public MoveStaticMembersDialogViewModel ViewModel { get; }
    public StaticMemberSelection MemberSelectionControl { get; }

    internal MoveStaticMembersDialog(MoveStaticMembersDialogViewModel viewModel)
        : base()
    {
        ViewModel = viewModel;
        DataContext = viewModel;

        MemberSelectionControl = new StaticMemberSelection(ViewModel.MemberSelectionViewModel);

        // Set focus to first tab control when the window is loaded
        Loaded += (s, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));

        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    internal TestAccessor GetTestAccessor() => new(this);

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = ViewModel.CanSubmit;
    }

    internal readonly struct TestAccessor
    {
        private readonly MoveStaticMembersDialog _dialog;
        public TestAccessor(MoveStaticMembersDialog dialog)
            => _dialog = dialog;

        public Button OKButton => _dialog.OKButton;
        public Button CancelButton => _dialog.CancelButton;

    }
}
