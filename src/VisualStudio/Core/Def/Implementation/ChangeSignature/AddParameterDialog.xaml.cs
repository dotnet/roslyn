// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Windows;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    /// <summary>
    /// Interaction logic for AddParameterDialog.xaml
    /// </summary>
    internal partial class AddParameterDialog : DialogWindow
    {
        private readonly AddParameterDialogViewModel _viewModel;
        private readonly Document _document;

        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }

        public string TypeNameLabel { get { return ServicesVSResources.Type_Name; } }

        public string ParameterNameLabel { get { return ServicesVSResources.Parameter_Name; } }

        public string CallSiteValueLabel { get { return ServicesVSResources.Call_site_value; } }

        public string AddParameterDialogTitle { get { return ServicesVSResources.Add_Parameter; } }

        public AddParameterDialog(AddParameterDialogViewModel viewModel)
        {
            // The current implementation supports Add only.
            // The dialog should be initialized the other way if called for Edit.
            _viewModel = viewModel;
            _document = viewModel.Document;
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
            _viewModel.ParameterName = NameContentControl.Text;
            _viewModel.CallSiteValue = CallSiteValueTextBox.Text;
            _viewModel.UpdateTypeSymbol(TypeContentControl.Text);

            if (_viewModel.TrySubmit(_document))
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
