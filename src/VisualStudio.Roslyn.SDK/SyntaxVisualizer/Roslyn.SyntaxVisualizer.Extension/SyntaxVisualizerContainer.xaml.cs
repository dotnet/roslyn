// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml.Linq;

using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;

using Roslyn.SyntaxVisualizer.DgmlHelper;

namespace Roslyn.SyntaxVisualizer.Extension
{
    // Control that hosts SyntaxVisualizerControl inside a Visual Studio ToolWindow. This control implements all the
    // logic necessary for interaction with Visual Studio's code documents and directed graph documents.
    internal partial class SyntaxVisualizerContainer : UserControl, IVsRunningDocTableEvents, IVsSolutionEvents, IDisposable
    {
        private readonly SyntaxVisualizerToolWindow parent;
        private IWpfTextView? activeWpfTextView;
        private IClassificationFormatMap? activeClassificationFormatMap;
        private IEditorFormatMap? activeEditorFormatMap;
        private SyntaxTree? activeSyntaxTree;
        private DispatcherTimer? typingTimer;

        private const string CSharpContentType = "CSharp";
        private const string VisualBasicContentType = "Basic";
        private readonly TimeSpan typingTimerTimeout = TimeSpan.FromMilliseconds(300);

        internal SyntaxVisualizerContainer(SyntaxVisualizerToolWindow parent)
        {
            InitializeComponent();

            this.parent = parent;

            InitializeRunningDocumentTable();

            var shellService = GetRequiredMefService<IVsShell, SVsShell>(GlobalServiceProvider);
            if (shellService != null)
            {

                // Only enable this feature if the Visual Studio package for DGML is installed.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                shellService.IsPackageInstalled(GuidList.GuidProgressionPkg, out var canDisplayDirectedSyntaxGraph);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                if (Convert.ToBoolean(canDisplayDirectedSyntaxGraph))
                {
                    syntaxVisualizer.SyntaxNodeDirectedGraphRequested += DisplaySyntaxNodeDgml;
                    syntaxVisualizer.SyntaxTokenDirectedGraphRequested += DisplaySyntaxTokenDgml;
                    syntaxVisualizer.SyntaxTriviaDirectedGraphRequested += DisplaySyntaxTriviaDgml;
                }
            }

            UpdateThemedColors();

            syntaxVisualizer.SyntaxNodeNavigationToSourceRequested += node => NavigateToSource(node?.Span);
            syntaxVisualizer.SyntaxTokenNavigationToSourceRequested += token => NavigateToSource(token.Span);
            syntaxVisualizer.SyntaxTriviaNavigationToSourceRequested += trivia => NavigateToSource(trivia.Span);
        }

        internal void UpdateThemedColors()
        {
            var uiShellService = GetRequiredMefService<IVsUIShell5, SVsUIShell>(GlobalServiceProvider);
            if (uiShellService != null)
            {
                syntaxVisualizer.SetPropertyGridColors(uiShellService);
            }

            if (activeClassificationFormatMap != null && activeEditorFormatMap != null)
            {
                var classificationTypeRegistryService = GetRequiredMefService<IClassificationTypeRegistryService>();
                if (classificationTypeRegistryService is not null)
                {
                    syntaxVisualizer.SetTreeViewColors(classificationTypeRegistryService, activeClassificationFormatMap, activeEditorFormatMap);
                }
            }
        }

        private void HandleFormatMappingChanged(object sender, EventArgs e)
        {
            UpdateThemedColors();
        }

        internal void Clear()
        {
            if (typingTimer != null)
            {
                typingTimer.Stop();
                typingTimer.Tick -= HandleTypingTimerTimeout;
                typingTimer = null;
            }

            if (activeWpfTextView != null)
            {
                activeWpfTextView.Selection.SelectionChanged -= HandleSelectionChanged;
                activeWpfTextView.TextBuffer.Changed -= HandleTextBufferChanged;
                activeWpfTextView.LostAggregateFocus -= HandleTextViewLostFocus;
                activeWpfTextView = null;
            }

            if (activeClassificationFormatMap != null)
            {
                activeClassificationFormatMap.ClassificationFormatMappingChanged -= HandleFormatMappingChanged;
                activeClassificationFormatMap = null;
            }

            if (activeEditorFormatMap != null)
            {
                activeEditorFormatMap.FormatMappingChanged -= HandleFormatMappingChanged;
                activeEditorFormatMap = null;
            }

            activeSyntaxTree = null;
            syntaxVisualizer.Clear();
        }

