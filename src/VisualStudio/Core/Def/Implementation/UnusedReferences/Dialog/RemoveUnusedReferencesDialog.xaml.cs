// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.UnusedReferences.Dialog
{
    /// <summary>
    /// Interaction logic for RemoveUnusedReferencesDialog.xaml
    /// </summary>
    internal partial class RemoveUnusedReferencesDialog : DialogWindow
    {
        public string RemoveUnusedReferences => ServicesVSResources.Remove_Unused_References;
        public string HelpText => ServicesVSResources.Choose_which_action_you_would_like_to_perform_on_the_unused_references;
        public string Apply => ServicesVSResources.Apply;
        public string Cancel => ServicesVSResources.Cancel;

        public RemoveUnusedReferencesDialog(FrameworkElement tableControl)
        {
            InitializeComponent();

            TablePanel.Child = tableControl;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
