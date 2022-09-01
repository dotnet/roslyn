// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Windows;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    internal partial class DetailedErrorInfoDialog : DialogWindow
    {
        private readonly string errorInfo;

        internal DetailedErrorInfoDialog(string title, string errorInfo)
        {
            InitializeComponent();
            this.errorInfo = errorInfo;
            this.Title = title;
            stackTraceText.AppendText(errorInfo);
            this.CopyButton.Content = ServicesVSResources.Copy_to_Clipboard;
            this.CloseButton.Content = ServicesVSResources.Close;
        }

        private void CopyMessageToClipBoard(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Clipboard.SetText(errorInfo);
            }
            catch (Exception)
            {
                // rdpclip.exe not running in a TS session, ignore
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
            => this.Close();
    }
}