        #region Helpers - GetService
        private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider? globalServiceProvider;
        private static Microsoft.VisualStudio.OLE.Interop.IServiceProvider GlobalServiceProvider
        {
            get
            {
                if (globalServiceProvider == null)
                {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                    globalServiceProvider = (Microsoft.VisualStudio.OLE.Interop.IServiceProvider)Package.GetGlobalService(
                        typeof(Microsoft.VisualStudio.OLE.Interop.IServiceProvider));
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                }

                return globalServiceProvider;
            }
        }

        private TServiceInterface? GetService<TServiceInterface, TService>()
            where TServiceInterface : class
            where TService : class
        {
            TServiceInterface? service = null;

            if (parent != null)
            {
                service = parent.GetVsService<TServiceInterface, TService>();
            }

            return service;
        }

        private static object? GetService(
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider, Guid guidService, bool unique)
        {
            var guidInterface = VSConstants.IID_IUnknown;
            var ptr = IntPtr.Zero;
            object? service = null;

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            if (serviceProvider.QueryService(ref guidService, ref guidInterface, out ptr) == 0 &&
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                ptr != IntPtr.Zero)
            {
                try
                {
                    if (unique)
                    {
                        service = Marshal.GetUniqueObjectForIUnknown(ptr);
                    }
                    else
                    {
                        service = Marshal.GetObjectForIUnknown(ptr);
                    }
                }
                finally
                {
                    Marshal.Release(ptr);
                }
            }

            return service;
        }

        private static TServiceInterface GetRequiredMefService<TServiceInterface, TService>(
            Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider)
            where TServiceInterface : class
            where TService : class
        {
            var service = (TServiceInterface?)GetService(serviceProvider, typeof(TService).GUID, false);
            Assumes.Present(service);
            return service;
        }

        private static TServiceInterface GetRequiredMefService<TServiceInterface>() where TServiceInterface : class
        {
            var componentModel = GetRequiredMefService<IComponentModel, SComponentModel>(GlobalServiceProvider);
            Assumes.Present(componentModel);
            return componentModel.GetService<TServiceInterface>(); ;
        }
        #endregion

        #region Helpers - Initialize and Dispose IVsRunningDocumentTable
        private uint runningDocumentTableCookie;

        private IVsRunningDocumentTable? runningDocumentTable;
        private IVsRunningDocumentTable? RunningDocumentTable
        {
            get
            {
                if (runningDocumentTable == null)
                {
                    runningDocumentTable = GetRequiredMefService<IVsRunningDocumentTable, SVsRunningDocumentTable>(GlobalServiceProvider);
                }

                return runningDocumentTable;
            }
        }

        private void InitializeRunningDocumentTable()
        {
            if (RunningDocumentTable != null)
            {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                RunningDocumentTable.AdviseRunningDocTableEvents(this, out runningDocumentTableCookie);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            }
        }

        void IDisposable.Dispose()
        {
            if (runningDocumentTableCookie != 0)
            {
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                runningDocumentTable?.UnadviseRunningDocTableEvents(runningDocumentTableCookie);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                runningDocumentTableCookie = 0;
            }
        }
        #endregion

        #region Event Handlers - Editor / IVsRunningDocTableEvents Events

        // Populate the treeview with the contents of the SyntaxTree corresponding to the code document
        // that is currently active in the editor.
        private void RefreshSyntaxVisualizer()
        {
            if (IsVisible && activeWpfTextView != null)
            {
                var snapshot = activeWpfTextView.TextBuffer.CurrentSnapshot;
                var contentType = snapshot.ContentType;

                if (contentType.IsOfType(VisualBasicContentType) ||
                    contentType.IsOfType(CSharpContentType))
                {
                    // Get the Document corresponding to the currently active text snapshot.
                    var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document != null)
                    {
                        // Get the SyntaxTree and SemanticModel corresponding to the Document.
                        activeSyntaxTree = ThreadHelper.JoinableTaskFactory.Run(() => document.GetSyntaxTreeAsync());
                        var activeSemanticModel = ThreadHelper.JoinableTaskFactory.Run(() => document.GetSemanticModelAsync());

                        // Display the SyntaxTree.
                        if (( contentType.IsOfType(VisualBasicContentType) || contentType.IsOfType(CSharpContentType) ) && activeSyntaxTree is not null)
                        {
                            syntaxVisualizer.DisplaySyntaxTree(activeSyntaxTree, activeSemanticModel, workspace: document.Project.Solution.Workspace);
                        }

                        NavigateFromSource();
                    }
                }
            }
        }

