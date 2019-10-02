// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Debugging;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : AbstractLanguageService
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        internal TPackage Package { get; }
        internal IVsDebugger Debugger { get; private set; }
        internal VsLanguageDebugInfo LanguageDebugInfo { get; private set; }

        // DevDiv 753309:
        // We've redefined some VS interfaces that had incorrect PIAs. When 
        // we interop with native parts of VS, they always QI, so everything
        // works. However, Razor is now managed, but assumes that the C#
        // language service is native. When setting breakpoints, they
        // get the language service from its GUID and cast it to IVsLanguageDebugInfo.
        // This would QI the native lang service. Since we're managed and
        // we've redefined IVsLanguageDebugInfo, the cast
        // fails. To work around this, we put the LS inside a ComAggregate object,
        // which always force a QueryInterface and allow their cast to succeed.
        // 
        // This also fixes 752331, which is a similar problem with the 
        // exception assistant.
        internal object ComAggregate { get; private set; }

        // Note: The lifetime for state in this class is carefully managed.  For every bit of state
        // we set up, there is a corresponding tear down phase which deconstructs the state in the
        // reverse order it was created in.
        internal VisualStudioWorkspaceImpl Workspace { get; private set; }
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; private set; }
        internal HostDiagnosticUpdateSource HostDiagnosticUpdateSource { get; private set; }
        internal AnalyzerFileWatcherService AnalyzerFileWatcherService { get; private set; }

        /// <summary>
        /// Whether or not we have been set up. This is set once everything is wired up and cleared once tear down has begun.
        /// </summary>
        /// <remarks>
        /// We don't set this until we've completed setup. If something goes sideways during it, we will never register
        /// with the shell and thus have a floating thing around that can't be safely shut down either. We're in a bad
        /// state but trying to proceed will only make things worse.
        /// </remarks>
        private bool _isSetUp;

        protected AbstractLanguageService(
            TPackage package)
        {
            this.Package = package;
        }

        public override IServiceProvider SystemServiceProvider
        {
            get
            {
                return this.Package;
            }
        }

        /// <summary>
        /// Setup and TearDown go in reverse order.
        /// </summary>
        internal void Setup()
        {
            this.ComAggregate = CreateComAggregate();

            // First, acquire any services we need throughout our lifetime.
            this.GetServices();

            var componentModel = this.Package.ComponentModel;

            // Start off a background task to prime some components we'll need for editing
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.BackgroundThread,
                () => PrimeLanguageServiceComponentsOnBackground(componentModel));

            // Next, make any connections to these services.
            this.ConnectToServices();

            // Finally, once our connections are established, set up any initial state that we need.
            // Note: we may be instantiated at any time (including when the IDE is already
            // debugging).  We must not assume anything about our initial state and must instead
            // query for all the information we need at this point.
            this.Initialize();

            _isSetUp = true;
        }

        private object CreateComAggregate()
        {
            return Interop.ComAggregate.CreateAggregatedObject(this);
        }

        internal void TearDown()
        {
            if (!_isSetUp)
            {
                throw new InvalidOperationException();
            }

            _isSetUp = false;
            GC.SuppressFinalize(this);

            this.Uninitialize();
            this.DisconnectFromServices();
            this.RemoveServices();
        }

        ~AbstractLanguageService()
        {
            if (!Environment.HasShutdownStarted && _isSetUp)
            {
                throw new InvalidOperationException("TearDown not called!");
            }
        }

        protected virtual void GetServices()
        {
            // This method should only contain calls to acquire services off of the component model
            // or service providers.  Anything else which is more complicated should go in Initialize
            // instead.
            this.Workspace = this.Package.Workspace;
            this.EditorAdaptersFactoryService = this.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>();
            this.HostDiagnosticUpdateSource = this.Package.ComponentModel.GetService<HostDiagnosticUpdateSource>();
            this.AnalyzerFileWatcherService = this.Package.ComponentModel.GetService<AnalyzerFileWatcherService>();

            this.Debugger = (IVsDebugger)this.SystemServiceProvider.GetService(typeof(SVsShellDebugger));
        }

        protected virtual void RemoveServices()
        {
            this.Debugger = null;
            this.EditorAdaptersFactoryService = null;
            this.Workspace = null;
        }

        /// <summary>
        /// Keep ConnectToServices and DisconnectFromServices in 1:1 correspondence.
        /// DisconnectFromServices should clean up resources in the reverse direction that they are
        /// initialized in.
        /// </summary>
        protected virtual void ConnectToServices()
        {
            // The language service may have wrapped itself in a ComAggregate.
            // Use the wrapper, because trying to marshal a second time will throw.
            Marshal.ThrowExceptionForHR(this.Debugger.AdviseDebuggerEvents((IVsDebuggerEvents)this.ComAggregate, out _debuggerEventsCookie));
        }

        protected virtual void DisconnectFromServices()
        {
            Marshal.ThrowExceptionForHR(this.Debugger.UnadviseDebuggerEvents(_debuggerEventsCookie));
        }

        /// <summary>
        /// Called right after we instantiate the language service.  Used to set up any internal
        /// state we need.
        /// 
        /// Try to keep this method fairly clean.  Any complicated logic should go in methods called
        /// from this one.  Initialize and Uninitialize go in reverse order 
        /// </summary>
        protected virtual void Initialize()
        {
            InitializeLanguageDebugInfo();
            InitializeDebugMode();
        }

        protected virtual void Uninitialize()
        {
            UninitializeDebugMode();
            UninitializeLanguageDebugInfo();
        }

        private void PrimeLanguageServiceComponentsOnBackground(IComponentModel componentModel)
        {
            var formatter = this.Workspace.Services.GetLanguageServices(RoslynLanguageName).GetService<ISyntaxFormattingService>();
            if (formatter != null)
            {
                formatter.GetDefaultFormattingRules();
            }
        }

        protected abstract string ContentTypeName { get; }
        protected abstract string LanguageName { get; }
        protected abstract string RoslynLanguageName { get; }

        /// <summary>
        /// Cookie used to register/unregister from debugger events.
        /// </summary>
        private uint _debuggerEventsCookie;

        /// <summary>
        /// The current debug mode we are in.
        /// </summary>
        private DebugMode _debugMode;

        protected virtual void SetupNewTextView(IVsTextView textView)
        {
            Contract.ThrowIfNull(textView);

            var wpfTextView = EditorAdaptersFactoryService.GetWpfTextView(textView);
            Contract.ThrowIfNull(wpfTextView, "Could not get IWpfTextView for IVsTextView");

            Debug.Assert(!wpfTextView.Properties.ContainsProperty(typeof(AbstractVsTextViewFilter<TPackage, TLanguageService>)));

            var workspace = Package.ComponentModel.GetService<VisualStudioWorkspace>();

            // The lifetime of CommandFilter is married to the view
            wpfTextView.GetOrCreateAutoClosingProperty(v =>
                new StandaloneCommandFilter<TPackage, TLanguageService>(
                    (TLanguageService)this, v, EditorAdaptersFactoryService).AttachToVsTextView());

            var openDocument = wpfTextView.TextBuffer.AsTextContainer().GetRelatedDocuments().FirstOrDefault();
            var isOpenMetadataAsSource = openDocument != null && openDocument.Project.Solution.Workspace.Kind == WorkspaceKind.MetadataAsSource;

            ConditionallyCollapseOutliningRegions(textView, wpfTextView, workspace, isOpenMetadataAsSource);

            // If this is a metadata-to-source view, we want to consider the file read-only
            if (isOpenMetadataAsSource && ErrorHandler.Succeeded(textView.GetBuffer(out var vsTextLines)))
            {
                ((IVsTextBuffer)vsTextLines).SetStateFlags((uint)BUFFERSTATEFLAGS.BSF_USER_READONLY);

                var runningDocumentTable = (IVsRunningDocumentTable)SystemServiceProvider.GetService(typeof(SVsRunningDocumentTable));
                var runningDocumentTable4 = (IVsRunningDocumentTable4)runningDocumentTable;

                if (runningDocumentTable4.IsMonikerValid(openDocument.FilePath))
                {
                    var cookie = runningDocumentTable4.GetDocumentCookie(openDocument.FilePath);
                    runningDocumentTable.ModifyDocumentFlags(cookie, (uint)_VSRDTFLAGS.RDT_DontAddToMRU | (uint)_VSRDTFLAGS.RDT_CantSave, fSet: 1);
                }
            }
        }

        private void ConditionallyCollapseOutliningRegions(IVsTextView textView, IWpfTextView wpfTextView, Microsoft.CodeAnalysis.Workspace workspace, bool isOpenMetadataAsSource)
        {
            var outliningManagerService = this.Package.ComponentModel.GetService<IOutliningManagerService>();
            var outliningManager = outliningManagerService.GetOutliningManager(wpfTextView);
            if (outliningManager == null)
            {
                return;
            }

            if (!workspace.Options.GetOption(FeatureOnOffOptions.Outlining, this.RoslynLanguageName))
            {
                outliningManager.Enabled = false;
            }
            else
            {
                if (textView is IVsTextViewEx viewEx)
                {
                    if (isOpenMetadataAsSource)
                    {
                        // If this file is a metadata-from-source file, we want to force-collapse any implementations.
                        // First make sure we know what all the outlining spans are.  Then ask the outlining mananger
                        // to collapse all the implementation spans.
                        EnsureOutliningTagsComputed(wpfTextView);
                        outliningManager.CollapseAll(wpfTextView.TextBuffer.CurrentSnapshot.GetFullSpan(), c => c.Tag.IsImplementation);
                    }
                    else
                    {
                        // We also want to automatically collapse any region tags *on the first 
                        // load of a file* if the file contains them.  In order to not do expensive
                        // parsing, we only do this if the file contains #region in it.
                        if (ContainsRegionTag(wpfTextView.TextSnapshot))
                        {
                            // Make sure we at least know what the outlining spans are.
                            // Then when we call PersistOutliningState below the editor will 
                            // get these outlining tags and automatically collapse any 
                            // IsDefaultCollapsed spans the first time around. 
                            //
                            // If it is not the first time opening a file, VS will simply use
                            // the data stored in the SUO file.  
                            EnsureOutliningTagsComputed(wpfTextView);
                        }

                        viewEx.PersistOutliningState();
                    }
                }
            }
        }

        private bool ContainsRegionTag(ITextSnapshot textSnapshot)
        {
            foreach (var line in textSnapshot.Lines)
            {
                if (StartsWithRegionTag(line))
                {
                    return true;
                }
            }

            return false;
        }

        private bool StartsWithRegionTag(ITextSnapshotLine line)
        {
            var snapshot = line.Snapshot;
            var start = line.GetFirstNonWhitespacePosition();
            if (start != null)
            {
                var index = start.Value;
                return line.StartsWith(index, "#region", ignoreCase: true);
            }

            return false;
        }

        private void EnsureOutliningTagsComputed(IWpfTextView wpfTextView)
        {
            // We need to get our outlining tag source to notify it to start blocking
            var outliningTaggerProvider = this.Package.ComponentModel.GetService<VisualStudio14StructureTaggerProvider>();

            var subjectBuffer = wpfTextView.TextBuffer;
            var snapshot = subjectBuffer.CurrentSnapshot;
            var tagger = outliningTaggerProvider.CreateTagger<IOutliningRegionTag>(subjectBuffer);

            using var disposable = tagger as IDisposable;
            tagger.GetAllTags(new NormalizedSnapshotSpanCollection(snapshot.GetFullSpan()), CancellationToken.None);
        }

        private void InitializeLanguageDebugInfo()
        {
            this.LanguageDebugInfo = this.CreateLanguageDebugInfo();
        }

        protected abstract Guid DebuggerLanguageId { get; }

        private VsLanguageDebugInfo CreateLanguageDebugInfo()
        {
            var workspace = this.Workspace;
            var languageServices = workspace.Services.GetLanguageServices(RoslynLanguageName);

            return new VsLanguageDebugInfo(
                this.DebuggerLanguageId,
                (TLanguageService)this,
                languageServices,
                this.Package.ComponentModel.GetService<IWaitIndicator>());
        }

        private void UninitializeLanguageDebugInfo()
        {
            this.LanguageDebugInfo = null;
        }

        private void InitializeDebugMode()
        {
            var modeArray = new DBGMODE[1];
            Marshal.ThrowExceptionForHR(this.Debugger.GetMode(modeArray));

            _debugMode = ConvertDebugMode(modeArray[0]);
            OnDebugModeChanged();
        }

        private void UninitializeDebugMode()
        {
            // Nothing to do here.
        }

        protected virtual IVsContainedLanguage CreateContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator, VisualStudioProject project,
            IVsHierarchy hierarchy, uint itemid)
        {
            return new ContainedLanguage<TPackage, TLanguageService>(
                bufferCoordinator, this.Package.ComponentModel, project, hierarchy, itemid, projectTrackerOpt: null, project.Id,
                (TLanguageService)this);
        }
    }
}
