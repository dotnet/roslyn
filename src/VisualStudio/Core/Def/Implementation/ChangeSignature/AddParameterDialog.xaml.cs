// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        public string ParameterInformation { get { return ServicesVSResources.Parameter_information; } }
        public string TypeNameLabel { get { return ServicesVSResources.Type_Name; } }
        public string ParameterNameLabel { get { return ServicesVSResources.Parameter_Name; } }
        public string CallSiteValueLabel { get { return ServicesVSResources.Call_site_value; } }
        public string AddParameterDialogTitle { get { return ServicesVSResources.Add_Parameter; } }
        public string ParameterKind { get { return ServicesVSResources.Parameter_kind; } }
        public string Required { get { return ServicesVSResources.Required; } }
        public string OptionalWithDefaultValue { get { return ServicesVSResources.Optional_with_default_value_colon; } }
        public string ValueToInjectAtCallsites { get { return ServicesVSResources.Value_to_inject_at_call_sites; } }
        public string Value { get { return ServicesVSResources.Value_colon; } }
        public string UseNamedArgument { get { return ServicesVSResources.Use_named_argument; } }
        public string IntroduceUndefinedTodoVariables { get { return ServicesVSResources.IntroduceUndefinedTodoVariables; } }
        public string OmitOnlyForOptionalParameters { get { return ServicesVSResources.Omit_only_for_optional_parameters; } }
        public string InferFromContext { get { return ServicesVSResources.Infer_from_context; } }

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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            // Workaround WPF bug: https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1101094
            DataContext = null;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TrySubmit())
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

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
