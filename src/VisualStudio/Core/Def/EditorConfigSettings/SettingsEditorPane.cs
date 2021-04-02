// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Microsoft.Internal.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.View;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Analyzers.ViewModel;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.View;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.CodeStyle.ViewModel;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Formatting.View;
using Microsoft.VisualStudio.LanguageServices.EditorConfigSettings.Formatting.ViewModel;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.TextManager.Interop;
using static Microsoft.VisualStudio.VSConstants;

namespace Microsoft.VisualStudio.LanguageServices.EditorConfigSettings
{
    internal sealed partial class SettingsEditorPane : WindowPane, IOleComponent, IVsDeferredDocView, IVsLinkedUndoClient, IVsWindowSearch
    {
        private readonly IVsEditorAdaptersFactoryService _vsEditorAdaptersFactoryService;
        private readonly IThreadingContext _threadingContext;
        private readonly ISettingsAggregator _settingsDataProviderService;
        private readonly IWpfTableControlProvider _controlProvider;
        private readonly ITableManagerProvider _tableMangerProvider;
        private readonly string _fileName;
        private readonly IVsTextLines _textBuffer;
        private readonly Workspace _workspace;
        private uint _componentId;
        private IOleUndoManager? _undoManager;
        private SettingsEditorControl? _control;

        public SettingsEditorPane(IVsEditorAdaptersFactoryService vsEditorAdaptersFactoryService,
                                  IThreadingContext threadingContext,
                                  ISettingsAggregator settingsDataProviderService,
                                  IWpfTableControlProvider controlProvider,
                                  ITableManagerProvider tableMangerProvider,
                                  string fileName,
                                  IVsTextLines textBuffer,
                                  Workspace workspace)
            : base(null)
        {
            _vsEditorAdaptersFactoryService = vsEditorAdaptersFactoryService;
            _threadingContext = threadingContext;
            _settingsDataProviderService = settingsDataProviderService;
            _controlProvider = controlProvider;
            _tableMangerProvider = tableMangerProvider;
            _fileName = fileName;
            _textBuffer = textBuffer;
            _workspace = workspace;
        }

        protected override void Initialize()
        {
            base.Initialize();

            // Create and initialize the editor
            if (_componentId == default && this.TryGetService<SOleComponentManager, IOleComponentManager>(out var componentManager))
            {
                var componentRegistrationInfo = new[]
                {
                    new OLECRINFO
                    {
                        cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO)),
                        grfcrf = (uint)_OLECRF.olecrfNeedIdleTime | (uint)_OLECRF.olecrfNeedPeriodicIdleTime,
                        grfcadvf = (uint)_OLECADVF.olecadvfModal | (uint)_OLECADVF.olecadvfRedrawOff | (uint)_OLECADVF.olecadvfWarningsOff,
                        uIdleTimeInterval = 100
                    }
                };

                var hr = componentManager.FRegisterComponent(this, componentRegistrationInfo, out _componentId);
                _ = ErrorHandler.Succeeded(hr);
            }

            if (this.TryGetService<SOleUndoManager, IOleUndoManager>(out _undoManager))
            {
                var linkCapableUndoMgr = (IVsLinkCapableUndoManager)_undoManager;
                if (linkCapableUndoMgr is not null)
                {
                    _ = linkCapableUndoMgr.AdviseLinkedUndoClient(this);
                }
            }

            // hook up our panel
            _control = new SettingsEditorControl(
                GetFormattingView(),
                GetCodeStyleView(),
                GetAnalyzerView(),
                _workspace,
                _fileName,
                _threadingContext,
                _vsEditorAdaptersFactoryService,
                _textBuffer);
            Content = _control;

            RegisterIndependentView(true);
            if (this.TryGetService<IMenuCommandService>(out var menuCommandService))
            {
                AddCommand(menuCommandService, GUID_VSStandardCommandSet97, (int)VSStd97CmdID.NewWindow,
                                new EventHandler(OnNewWindow), new EventHandler(OnQueryNewWindow));
                AddCommand(menuCommandService, GUID_VSStandardCommandSet97, (int)VSStd97CmdID.ViewCode,
                                new EventHandler(OnViewCode), new EventHandler(OnQueryViewCode));
            }

            ISettingsEditorView GetFormattingView()
            {
                var dataProvider = _settingsDataProviderService.GetSettingsProvider<FormattingSetting>(_fileName);
                if (dataProvider is null)
                {
                    throw new InvalidOperationException("Unable to get formatter settings");
                }
                var viewModel = new FormattingViewModel(dataProvider, _controlProvider, _tableMangerProvider);
                return new FormattingSettingsView(viewModel);
            }