        // When user clicks / selects text in the editor select the corresponding item in the treeview.
        private void NavigateFromSource()
        {
            if (IsVisible && activeWpfTextView != null)
            {
                var span = activeWpfTextView.Selection.StreamSelectionSpan.SnapshotSpan.Span;
                syntaxVisualizer.NavigateToBestMatch(span.Start, span.Length);
            }
        }

        // When user clicks on a particular item in the treeview select the corresponding text in the editor.
        private void NavigateToSource(TextSpan? span)
        {
            if (IsVisible && activeWpfTextView != null && span is TextSpan nonNullableSpan)
            {
                var snapShotSpan = nonNullableSpan.ToSnapshotSpan(activeWpfTextView.TextBuffer.CurrentSnapshot);

                // See SyntaxVisualizerToolWindow_GotFocus and SyntaxVisualizerToolWindow_LostFocus
                // for some notes about selection opacity and why it needs to be manipulated.
                activeWpfTextView.Selection.Select(snapShotSpan, false);
                activeWpfTextView.ViewScroller.EnsureSpanVisible(snapShotSpan);
            }
        }

        private void HandleSelectionChanged(object sender, EventArgs e)
        {
            NavigateFromSource();
        }

        // Below timer logic ensures that we won't refresh the treeview for each and every
        // character that the user is typing. Instead we wait for a pause in the user's typing
        // that is at least a few hundred milliseconds long before we refresh the treeview.

        private void HandleTextBufferChanged(object sender, EventArgs e)
        {
            if (typingTimer == null)
            {
                typingTimer = new DispatcherTimer
                {
                    Interval = typingTimerTimeout
                };
                typingTimer.Tick += HandleTypingTimerTimeout;
            }

            typingTimer.Stop();
            typingTimer.Start();
        }

        private void HandleTextViewLostFocus(object sender, EventArgs e)
        {
            if (typingTimer != null)
            {
                typingTimer.Stop();
            }
        }

        private void HandleTypingTimerTimeout(object sender, EventArgs e)
        {
            typingTimer?.Stop();
            RefreshSyntaxVisualizer();
        }

