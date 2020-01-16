// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly IntellisenseTextBoxViewModel _typeIntellisenseTextBoxViewModel;
        private readonly IntellisenseTextBoxViewModel _nameIntellisenseTextBoxViewModel;
        private readonly Document _document;

        public string OK { get { return ServicesVSResources.OK; } }
        public string Cancel { get { return ServicesVSResources.Cancel; } }

        public string TypeNameLabel { get { return ServicesVSResources.Type_Name; } }

        public string ParameterNameLabel { get { return ServicesVSResources.Parameter_Name; } }

        public string CallsiteValueLabel { get { return ServicesVSResources.Callsite_Value; } }

        public string AddParameterDialogTitle { get { return ServicesVSResources.Add_Parameter; } }

        public AddParameterDialog(
            IntellisenseTextBoxViewModel typeIntellisenseTextBoxViewModel,
            IntellisenseTextBoxViewModel nameIntellisenseTextBoxViewModel,
            INotificationService notificationService,
            Document document)
        {
            // The current implementation supports Add only.
            // The dialog should be initialized the other way if called for Edit.
            ViewModel = new AddParameterDialogViewModel(notificationService);
            _typeIntellisenseTextBoxViewModel = typeIntellisenseTextBoxViewModel;
            _nameIntellisenseTextBoxViewModel = nameIntellisenseTextBoxViewModel;
            _document = document;
            this.Loaded += AddParameterDialog_Loaded;
            DataContext = ViewModel;

            InitializeComponent();
        }

        private void AddParameterDialog_Loaded(object sender, RoutedEventArgs e)
        {
            IntellisenseTextBox typeTextBox = new IntellisenseTextBox(
                _typeIntellisenseTextBoxViewModel, TypeContentControl);
            this.TypeContentControl.Content = typeTextBox;

            IntellisenseTextBox nameTextBox = new IntellisenseTextBox(
                _nameIntellisenseTextBoxViewModel, NameContentControl);
            this.NameContentControl.Content = nameTextBox;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TypeName = ((IntellisenseTextBox)TypeContentControl.Content).Text;
            ViewModel.ParameterName = ((IntellisenseTextBox)NameContentControl.Content).Text;

            if (ViewModel.TrySubmit(_document))
            {
                DialogResult = true;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void TypeNameContentControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            if (elementWithFocus is IWpfTextView)
            {
                IntellisenseTextBox typeNameTextBox = elementWithFocus.GetParentOfType<IntellisenseTextBox>();

                if (typeNameTextBox != null)
                {
                    if (e.Key == Key.Escape && !typeNameTextBox.HasActiveIntellisenseSession)
                    {
                        e.Handled = true;
                    }
                    else if (e.Key == Key.Enter && !typeNameTextBox.HasActiveIntellisenseSession)
                    {
                        // Do nothing. This case is handled in parent control KeyDown events.
                    }
                    else if (e.Key == Key.Tab && !typeNameTextBox.HasActiveIntellisenseSession)
                    {
                        // Do nothing. This case is handled in parent control KeyDown events.
                    }
                    else if (e.Key == Key.Space &&
                        (typeNameTextBox.ContainerName.Equals("NameContentControl") ||
                        (typeNameTextBox.ContainerName.Equals("TypeContentControl") && _document.Project.Language.Equals(LanguageNames.CSharp))))
                    {
                        // Do nothing. We disallow spaces in the name field for both C# and VB, and in the type field for C#.
                        e.Handled = true;
                    }
                    else
                    {
                        // Let the editor control handle the keystrokes
                        e.Handled = typeNameTextBox.HandleKeyDown();
                    }
                }
            }
        }
    }
}
