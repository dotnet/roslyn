// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings;

/// <summary>
/// Interaction logic for SettingsEditorControl.xaml
/// </summary>
internal partial class SettingsEditorControl : UserControl
{
    private readonly ISettingsEditorView[] _views;
    private readonly IWpfTableControl[] _tableControls;
    private readonly Workspace _workspace;
    private readonly string _filepath;
    private readonly IThreadingContext _threadingContext;
    private readonly EditorTextUpdater _textUpdater;

    public static string WhitespaceTabTitle => ServicesVSResources.Whitespace;
    public UserControl WhitespaceControl { get; }
    public static string CodeStyleTabTitle => ServicesVSResources.Code_Style;
    public UserControl CodeStyleControl { get; }
    public static string NamingStyleTabTitle => ServicesVSResources.Naming_Style;
    public UserControl NamingStyleControl { get; }
    public static string AnalyzersTabTitle => ServicesVSResources.Analyzers;
    public UserControl AnalyzersControl { get; }

    public SettingsEditorControl(ISettingsEditorView whitespaceView,
                                 ISettingsEditorView codeStyleView,
                                 ISettingsEditorView namingStyleView,
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

        WhitespaceControl = whitespaceView.SettingControl;
        CodeStyleControl = codeStyleView.SettingControl;
        NamingStyleControl = namingStyleView.SettingControl;
        AnalyzersControl = analyzerView.SettingControl;

        _views =
        [
            whitespaceView,
            codeStyleView,
            namingStyleView,
            analyzerView
        ];

        _tableControls = _views.SelectAsArray(view => view.TableControl).ToArray();

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
            var originalText = await analyzerConfigDocument.GetValueTextAsync(default).ConfigureAwait(false);
            var updatedText = originalText;
            foreach (var view in _views)
            {
                // Get any changes for the editors. This will return the source text if there are no changes.
                updatedText = await view.UpdateEditorConfigAsync(updatedText).ConfigureAwait(false);
            }

            // Save the updates if they are different from what is currently saved
            if (updatedText != originalText)
            {
                _textUpdater.UpdateText(updatedText.GetTextChanges(originalText));
            }
        });
    }

    internal IWpfTableControl[] GetTableControls() => _tableControls;

    internal void OnClose()
    {
        foreach (var view in _views)
        {
            view.OnClose();
        }
    }

    private void TabsSettingsEditor_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            else if (ReferenceEquals(tag, NamingStyleControl))
            {
                return NamingStyleFrame;
            }
            else if (ReferenceEquals(tag, AnalyzersControl))
            {
                return AnalyzersFrame;
            }

            throw ExceptionUtilities.Unreachable();
        }
    }
}
