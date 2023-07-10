// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.TaskList;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : AbstractLanguageService
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        internal TPackage Package { get; }
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
        internal EditorOptionsService EditorOptionsService { get; private set; }
        internal VisualStudioWorkspaceImpl Workspace { get; private set; }
        internal IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; private set; }
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

        protected AbstractLanguageService(TPackage package)
        {
            Package = package;
        }

        public override IServiceProvider SystemServiceProvider
            => Package;

        /// <summary>
        /// Setup and TearDown go in reverse order.
        /// </summary>
        internal void Setup()
        {
            this.ComAggregate = CreateComAggregate();

            // First, acquire any services we need throughout our lifetime.
            this.GetServices();

            // TODO: Is the below access to component model required or can be removed?
            _ = this.Package.ComponentModel;

            // Start off a background task to prime some components we'll need for editing
            VsTaskLibraryHelper.CreateAndStartTask(VsTaskLibraryHelper.ServiceInstance, VsTaskRunContext.BackgroundThread,
                () => PrimeLanguageServiceComponentsOnBackground());

            // Finally, once our connections are established, set up any initial state that we need.
            // Note: we may be instantiated at any time (including when the IDE is already
            // debugging).  We must not assume anything about our initial state and must instead
            // query for all the information we need at this point.
            this.Initialize();

            _isSetUp = true;
        }

        private object CreateComAggregate()
            => Interop.ComAggregate.CreateAggregatedObject(this);

        internal void TearDown()
        {
            if (!_isSetUp)
            {
                throw new InvalidOperationException();
            }

            _isSetUp = false;
            GC.SuppressFinalize(this);

            this.Uninitialize();
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
            this.EditorOptionsService = this.Package.ComponentModel.GetService<EditorOptionsService>();
            this.Workspace = this.Package.ComponentModel.GetService<VisualStudioWorkspaceImpl>();
            this.EditorAdaptersFactoryService = this.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>();
            this.AnalyzerFileWatcherService = this.Package.ComponentModel.GetService<AnalyzerFileWatcherService>();
        }

        protected virtual void RemoveServices()
        {
            this.EditorAdaptersFactoryService = null;
            this.Workspace = null;
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
        }

        protected virtual void Uninitialize()
        {
            UninitializeLanguageDebugInfo();
        }

        private void PrimeLanguageServiceComponentsOnBackground()
        {
            var formatter = this.Workspace.Services.GetLanguageServices(RoslynLanguageName).GetService<ISyntaxFormattingService>();
            formatter?.GetDefaultFormattingRules();
        }

        protected abstract string ContentTypeName { get; }
        protected abstract string LanguageName { get; }
        protected abstract string RoslynLanguageName { get; }

        protected virtual void SetupNewTextView(IVsTextView textView)
        {
            Contract.ThrowIfNull(textView);

            var wpfTextView = EditorAdaptersFactoryService.GetWpfTextView(textView);
            Contract.ThrowIfNull(wpfTextView, "Could not get IWpfTextView for IVsTextView");

            Debug.Assert(!wpfTextView.Properties.ContainsProperty(typeof(AbstractVsTextViewFilter)));

            var workspace = Package.ComponentModel.GetService<VisualStudioWorkspace>();

            // The lifetime of CommandFilter is married to the view
            wpfTextView.GetOrCreateAutoClosingProperty(v =>
                new StandaloneCommandFilter(
                    v, Package.ComponentModel).AttachToVsTextView());

            var isMetadataAsSource = false;
            var collapseAllImplementations = false;

            var openDocument = wpfTextView.TextBuffer.AsTextContainer().GetRelatedDocuments().FirstOrDefault();
            if (openDocument?.Project.Solution.Workspace is MetadataAsSourceWorkspace masWorkspace)
            {
                isMetadataAsSource = true;

                // If the file is metadata as source, and the user has the preference set to collapse them, then
                // always collapse all metadata as source
                var globalOptions = this.Package.ComponentModel.GetService<IGlobalOptionService>();
                var options = BlockStructureOptionsStorage.GetBlockStructureOptions(globalOptions, openDocument.Project.Language, isMetadataAsSource: masWorkspace is not null);
                collapseAllImplementations = masWorkspace.FileService.ShouldCollapseOnOpen(openDocument.FilePath, options);
            }

            ConditionallyCollapseOutliningRegions(textView, wpfTextView, collapseAllImplementations);

            // If this is a metadata-to-source view, we want to consider the file read-only and prevent
            // it from being re-opened when VS is opened
            if (isMetadataAsSource && ErrorHandler.Succeeded(textView.GetBuffer(out var vsTextLines)))
            {
                Contract.ThrowIfNull(openDocument);

                ErrorHandler.ThrowOnFailure(vsTextLines.GetStateFlags(out var flags));
                flags |= (int)BUFFERSTATEFLAGS.BSF_USER_READONLY;
                ErrorHandler.ThrowOnFailure(vsTextLines.SetStateFlags(flags));

                var runningDocumentTable = (IVsRunningDocumentTable)SystemServiceProvider.GetService(typeof(SVsRunningDocumentTable));
                var runningDocumentTable4 = (IVsRunningDocumentTable4)runningDocumentTable;

                if (runningDocumentTable4.IsMonikerValid(openDocument.FilePath))
                {
                    var cookie = runningDocumentTable4.GetDocumentCookie(openDocument.FilePath);
                    runningDocumentTable.ModifyDocumentFlags(cookie, (uint)_VSRDTFLAGS.RDT_DontAddToMRU | (uint)_VSRDTFLAGS.RDT_CantSave | (uint)_VSRDTFLAGS.RDT_DontAutoOpen, fSet: 1);
                }
            }
        }

        private void ConditionallyCollapseOutliningRegions(IVsTextView textView, IWpfTextView wpfTextView, bool collapseAllImplementations)
        {
            var outliningManagerService = this.Package.ComponentModel.GetService<IOutliningManagerService>();
            var outliningManager = outliningManagerService.GetOutliningManager(wpfTextView);
            if (outliningManager == null)
                return;

            if (textView is IVsTextViewEx viewEx)
            {
                if (collapseAllImplementations)
                {
                    // If this file is a metadata-from-source file, we want to force-collapse any implementations
                    // to keep the display clean and condensed.
                    outliningManager.CollapseAll(wpfTextView.TextBuffer.CurrentSnapshot.GetFullSpan(), c => c.Tag.IsImplementation);
                }
                else
                {
                    // Otherwise, attempt to persist any outlining state we have computed. This
                    // ensures that any new opened files that have any IsDefaultCollapsed spans
                    // will both have them collapsed and remembered in the SUO file.
                    // NOTE: Despite its name, this call will LOAD the state from the SUO file,
                    //       or collapse to definitions if it can't do that.
                    viewEx.PersistOutliningState();
                }
            }
        }

        private void InitializeLanguageDebugInfo()
            => this.LanguageDebugInfo = this.CreateLanguageDebugInfo();

        protected abstract Guid DebuggerLanguageId { get; }

        private VsLanguageDebugInfo CreateLanguageDebugInfo()
        {
            var workspace = this.Workspace;
            var languageServices = workspace.Services.GetLanguageServices(RoslynLanguageName);

            return new VsLanguageDebugInfo(
                this.DebuggerLanguageId,
                (TLanguageService)this,
                languageServices,
                this.Package.ComponentModel.GetService<IThreadingContext>(),
                this.Package.ComponentModel.GetService<IUIThreadOperationExecutor>());
        }

        private void UninitializeLanguageDebugInfo()
            => this.LanguageDebugInfo = null;

        protected virtual IVsContainedLanguage CreateContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator, ProjectSystemProject project,
            IVsHierarchy hierarchy, uint itemid)
        {
            return new ContainedLanguage(
                bufferCoordinator,
                this.Package.ComponentModel,
                this.Workspace,
                project.Id,
                project,
                this.LanguageServiceId);
        }
    }
}
