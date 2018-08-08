// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.Win32;

namespace Roslyn.Hosting.Diagnostics.RemoteHost
{
    /// <summary>
    /// Interaction logic for RemoteHostPanel.xaml
    /// </summary>
    public partial class RemoteHostPanel : UserControl
    {
        private readonly Workspace _workspace;

        public RemoteHostPanel()
        {
            InitializeComponent();
        }

        public RemoteHostPanel(Workspace workspace) : this()
        {
            _workspace = workspace;
        }

        private async void OnSolutionSave(object sender, RoutedEventArgs e)
        {
            using (Disable(SolutionSaveButton))
            {
                string fileName = null;

                while (true)
                {
                    fileName = FileToSave.Content as string;
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        OnFileToSave(sender, e);
                    }
                    else
                    {
                        break;
                    }
                }

                // save from BG
                var checksum = await Task.Run(
                    () => SolutionAssetManager.SaveAsync(fileName, _workspace.CurrentSolution, CancellationToken.None)).ConfigureAwait(true);

                SaveChecksum.Content = checksum.ToString();
            }
        }

        private void OnFileToSave(object sender, RoutedEventArgs e)
        {
            using (Disable(FileToSaveButton))
            {
                var dialog = new SaveFileDialog();
                if (dialog.ShowDialog() == true)
                {
                    FileToSave.Content = dialog.FileName;
                }
            }
        }

        private async void OnSolutionOpen(object sender, RoutedEventArgs e)
        {
            using (Disable(SolutionOpenButton))
            {
                string fileName = null;

                while (true)
                {
                    fileName = FileToOpen.Content as string;
                    if (string.IsNullOrWhiteSpace(fileName))
                    {
                        OnFileToOpen(sender, e);
                    }
                    else
                    {
                        break;
                    }
                }

                // load from BG
                var solution = await Task.Run(
                    () => SolutionAssetManager.LoadAsync(fileName, _workspace.CurrentSolution, CancellationToken.None)).ConfigureAwait(true);

                OpenChecksum.Content = await solution.State.GetChecksumAsync(CancellationToken.None).ConfigureAwait(true);
            }
        }

        private void OnFileToOpen(object sender, RoutedEventArgs e)
        {
            using (Disable(FileToOpenButton))
            {
                var dialog = new OpenFileDialog();
                if (dialog.ShowDialog() == true)
                {
                    FileToOpen.Content = dialog.FileName;
                }
            }
        }

        private IDisposable Disable(UIElement control)
        {
            control.IsEnabled = false;
            return new RAII(() => control.IsEnabled = true);
        }

        private sealed class RAII : IDisposable
        {
            private readonly Action _action;

            public RAII(Action disposeAction)
            {
                _action = disposeAction;
            }
            public void Dispose()
            {
                _action?.Invoke();
            }
        }
    }
}