            ISettingsEditorView GetCodeStyleView()
            {
                var dataProvider = _settingsDataProviderService.GetSettingsProvider<CodeStyleSetting>(_fileName);
                if (dataProvider is null)
                {
                    throw new InvalidOperationException("Unable to get code style settings");
                }
                var viewModel = new CodeStyleSettingsViewModel(dataProvider, _controlProvider, _tableMangerProvider);
                return new CodeStyleSettingsView(viewModel);
            }

            ISettingsEditorView GetAnalyzerView()
            {
                var dataProvider = _settingsDataProviderService.GetSettingsProvider<AnalyzerSetting>(_fileName);
                if (dataProvider is null)
                {
                    throw new InvalidOperationException("Unable to get analyzer settings");
                }

                var viewModel = new AnalyzerSettingsViewModel(dataProvider, _controlProvider, _tableMangerProvider);
                return new AnalyzerSettingsView(viewModel);
            }
        }

        private void OnQueryNewWindow(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            command.Enabled = true;
        }

        private void OnNewWindow(object sender, EventArgs e)
        {
            NewWindow();
        }

        private void OnQueryViewCode(object sender, EventArgs e)
        {
            var command = (OleMenuCommand)sender;
            command.Enabled = true;
        }

        private void OnViewCode(object sender, EventArgs e)
        {
            ViewCode();
        }

        private void NewWindow()
        {
            if (this.TryGetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>(out var uishellOpenDocument) &&
                this.TryGetService<SVsWindowFrame, IVsWindowFrame>(out var windowFrameOrig))
            {
                var logicalView = Guid.Empty;
                var hr = uishellOpenDocument.OpenCopyOfStandardEditor(windowFrameOrig, ref logicalView, out var windowFrameNew);
                if (windowFrameNew != null)
                {
                    hr = windowFrameNew.Show();
                }

                _ = ErrorHandler.ThrowOnFailure(hr);
            }
        }

        private void ViewCode()
        {
            var sourceCodeTextEditorGuid = VsEditorFactoryGuid.TextEditor_guid;

            // Open the referenced document using our editor.
            VsShellUtilities.OpenDocumentWithSpecificEditor(this, _fileName,
                sourceCodeTextEditorGuid, LOGVIEWID_Primary, out _, out _, out var frame);
            _ = ErrorHandler.ThrowOnFailure(frame.Show());
        }

        protected override void OnClose()
        {
            // unhook from Undo related services
            if (_undoManager != null)
            {
                var linkCapableUndoMgr = (IVsLinkCapableUndoManager)_undoManager;
                if (linkCapableUndoMgr != null)
                {
                    _ = linkCapableUndoMgr.UnadviseLinkedUndoClient();
                }

                // Throw away the undo stack etc.
                // It is important to "zombify" the undo manager when the owning object is shutting down.
                // This is done by calling IVsLifetimeControlledObject.SeverReferencesToOwner on the undoManager.
                // This call will clear the undo and redo stacks. This is particularly important to do if
                // your undo units hold references back to your object. It is also important if you use
                // "mdtStrict" linked undo transactions as this sample does (see IVsLinkedUndoTransactionManager). 
                // When one object involved in linked undo transactions clears its undo/redo stacks, then 
                // the stacks of the other documents involved in the linked transaction will also be cleared. 
                var lco = (IVsLifetimeControlledObject)_undoManager;
                _ = lco.SeverReferencesToOwner();
                _undoManager = null;
            }

            if (this.TryGetService<SOleComponentManager, IOleComponentManager>(out var componentManager))
            {
                _ = componentManager.FRevokeComponent(_componentId);
            }

            _control?.OnClose();

            Dispose(true);

            base.OnClose();
        }

        public int FDoIdle(uint grfidlef)
        {
            if (_control is not null)
            {
                _control.SynchronizeSettings();
            }
            return S_OK;
        }

        /// <summary>
        /// Registers an independent view with the IVsTextManager so that it knows
        /// the user is working with a view over the text buffer. This will trigger
        /// the text buffer to prompt the user whether to reload the file if it is
        /// edited outside of the environment.
        /// </summary>
        /// <param name="subscribe">True to subscribe, false to unsubscribe</param>
        private void RegisterIndependentView(bool subscribe)
        {
            if (this.TryGetService<SVsTextManager, IVsTextManager>(out var textManager))
            {
                _ = subscribe
                    ? textManager.RegisterIndependentView(this, _textBuffer)
                    : textManager.UnregisterIndependentView(this, _textBuffer);
            }
        }

        /// <summary>
        /// Helper function used to add commands using IMenuCommandService
        /// </summary>
        /// <param name="menuCommandService"> The IMenuCommandService interface.</param>
        /// <param name="menuGroup"> This guid represents the menu group of the command.</param>
        /// <param name="cmdID"> The command ID of the command.</param>
        /// <param name="commandEvent"> An EventHandler which will be called whenever the command is invoked.</param>
        /// <param name="queryEvent"> An EventHandler which will be called whenever we want to query the status of
        /// the command.  If null is passed in here then no EventHandler will be added.</param>
        private static void AddCommand(IMenuCommandService menuCommandService,
                                       Guid menuGroup,
                                       int cmdID,
                                       EventHandler commandEvent,
                                       EventHandler queryEvent)
        {
            // Create the OleMenuCommand from the menu group, command ID, and command event
            var menuCommandID = new CommandID(menuGroup, cmdID);
            var command = new OleMenuCommand(commandEvent, menuCommandID);

            // Add an event handler to BeforeQueryStatus if one was passed in
            if (null != queryEvent)
            {
                command.BeforeQueryStatus += queryEvent;
            }

            // Add the command using our IMenuCommandService instance
            menuCommandService.AddCommand(command);
        }

        public int get_DocView(out IntPtr ppUnkDocView)
        {
            ppUnkDocView = Marshal.GetIUnknownForObject(this);
            return S_OK;
        }

        public int get_CmdUIGuid(out Guid pGuidCmdId)
        {
            pGuidCmdId = SettingsEditorFactory.SettingsEditorFactoryGuid;
            return S_OK;
        }

        public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam) => S_OK;
        public int FPreTranslateMessage(MSG[] pMsg) => S_OK;
        public void OnEnterState(uint uStateID, int fEnter) { }
        public void OnAppActivate(int fActive, uint dwOtherThreadID) { }
        public void OnLoseActivation() { }
        public void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved) { }
        public int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked) => S_OK;
        public int FQueryTerminate(int fPromptUser) => 1; //true
        public void Terminate() { }
        public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved) => IntPtr.Zero;
        public int OnInterveningUnitBlockingLinkedUndo() => E_FAIL;

        public IVsSearchTask? CreateSearch(uint dwCookie, IVsSearchQuery pSearchQuery, IVsSearchCallback pSearchCallback)
        {
            if (_control is not null)
            {
                var tables = _control.GetTableControls();
                return new SearchTask(dwCookie, pSearchQuery, pSearchCallback, tables, _threadingContext);
            }

            return null;
        }

        public void ClearSearch()
        {
            _threadingContext.ThrowIfNotOnUIThread();
            if (_control is not null)
            {
                var tables = _control.GetTableControls();
                // remove filter on tablar data controls
                foreach (var tableControl in tables)
                {
                    _ = tableControl.SetFilter(string.Empty, null);
                }
            }
        }

        public void ProvideSearchSettings(IVsUIDataSource pSearchSettings)
        {
            SetIntValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.ControlMaxWidth, 200);
            SetIntValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchStartType, (int)VSSEARCHSTARTTYPE.SST_DELAYED);
            SetIntValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchStartDelay, 100);
            SetBoolValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchUseMRU, true);
            SetBoolValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.PrefixFilterMRUItems, false);
            SetIntValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.MaximumMRUItems, 25);
            SetStringValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchWatermark, ServicesVSResources.Search_Settings);
            SetBoolValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchPopupAutoDropdown, false);
            SetStringValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.ControlBorderThickness, "1");
            SetIntValue(pSearchSettings, SearchSettingsDataSource.PropertyNames.SearchProgressType, (int)VSSEARCHPROGRESSTYPE.SPT_INDETERMINATE);

            void SetBoolValue(IVsUIDataSource source, string property, bool value)
            {
                var valueProp = BuiltInPropertyValue.FromBool(value);
                _ = source.SetValue(property, valueProp);
            }

            void SetIntValue(IVsUIDataSource source, string property, int value)
            {
                var valueProp = BuiltInPropertyValue.Create(value);
                _ = source.SetValue(property, valueProp);
            }

            void SetStringValue(IVsUIDataSource source, string property, string value)
            {
                var valueProp = BuiltInPropertyValue.Create(value);
                _ = source.SetValue(property, valueProp);
            }
        }

        public bool OnNavigationKeyDown(uint dwNavigationKey, uint dwModifiers) => false;

        public bool SearchEnabled { get; } = true;

        public Guid Category { get; } = new Guid("1BE8950F-AF27-4B71-8D54-1F7FFEFDC237");
        public IVsEnumWindowSearchFilters? SearchFiltersEnum => null;
        public IVsEnumWindowSearchOptions? SearchOptionsEnum => null;
    }
}
