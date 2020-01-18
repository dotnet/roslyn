// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.IntellisenseControls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Notification;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ChangeSignature
{
    /// <summary>
    /// Interaction logic for AddParameterDialog.xaml
    /// </summary>
    internal partial class AddParameterDialog : DialogWindow
    {
        public readonly AddParameterDialogViewModel ViewModel;
        private readonly Task<ChangeSignatureIntellisenseTextBoxesViewModel?> _createIntellisenseTextBoxViewModelsTask;
        private readonly Document _document;

        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }

        public string TypeNameLabel { get { return ServicesVSResources.Type_Name; } }

        public string ParameterNameLabel { get { return ServicesVSResources.Parameter_Name; } }

        public string CallSiteValueLabel { get { return ServicesVSResources.Call_site_value; } }

        public string AddParameterDialogTitle { get { return ServicesVSResources.Add_Parameter; } }

        public AddParameterDialog(
            Task<ChangeSignatureIntellisenseTextBoxesViewModel?> createViewModelsTask,
            INotificationService? notificationService,
            Document document)
        {
            // The current implementation supports Add only.
            // The dialog should be initialized the other way if called for Edit.
            ViewModel = new AddParameterDialogViewModel(notificationService);
            _createIntellisenseTextBoxViewModelsTask = createViewModelsTask;

            _document = document;
            this.Loaded += AddParameterDialog_Loaded;
            DataContext = ViewModel;

            InitializeComponent();
        }

        private void AddParameterDialog_Loaded(object sender, RoutedEventArgs e)
        {
            var viewModels = _createIntellisenseTextBoxViewModelsTask.Result;

            if (viewModels != null)
            {
                this.TypeContentControl.Content = new IntellisenseTextBox(
                    viewModels.Value.TypeIntellisenseTextBoxViewModel, TypeContentControl);

                this.NameContentControl.Content = new IntellisenseTextBox(
                    viewModels.Value.NameIntellisenseTextBoxViewModel, NameContentControl);
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TypeName = ((IntellisenseTextBox)TypeContentControl.Content).Text;
            ViewModel.ParameterName = ((IntellisenseTextBox)NameContentControl.Content).Text;
            ViewModel.CallSiteValue = CallSiteValueTextBox.Text;

            if (ViewModel.TrySubmit(_document))
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TypeOrNameContentControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            UIElement? elementWithFocus = Keyboard.FocusedElement as UIElement;

            if (elementWithFocus is IWpfTextView)
            {
                IntellisenseTextBox typeOrNameTextBox = elementWithFocus.GetParentOfType<IntellisenseTextBox>();

                if (typeOrNameTextBox != null)
                {
                    if (e.Key == Key.Escape && !typeOrNameTextBox.HasActiveIntellisenseSession)
                    {
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Enter && !typeOrNameTextBox.HasActiveIntellisenseSession)
                    {
                        // Do nothing. This case is handled in parent control KeyDown events.
                    }
                    else if (e.Key == Key.Tab && !typeOrNameTextBox.HasActiveIntellisenseSession)
                    {
                        // Do nothing. This case is handled in parent control KeyDown events.
                    }
                    else if (e.Key == Key.Space &&
                        (typeOrNameTextBox.ContainerName.Equals("NameContentControl") ||
                        (typeOrNameTextBox.ContainerName.Equals("TypeContentControl") && _document.Project.Language.Equals(LanguageNames.CSharp))))
                    {
                        // Do nothing. We disallow spaces in the name field for both C# and VB, and in the type field for C#.
                        e.Handled = true;
                    }
                    else
                    {
                        // Let the editor control handle the keystrokes
                        e.Handled = typeOrNameTextBox.HandleKeyDown();
                    }
                }
            }
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
