// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View
{
    /// <summary>
    /// Interaction logic for CodeStyleSeverityControl.xaml
    /// </summary>
    internal partial class CodeStyleSeverityControl : UserControl
    {
        private readonly ComboBox _comboBox;
        private readonly CodeStyleSetting _setting;

        public CodeStyleSeverityControl(CodeStyleSetting setting)
        {
            InitializeComponent();
            var refactoring = CreateGridElement(KnownMonikers.None, ServicesVSResources.Refactoring_Only);
            var suggestion = CreateGridElement(KnownMonikers.StatusInformation, ServicesVSResources.Suggestion);
            var warning = CreateGridElement(KnownMonikers.StatusWarning, ServicesVSResources.Warning);
            var error = CreateGridElement(KnownMonikers.StatusError, ServicesVSResources.Error);
            _comboBox = new ComboBox()
            {
                ItemsSource = new[]
    {
                    refactoring,
                    suggestion,
                    warning,
                    error
                }
            };

            switch (setting.Severity)
            {
                case ReportDiagnostic.Hidden:
                    _comboBox.SelectedIndex = 0;
                    break;
                case ReportDiagnostic.Info:
                    _comboBox.SelectedIndex = 1;
                    break;
                case ReportDiagnostic.Warn:
                    _comboBox.SelectedIndex = 2;
                    break;
                case ReportDiagnostic.Error:
                    _comboBox.SelectedIndex = 3;
                    break;
                default:
                    break;
            }

            _comboBox.SelectionChanged += ComboBox_SelectionChanged;

            _ = RootGrid.Children.Add(_comboBox);
            _setting = setting;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (_comboBox.SelectedIndex)
            {
                case 0:
                    _setting.ChangeSeverity(DiagnosticSeverity.Hidden);
                    return;
                case 1:
                    _setting.ChangeSeverity(DiagnosticSeverity.Info);
                    return;
                case 2:
                    _setting.ChangeSeverity(DiagnosticSeverity.Warning);
                    return;
                case 3:
                    _setting.ChangeSeverity(DiagnosticSeverity.Error);
                    return;
                default: return;
            }
        }

        private static FrameworkElement CreateGridElement(ImageMoniker imageMoniker, string text)
        {
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var block = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Text = text
            };

            if (!imageMoniker.IsNullImage())
            {
                // If we have an image and text, then create some space between them.
                block.Margin = new Thickness(5.0, 0.0, 0.0, 0.0);

                var image = new CrispImage
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Moniker = imageMoniker
                };
                image.Width = image.Height = 16.0;

                _ = stackPanel.Children.Add(image);
            }

            // Always add the textblock last so it can follow the image.
            _ = stackPanel.Children.Add(block);

            return stackPanel;
        }
    }
}
