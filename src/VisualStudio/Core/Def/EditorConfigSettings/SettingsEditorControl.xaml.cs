// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Common;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings
{
    /// <summary>
    /// Interaction logic for SettingsEditorControl.xaml
    /// </summary>
    internal partial class SettingsEditorControl : UserControl
    {
        private readonly ISettingsEditorView _whitespaceView;
        private readonly ISettingsEditorView _codeStyleView;
        private readonly ISettingsEditorView _analyzerView;
        private readonly Workspace _workspace;
        private readonly string _filepath;
        private readonly IThreadingContext _threadingContext;
        private readonly EditorTextUpdater _textUpdater;

        public static string WhitespaceTabTitle => ServicesVSResources.Whitespace;
        public UserControl WhitespaceControl => _whitespaceView.SettingControl;
        public static string CodeStyleTabTitle => ServicesVSResources.Code_Style;
        public UserControl CodeStyleControl => _codeStyleView.SettingControl;
        public static string AnalyzersTabTitle => ServicesVSResources.Analyzers;
        public UserControl AnalyzersControl => _analyzerView.SettingControl;

        public SettingsEditorControl(ISettingsEditorView whitespaceView,
                                     ISettingsEditorView codeStyleView,
                                     ISettingsEditorView analyzerView,
                                     Workspace workspace,
                                     string filepath,
                                     IThreadingContext threadingContext,
                                     IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
                                     IVsTextLines textLines)
        {
            DataContext = this;
            _workspace = workspace;
            _filepath = filepath;
            _threadingContext = threadingContext;
            _textUpdater = new EditorTextUpdater(editorAdaptersFactoryService, textLines);
            _whitespaceView = whitespaceView;
            _codeStyleView = codeStyleView;
            _analyzerView = analyzerView;
            InitializeComponent();
        }

        public Border SearchControlParent => SearchControl;

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
                var updatedText = await _whitespaceView.UpdateEditorConfigAsync(originalText).ConfigureAwait(false);
                updatedText = await _codeStyleView.UpdateEditorConfigAsync(updatedText).ConfigureAwait(false);
                updatedText = await _analyzerView.UpdateEditorConfigAsync(updatedText).ConfigureAwait(false);
                _textUpdater.UpdateText(updatedText.GetTextChanges(originalText));
            });
        }

        internal IWpfTableControl[] GetTableControls()
            => new[]
            {
                _whitespaceView.TableControl,
                _codeStyleView.TableControl,
                _analyzerView.TableControl,
            };

        internal void OnClose()
        {
            _whitespaceView.OnClose();
            _codeStyleView.OnClose();
            _analyzerView.OnClose();
        }

        private void tabsSettingsEditor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var previousTabItem = e.RemovedItems.Count > 0 ? e.RemovedItems[0] as TabItem : null;
            var selectedTabItem = e.AddedItems.Count > 0 ? e.AddedItems[0] as TabItem : null;

            if (previousTabItem is not null && selectedTabItem is not null)
            {
                if (ReferenceEquals(selectedTabItem.Tag, previousTabItem.Tag))
                {
                    return;
                }

                if (GetTabItem(previousTabItem.Tag) is ContentPresenter prevFrame &&
                    GetTabItem(selectedTabItem.Tag) is ContentPresenter currentFrame)
                {
                    prevFrame.Visibility = Visibility.Hidden;
                    currentFrame.Visibility = Visibility.Visible;
                }
            }

            ContentPresenter GetTabItem(object tag)
            {
                if (ReferenceEquals(tag, WhitespaceControl))
                {
                    return WhitespaceFrame;
                }
                else if (ReferenceEquals(tag, CodeStyleControl))
                {
                    return CodeStyleFrame;
                }
                else if (ReferenceEquals(tag, AnalyzersControl))
                {
                    return AnalyzersFrame;
                }

                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
