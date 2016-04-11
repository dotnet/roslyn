// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.MoveType
{
    /// <summary>
    /// Interaction logic for MoveTypeDialog.xaml
    /// </summary>
    internal partial class MoveTypeDialog : DialogWindow
    {
        private readonly MoveTypeDialogViewModel _viewModel;

        // TODO: Localize
        public string MoveTypeDialogTitle { get { return "Move Type to File..."; } }
        public string Folder { get; }
        public string AddFile { get { return "Add File"; } }
        public string RemoveUnusedUsings { get { return "Remove unused usings in source file"; } }

        public MoveTypeDialog(MoveTypeDialogViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = viewModel;

            this.PreviewKeyDown += new KeyEventHandler(CloseOnEscape);
        }

        private void CloseOnEscape(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void AddFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.TrySubmit())
            {
                DialogResult = true;
            }
        }
    }
}