        // Handle the case where the user opens a new code document / switches to a different code document.
        int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int isFirstShow, IVsWindowFrame vsWindowFrame)
        {
            if (IsVisible && isFirstShow == 0)
            {
                var wpfTextView = vsWindowFrame.ToWpfTextView();
                if (wpfTextView != null)
                {
                    var contentType = wpfTextView.TextBuffer.ContentType;
                    if (contentType.IsOfType(VisualBasicContentType) ||
                        contentType.IsOfType(CSharpContentType))
                    {
                        Clear();
                        activeWpfTextView = wpfTextView;
                        activeWpfTextView.Selection.SelectionChanged += HandleSelectionChanged;
                        activeWpfTextView.TextBuffer.Changed += HandleTextBufferChanged;
                        activeWpfTextView.LostAggregateFocus += HandleTextViewLostFocus;

                        var classificationFormatMapService = GetRequiredMefService<IClassificationFormatMapService>();
                        var editorFormatMapService = GetRequiredMefService<IEditorFormatMapService>();
                        activeClassificationFormatMap = classificationFormatMapService?.GetClassificationFormatMap(activeWpfTextView);
                        if (activeClassificationFormatMap is not null)
                        {
                            activeClassificationFormatMap.ClassificationFormatMappingChanged += HandleFormatMappingChanged;
                        }
                        activeEditorFormatMap = editorFormatMapService?.GetEditorFormatMap(activeWpfTextView);
                        if (activeEditorFormatMap is not null)
                        {
                            activeEditorFormatMap.FormatMappingChanged += HandleFormatMappingChanged;
                        }

                        UpdateThemedColors();
                        RefreshSyntaxVisualizer();
                    }
                }
            }

            return VSConstants.S_OK;
        }

        // Handle the case where the user closes the current code document / switches to a different code document.
        int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame vsWindowFrame)
        {
            if (IsVisible && activeWpfTextView != null)
            {
                var wpfTextView = vsWindowFrame.ToWpfTextView();
                if (wpfTextView == activeWpfTextView)
                {
                    Clear();
                }
            }

            return VSConstants.S_OK;
        }

        #region Unused IVsRunningDocTableEvents Events
        int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint lockType, uint readLocksRemaining, uint editLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint lockType, uint readLocksRemaining, uint editLocksRemaining)
        {
            return VSConstants.S_OK;
        }

        int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
        {
            return VSConstants.S_OK;
        }
        #endregion
        #endregion

        #region Event Handlers - Directed Syntax Graph / IVsSolutionEvents Events
        private string? dgmlFilePath;
        private IVsFileChangeEx? fileChangeService;
        private IVsFileChangeEx? FileChangeService
        {
            get
            {
                if (fileChangeService == null)
                {
                    fileChangeService = GetService<IVsFileChangeEx, SVsFileChangeEx>();
                }

                return fileChangeService;
            }
        }

        private IVsSolution? solutionService;
        private IVsSolution? SolutionService
        {
            get
            {
                if (solutionService == null)
                {
                    solutionService = GetService<IVsSolution, SVsSolution>();
                }

                return solutionService;
            }
        }

        private void DisplayDgml(XElement? dgml)
        {
            uint cookie;
            const int TRUE = -1;

            if (string.IsNullOrWhiteSpace(dgmlFilePath))
            {
                var folderPath = Path.Combine(Path.GetTempPath(),
                    "Syntax-" + System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
                Directory.CreateDirectory(folderPath);
                dgmlFilePath = Path.Combine(folderPath, "Syntax.dgml");
            }

            // Check whether the file is already open in the 'design' view.
            // If the file is already open in the desired view then we will update the 
            // contents of the file on disk with the new directed syntax graph and load 
            // this new graph into the already open view of the file.
            if (VsShellUtilities.IsDocumentOpen(
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                ServiceProvider.GlobalProvider, dgmlFilePath, GuidList.GuidVsDesignerViewKind,
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                out var docUIHierarchy, out var docItemId, out var docWindowFrame) && docWindowFrame != null)
            {
                if (RunningDocumentTable is not null &&
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                    RunningDocumentTable.FindAndLockDocument((uint)_VSRDTFLAGS.RDT_NoLock, dgmlFilePath,
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                                                             out var docHierarchy, out docItemId,
                                                             out var docDataIUnknownPointer,
                                                             out cookie) == VSConstants.S_OK)
                {
                    var persistDocDataServiceGuid = typeof(IVsPersistDocData).GUID;

                    if (Marshal.QueryInterface(docDataIUnknownPointer, ref persistDocDataServiceGuid,
                                               out var persistDocDataServicePointer) == 0)
                    {
                        try
                        {
                            var persistDocDataService =
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                                (IVsPersistDocData)Marshal.GetObjectForIUnknown(persistDocDataServicePointer);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread

                            if (persistDocDataService != null)
                            {
                                // The below call ensures that there are no pop-ups from Visual Studio
                                // prompting the user to reload the file each time it is changed.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                                FileChangeService?.IgnoreFile(0, dgmlFilePath, TRUE);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread

                                // Update the file on disk with the new directed syntax graph.
                                dgml?.Save(dgmlFilePath);

                                // The below calls ensure that the file is refreshed inside Visual Studio
                                // so that the latest contents are displayed to the user.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                                FileChangeService?.SyncFile(dgmlFilePath);
                                persistDocDataService.ReloadDocData((uint)_VSRELOADDOCDATA.RDD_IgnoreNextFileChange);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread

                                // Make sure the directed syntax graph window is visible but don't give it focus.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                                docWindowFrame.ShowNoActivate();
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                            }
                        }
                        finally
                        {
                            Marshal.Release(persistDocDataServicePointer);
                        }
                    }
                }
            }
            else
            {
                // Update the file on disk with the new directed syntax graph.
                dgml?.Save(dgmlFilePath);

                // Open the new directed syntax graph in the 'design' view.
                VsShellUtilities.OpenDocument(
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                    ServiceProvider.GlobalProvider, dgmlFilePath, GuidList.GuidVsDesignerViewKind,
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
                    out docUIHierarchy, out docItemId, out docWindowFrame);

                // Register event handler to ensure that directed syntax graph file is deleted when the solution is closed.
                // This ensures that the file won't be persisted in the .suo file and that it therefore won't get re-opened
                // when the solution is re-opened.
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                SolutionService?.AdviseSolutionEvents(this, out cookie);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            }
        }

        private void DisplaySyntaxNodeDgml(SyntaxNode? node)
        {
            if (activeWpfTextView != null)
            {
                var snapshot = activeWpfTextView.TextBuffer.CurrentSnapshot;
                var contentType = snapshot.ContentType;
                XElement? dgml = null;

                if (contentType.IsOfType(CSharpContentType) || contentType.IsOfType(VisualBasicContentType))
                {
                    dgml = node.ToDgml();
                }

                DisplayDgml(dgml);
            }
        }

        private void DisplaySyntaxTokenDgml(SyntaxToken token)
        {
            if (activeWpfTextView != null)
            {
                var snapshot = activeWpfTextView.TextBuffer.CurrentSnapshot;
                var contentType = snapshot.ContentType;
                XElement? dgml = null;

                if (contentType.IsOfType(CSharpContentType) || contentType.IsOfType(VisualBasicContentType))
                {
                    dgml = token.ToDgml();
                }

                DisplayDgml(dgml);
            }
        }

        private void DisplaySyntaxTriviaDgml(SyntaxTrivia trivia)
        {
            if (activeWpfTextView != null)
            {
                var snapshot = activeWpfTextView.TextBuffer.CurrentSnapshot;
                var contentType = snapshot.ContentType;
                XElement? dgml = null;

                if (contentType.IsOfType(CSharpContentType) || contentType.IsOfType(VisualBasicContentType))
                {
                    dgml = trivia.ToDgml();
                }

                DisplayDgml(dgml);
            }
        }

        // Ensure that directed syntax graph file is deleted when the solution is closed. This ensures that
        // the file won't be persisted in the .suo file and that it therefore won't get re-opened when the
        // solution is re-opened.
        int IVsSolutionEvents.OnBeforeCloseSolution(object ignore)
        {
            if (!string.IsNullOrWhiteSpace(dgmlFilePath))
            {
                var folderPath = Path.GetDirectoryName(dgmlFilePath);
                if (Directory.Exists(folderPath))
                {
                    Directory.Delete(folderPath, recursive: true);
                }
            }

            return VSConstants.S_OK;
        }

        #region Unused IVsSolutionEvents Events
        int IVsSolutionEvents.OnAfterCloseSolution(object ignore)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy ignore, IVsHierarchy ignore1)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy ignore, int ignore1)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object ignore, int ignore1)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy ignore, int ignore1)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy ignore, IVsHierarchy ignore1)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy ignore, int ignore1, ref int ignore2)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object ignore, ref int ignore2)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy ignore, ref int ignore2)
        {
            return VSConstants.S_OK;
        }
        #endregion
        #endregion

        #region Event Handlers - Other
        private void SyntaxVisualizerToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            HandleTextBufferChanged(sender, e);
        }

        private void SyntaxVisualizerToolWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            Clear();
        }

        private void SyntaxVisualizerToolWindow_GotFocus(object sender, RoutedEventArgs e)
        {
            if (activeWpfTextView != null && !activeWpfTextView.Properties.ContainsProperty("BackupOpacity"))
            {
                var selectionLayer = activeWpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.Selection);

                // Backup current selection opacity value.
                activeWpfTextView.Properties.AddProperty("BackupOpacity", selectionLayer.Opacity);

                // Set selection opacity to a high value. This ensures that the text selection is visible
                // even when the code editor loses focus (i.e. when user is changing the text selection by
                // clicking on nodes in the TreeView).
                selectionLayer.Opacity = 1;
            }
        }

        private void SyntaxVisualizerToolWindow_LostFocus(object sender, RoutedEventArgs e)
        {
            if (activeWpfTextView != null && activeWpfTextView.Properties.ContainsProperty("BackupOpacity"))
            {
                var selectionLayer = activeWpfTextView.GetAdornmentLayer(PredefinedAdornmentLayers.Selection);

                // Restore backed up selection opacity value.
                selectionLayer.Opacity = (double)activeWpfTextView.Properties.GetProperty("BackupOpacity");
                activeWpfTextView.Properties.RemoveProperty("BackupOpacity");
            }
        }
        #endregion
    }
}
