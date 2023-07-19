// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Options
{
    internal partial class GridOptionPreviewControl : AbstractOptionPageControl
    {
        private const string UseEditorConfigUrl = "https://go.microsoft.com/fwlink/?linkid=866541";
        internal AbstractOptionPreviewViewModel ViewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly Func<OptionStore, IServiceProvider, AbstractOptionPreviewViewModel> _createViewModel;
        private readonly ImmutableArray<(string feature, ImmutableArray<IOption2> options)> _groupedEditorConfigOptions;
        private readonly string _language;

        public static readonly Uri CodeStylePageHeaderLearnMoreUri = new Uri(UseEditorConfigUrl);
        public static string CodeStylePageHeader => ServicesVSResources.Code_style_header_use_editor_config;
        public static string CodeStylePageHeaderLearnMoreText => ServicesVSResources.Learn_more;
        public static string DescriptionHeader => ServicesVSResources.Description;
        public static string PreferenceHeader => ServicesVSResources.Preference;
        public static string SeverityHeader => ServicesVSResources.Severity;
        public static string GenerateEditorConfigFileFromSettingsText => ServicesVSResources.Generate_dot_editorconfig_file_from_settings;

        internal GridOptionPreviewControl(
            IServiceProvider serviceProvider,
            OptionStore optionStore,
            Func<OptionStore, IServiceProvider,
            AbstractOptionPreviewViewModel> createViewModel,
            ImmutableArray<(string feature, ImmutableArray<IOption2> options)> groupedEditorConfigOptions,
            string language)
            : base(optionStore)
        {
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _createViewModel = createViewModel;
            _language = language;
            _groupedEditorConfigOptions = groupedEditorConfigOptions;
        }

        private void LearnMoreHyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }

            VisualStudioNavigateToLinkService.StartBrowser(e.Uri);
            e.Handled = true;
        }

        private void Options_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            var codeStyleItem = (AbstractCodeStyleOptionViewModel)dataGrid.SelectedItem;

            if (codeStyleItem != null)
            {
                ViewModel.UpdatePreview(codeStyleItem.GetPreview());
            }
        }

        private void Options_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // TODO: make the combo to drop down on space or some key.
            if (e.Key == Key.Space && e.KeyboardDevice.Modifiers == ModifierKeys.None)
            {
            }
        }

        internal override void OnLoad()
        {
            this.ViewModel = _createViewModel(OptionStore, _serviceProvider);

            var firstItem = this.ViewModel.CodeStyleItems.OfType<AbstractCodeStyleOptionViewModel>().First();
            this.ViewModel.SetOptionAndUpdatePreview(firstItem.SelectedPreference.IsChecked, firstItem.Option, firstItem.GetPreview());

            DataContext = ViewModel;
        }

        internal override void Close()
        {
            base.Close();

            this.ViewModel?.Dispose();
        }

        internal void Generate_Save_EditorConfig(object sender, System.Windows.RoutedEventArgs e)
        {
            Logger.Log(FunctionId.ToolsOptions_GenerateEditorconfig);

            var editorconfig = EditorConfigFileGenerator.Generate(_groupedEditorConfigOptions, OptionStore, _language);
            using (var sfd = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "All files (*.*)|",
                FileName = ".editorconfig",
                Title = ServicesVSResources.Save_dot_editorconfig_file,
                InitialDirectory = GetInitialDirectory()
            })
            {
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    IOUtilities.PerformIO(() =>
                    {
                        var filePath = sfd.FileName;
                        File.WriteAllText(filePath, editorconfig.ToString());
                    });
                }
            }
        }

        private static string GetInitialDirectory()
        {
            var solution = (IVsSolution)Shell.ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution));
            if (solution is object)
            {
                if (!ErrorHandler.Failed(solution.GetSolutionInfo(out _, out var solutionFilePath, out _)))
                {
                    return Path.GetDirectoryName(solutionFilePath);
                }
            }

            // returning an empty string will cause SaveFileDialog to use the directory from which 
            // the user last selected a file
            return string.Empty;
        }
    }
}
