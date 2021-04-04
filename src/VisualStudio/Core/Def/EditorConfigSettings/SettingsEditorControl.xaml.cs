// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings
{
    /// <summary>
    /// Interaction logic for SettingsEditorControl.xaml
    /// </summary>
    internal partial class SettingsEditorControl : UserControl
    {
        private readonly ISettingsEditorView _formattingView;
        private readonly ISettingsEditorView _codeStyleView;
        private readonly ISettingsEditorView _analyzerSettingsView;
        private readonly Workspace _workspace;
        private readonly string _filepath;
        private readonly IThreadingContext _threadingContext;
        private readonly EditorTextUpdater _textUpdater;

        public static string Formatting => ServicesVSResources.Formatting;
        public static string CodeStyle => ServicesVSResources.Code_Style;
        public static string Analyzers => ServicesVSResources.Analyzers;

        public SettingsEditorControl(ISettingsEditorView formattingView,
                                     ISettingsEditorView codeStyleView,
                                     ISettingsEditorView analyzerSettingsView,
                                     Workspace workspace,
                                     string filepath,
                                     IThreadingContext threadingContext,
                                     IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
                                     IVsTextLines textLines)
        {
            InitializeComponent();
            DataContext = this;
            _workspace = workspace;
            _filepath = filepath;
            _threadingContext = threadingContext;
            _textUpdater = new EditorTextUpdater(editorAdaptersFactoryService, textLines);
            _formattingView = formattingView;
            FormattingTab.Content = _formattingView.SettingControl;
            _codeStyleView = codeStyleView;
            CodeStyleTab.Content = _codeStyleView.SettingControl;
            _analyzerSettingsView = analyzerSettingsView;
            AnalyzersTab.Content = _analyzerSettingsView.SettingControl;
        }

        internal void SynchronizeSettings()
        {
            if (!IsKeyboardFocusWithin)
            {
                return;
            }

            var solution = _workspace.CurrentSolution;
            var analyzerConfigDocument = solution.Projects
                .Select(p => p.TryGetExistingAnalyzerConfigDocumentAtPath(_filepath)).FirstOrDefault();
            if (analyzerConfigDocument is null)
            {
                return;
            }

            _threadingContext.JoinableTaskFactory.Run(async () =>
            {
                var originalText = await analyzerConfigDocument.GetTextAsync(default).ConfigureAwait(false);
                var updatedText = await _formattingView.UpdateEditorConfigAsync(originalText).ConfigureAwait(false);
                updatedText = await _codeStyleView.UpdateEditorConfigAsync(updatedText).ConfigureAwait(false);
                updatedText = await _analyzerSettingsView.UpdateEditorConfigAsync(updatedText).ConfigureAwait(false);
                _textUpdater.UpdateText(updatedText.GetTextChanges(originalText));
            });
        }

        internal IWpfTableControl[] GetTableControls()
            => new[]
            {
                _formattingView.TableControl,
                _codeStyleView.TableControl,
                _analyzerSettingsView.TableControl,
            };

        internal void OnClose()
        {
            _formattingView.OnClose();
            _codeStyleView.OnClose();
            _analyzerSettingsView.OnClose();
        }
    }
}
