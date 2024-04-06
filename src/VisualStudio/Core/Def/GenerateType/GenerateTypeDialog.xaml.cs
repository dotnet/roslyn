// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.GenerateType;

/// <summary>
/// Interaction logic for GenerateTypeDialog.xaml
/// </summary>
internal partial class GenerateTypeDialog : DialogWindow
{
    private readonly GenerateTypeDialogViewModel _viewModel;

    // Expose localized strings for binding
    public string GenerateTypeDialogTitle { get { return ServicesVSResources.Generate_Type; } }
    public string TypeDetails { get { return ServicesVSResources.Type_Details_colon; } }
    public string Access { get { return ServicesVSResources.Access_colon; } }
    public string Kind { get { return ServicesVSResources.Kind_colon; } }
    public string NameLabel { get { return ServicesVSResources.Name_colon1; } }
    public string Location { get { return ServicesVSResources.Location_colon; } }
    public string Project { get { return ServicesVSResources.Project_colon; } }
    public string FileName { get { return ServicesVSResources.File_Name_colon; } }
    public string CreateNewFile { get { return ServicesVSResources.Create_new_file; } }
    public string AddToExistingFile { get { return ServicesVSResources.Add_to_existing_file; } }
    public string OK { get { return ServicesVSResources.OK; } }
    public string Cancel { get { return ServicesVSResources.Cancel; } }

    public GenerateTypeDialog(GenerateTypeDialogViewModel viewModel)
        : base("vsl.GenerateFromUsage")
    {
        _viewModel = viewModel;
        SetCommandBindings();

        InitializeComponent();
        DataContext = viewModel;
    }

    private void SetCommandBindings()
    {
        CommandBindings.Add(new CommandBinding(
            new RoutedCommand(
                "SelectAccessKind",
                typeof(GenerateTypeDialog),
                new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.A, ModifierKeys.Alt) })),
            Select_Access_Kind));

        CommandBindings.Add(new CommandBinding(
            new RoutedCommand(
                "SelectTypeKind",
                typeof(GenerateTypeDialog),
                new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.K, ModifierKeys.Alt) })),
            Select_Type_Kind));

        CommandBindings.Add(new CommandBinding(
            new RoutedCommand(
                "SelectProject",
                typeof(GenerateTypeDialog),
                new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.P, ModifierKeys.Alt) })),
            Select_Project));

        CommandBindings.Add(new CommandBinding(
            new RoutedCommand(
                "CreateNewFile",
                typeof(GenerateTypeDialog),
                new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.C, ModifierKeys.Alt) })),
            Create_New_File));

        CommandBindings.Add(new CommandBinding(
            new RoutedCommand(
                "AddToExistingFile",
                typeof(GenerateTypeDialog),
                new InputGestureCollection(new List<InputGesture> { new KeyGesture(Key.X, ModifierKeys.Alt) })),
            Add_To_Existing_File));
    }

    private void Select_Access_Kind(object sender, RoutedEventArgs e)
        => accessListComboBox.Focus();

    private void Select_Type_Kind(object sender, RoutedEventArgs e)
        => kindListComboBox.Focus();

    private void Select_Project(object sender, RoutedEventArgs e)
        => projectListComboBox.Focus();

    private void Create_New_File(object sender, RoutedEventArgs e)
        => createNewFileRadioButton.Focus();

    private void Add_To_Existing_File(object sender, RoutedEventArgs e)
        => addToExistingFileRadioButton.Focus();

    private void FileNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        => _viewModel.UpdateFileNameExtension();

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.UpdateFileNameExtension();
        if (_viewModel.TrySubmit())
        {
            DialogResult = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor
    {
        private readonly GenerateTypeDialog _dialog;

        public TestAccessor(GenerateTypeDialog dialog)
            => _dialog = dialog;

        public Button OKButton => _dialog.OKButton;

        public Button CancelButton => _dialog.CancelButton;

        public ComboBox AccessListComboBox => _dialog.accessListComboBox;

        public ComboBox KindListComboBox => _dialog.kindListComboBox;

        public TextBox TypeNameTextBox => _dialog.TypeNameTextBox;

        public ComboBox ProjectListComboBox => _dialog.projectListComboBox;

        public RadioButton AddToExistingFileRadioButton => _dialog.addToExistingFileRadioButton;

        public ComboBox AddToExistingFileComboBox => _dialog.AddToExistingFileComboBox;

        public RadioButton CreateNewFileRadioButton => _dialog.createNewFileRadioButton;

        public ComboBox CreateNewFileComboBox => _dialog.CreateNewFileComboBox;
    }
}
