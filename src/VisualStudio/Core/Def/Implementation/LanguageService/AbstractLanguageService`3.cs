// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Debugging;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService, TProject> : AbstractLanguageService<TPackage, TLanguageService>
        where TPackage : AbstractPackage<TPackage, TLanguageService, TProject>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService, TProject>
        where TProject : AbstractProject
    {
        internal IVsDebugger Debugger { get; private set; }
        internal VsLanguageDebugInfo LanguageDebugInfo { get; private set; }

        /// <summary>
        /// Cookie used to register/unregister from debugger events.
        /// </summary>
        private uint _debuggerEventsCookie;

        /// <summary>
        /// The current debug mode we are in.
        /// </summary>
        private DebugMode _debugMode;

        protected AbstractLanguageService(
            TPackage package)
            : base(package)
        {
        }

        protected override void GetServices()
        {
            base.GetServices();

            this.Debugger = (IVsDebugger)this.SystemServiceProvider.GetService(typeof(SVsShellDebugger));
        }

        protected override void RemoveServices()
        {
            this.Debugger = null;

            base.RemoveServices();
        }

        protected override void ConnectToServices()
        {
            base.ConnectToServices();

            // The language service may have wrapped itself in a ComAggregate.
            // Use the wrapper, because trying to marshal a second time will throw.
            Marshal.ThrowExceptionForHR(this.Debugger.AdviseDebuggerEvents((IVsDebuggerEvents)this.ComAggregate, out _debuggerEventsCookie));
        }

        protected override void DisconnectFromServices()
        {
            Marshal.ThrowExceptionForHR(this.Debugger.UnadviseDebuggerEvents(_debuggerEventsCookie));

            base.DisconnectFromServices();
        }

        /// <summary>
        /// Called right after we instantiate the language service.  Used to set up any internal
        /// state we need.
        /// 
        /// Try to keep this method fairly clean.  Any complicated logic should go in methods called
        /// from this one.  Initialize and Uninitialize go in reverse order 
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            InitializeLanguageDebugInfo();
            InitializeDebugMode();
        }

        protected override void Uninitialize()
        {
            UninitializeDebugMode();
            UninitializeLanguageDebugInfo();

            base.Uninitialize();
        }

        protected override void SetupNewTextView(IVsTextView textView)
        {
            Contract.ThrowIfNull(textView);

            var wpfTextView = EditorAdaptersFactoryService.GetWpfTextView(textView);
            Contract.ThrowIfNull(wpfTextView, "Could not get IWpfTextView for IVsTextView");

            Contract.Assert(!wpfTextView.Properties.ContainsProperty(typeof(AbstractVsTextViewFilter<TPackage, TLanguageService, TProject>)));

            var commandHandlerFactory = Package.ComponentModel.GetService<ICommandHandlerServiceFactory>();
            var workspace = Package.ComponentModel.GetService<VisualStudioWorkspace>();
            var optionsService = workspace.Services.GetService<IOptionService>();

            // The lifetime of CommandFilter is married to the view
            wpfTextView.GetOrCreateAutoClosingProperty(v =>
                new StandaloneCommandFilter<TPackage, TLanguageService, TProject>(
                    (TLanguageService)this, v, commandHandlerFactory, optionsService, EditorAdaptersFactoryService).AttachToVsTextView());

            var openDocument = wpfTextView.TextBuffer.AsTextContainer().GetRelatedDocuments().FirstOrDefault();
            var isOpenMetadataAsSource = openDocument != null && openDocument.Project.Solution.Workspace.Kind == WorkspaceKind.MetadataAsSource;

            ConditionallyCollapseOutliningRegions(textView, wpfTextView, optionsService, isOpenMetadataAsSource);

            // If this is a metadata-to-source view, we want to consider the file read-only
            IVsTextLines vsTextLines;
            if (isOpenMetadataAsSource && ErrorHandler.Succeeded(textView.GetBuffer(out vsTextLines)))
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

        private void ConditionallyCollapseOutliningRegions(IVsTextView textView, IWpfTextView wpfTextView, IOptionService optionsService, bool isOpenMetadataAsSource)
        {
            var outliningManagerService = this.Package.ComponentModel.GetService<IOutliningManagerService>();
            var outliningManager = outliningManagerService.GetOutliningManager(wpfTextView);
            if (outliningManager == null)
            {
                return;
            }

            if (!optionsService.GetOption(FeatureOnOffOptions.Outlining, this.RoslynLanguageName))
            {
                outliningManager.Enabled = false;
            }
            else
            {
                var viewEx = textView as IVsTextViewEx;
                if (viewEx != null)
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
            var outliningTaggerProvider = this.Package.ComponentModel.GetService<OutliningTaggerProvider>();

            var subjectBuffer = wpfTextView.TextBuffer;
            var snapshot = subjectBuffer.CurrentSnapshot;
            var tagger = outliningTaggerProvider.CreateTagger<IOutliningRegionTag>(subjectBuffer);
            using (var disposable = tagger as IDisposable)
            {
                tagger.GetAllTags(new NormalizedSnapshotSpanCollection(snapshot.GetFullSpan()), CancellationToken.None);
            }
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

        protected virtual IVsContainedLanguage CreateContainedLanguage(IVsTextBufferCoordinator bufferCoordinator, TProject project, IVsHierarchy hierarchy, uint itemid)
        {
            return new ContainedLanguage<TPackage, TLanguageService, TProject>(
                bufferCoordinator, this.Package.ComponentModel, project, hierarchy, itemid,
                (TLanguageService)this, SourceCodeKind.Regular);
        }
    }
}
