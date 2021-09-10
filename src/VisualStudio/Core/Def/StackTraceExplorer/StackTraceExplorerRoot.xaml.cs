// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Microsoft.VisualStudio.LanguageServices.StackTraceExplorer
{

    /// <summary>
    /// Interaction logic for CallstackExplorerRoot.xaml
    /// </summary>
    internal partial class StackTraceExplorerRoot : UserControl
    {
        public string EmptyText => ServicesVSResources.Paste_valid_stack_trace;

        public StackTraceExplorerRoot()
        {
            InitializeComponent();
            DataObject.AddPastingHandler(this, OnPaste);
        }

        private void CommandBinding_OnPaste(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
            => OnPaste();

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
            => OnPaste();

        private void OnPaste()
        {
            if (RootGrid.Children.Count == 0)
            {
                return;
            }

            var content = RootGrid.Children[0];
            if (content is StackTraceExplorer explorer)
            {
                explorer.OnPaste();
            }
        }

        public void SetChild(FrameworkElement? child)
        {
            RootGrid.Children.Clear();

            if (child is null)
            {
                RootGrid.Children.Add(EmptyTextMessage);
                EmptyTextMessage.Visibility = Visibility.Visible;
            }
            else
            {
                Grid.SetRow(child, 0);
                EmptyTextMessage.Visibility = Visibility.Collapsed;
                RootGrid.Children.Add(child);
            }
        }
    }
}
