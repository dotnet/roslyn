// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    /// <summary>
    /// Interaction logic for AddParameterDialog.xaml
    /// </summary>
    internal partial class AddParameterDialog : DialogWindow
    {
        private readonly AddParameterDialogViewModel _viewModel;

        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }
        public string TypeNameLabel { get { return ServicesVSResources.Type_Name; } }
        public string ParameterNameLabel { get { return ServicesVSResources.Parameter_Name; } }
        public string CallSiteValueLabel { get { return ServicesVSResources.Call_site_value; } }
        public string AddParameterDialogTitle { get { return ServicesVSResources.Add_Parameter; } }
        public string ParameterKind { get { return ServicesVSResources.Parameter_kind_colon; } }
        public string Required { get { return ServicesVSResources.Required; } }
        public string OptionalWithDefaultValue { get { return ServicesVSResources.Optional_with_default_value_colon; } }
        public string ValueToInjectAtCallsites { get { return ServicesVSResources.Value_to_inject_at_call_sites_colon; } }
        public string Value { get { return ServicesVSResources.Value_colon; } }
        public string UseNamedArgument { get { return ServicesVSResources.Use_named_argument; } }
        public string IntroduceUndefinedTodoVariables { get { return ServicesVSResources.IntroduceUndefinedTodoVariables; } }
        public string OmitOnlyForOptionalParameters { get { return ServicesVSResources.Omit_only_for_optional_parameters; } }

        public AddParameterDialog(AddParameterDialogViewModel viewModel)
        {
            _viewModel = viewModel;
            this.Loaded += AddParameterDialog_Loaded;
            DataContext = _viewModel;

            InitializeComponent();
        }

        private void AddParameterDialog_Loaded(object sender, RoutedEventArgs e)
        {
            MinHeight = Height;
            TypeContentControl.Focus();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.UpdateTypeSymbol(TypeContentControl.Text);
            _viewModel.ParameterName = NameContentControl.Text;

            _viewModel.IsRequired = RequiredParameterRadioButton.IsChecked ?? false;
            _viewModel.DefaultValue = _viewModel.IsRequired ? "" : DefaultValue.Text;

            _viewModel.IsCallsiteError = IntroduceErrorRadioButton.IsChecked ?? false;
            _viewModel.IsCallsiteOmitted = OmitArgumentRadioButton.IsChecked ?? false;

            _viewModel.CallSiteValue = CallsiteValueTextBox.Text;
            _viewModel.UseNamedArguments = UseNamedArgumentButton.IsChecked ?? false;

            if (_viewModel.TrySubmit(_viewModel.Document))
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly AddParameterDialog _dialog;

            public TestAccessor(AddParameterDialog dialog)
            {
                _dialog = dialog;
            }

            public DialogButton OKButton => _dialog.OKButton;

            public DialogButton CancelButton => _dialog.CancelButton;
        }
    }
}
