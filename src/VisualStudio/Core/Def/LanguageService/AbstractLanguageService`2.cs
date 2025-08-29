// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : AbstractLanguageService
    where TPackage : AbstractPackage<TPackage, TLanguageService>
    where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
{
    internal TPackage Package { get; }

    private readonly Lazy<VsLanguageDebugInfo> _languageDebugInfo;

    internal readonly Lazy<VisualStudioWorkspaceImpl> Workspace;

    protected abstract string ContentTypeName { get; }
    protected abstract string LanguageName { get; }
    protected abstract string RoslynLanguageName { get; }
    protected abstract Guid DebuggerLanguageId { get; }

    protected AbstractLanguageService(TPackage package)
    {
        Package = package;

        Debug.Assert(!this.Package.JoinableTaskFactory.Context.IsOnMainThread, "Language service should be instantiated on background thread");

        this.Workspace = this.Package.ComponentModel.DefaultExportProvider.GetExport<VisualStudioWorkspaceImpl>();

        this._languageDebugInfo = new Lazy<VsLanguageDebugInfo>(CreateLanguageDebugInfo);
    }

    private IThreadingContext ThreadingContext => this.Package.ComponentModel.GetService<IThreadingContext>();

    public override IServiceProvider SystemServiceProvider
        => Package;

    public void Setup(CancellationToken cancellationToken)
    {
        // Start off a background task to prime some components we'll need for editing.
        Task.Run(() =>
        {
            var formatter = this.Workspace.Value.Services.GetLanguageServices(RoslynLanguageName).GetService<ISyntaxFormattingService>();
            formatter?.GetDefaultFormattingRules();
        }, cancellationToken).Forget();
    }

    protected virtual void SetupNewTextView(IVsTextView textView)
    {
        Contract.ThrowIfNull(textView);

        var editorAdaptersFactoryService = this.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>();
        var wpfTextView = editorAdaptersFactoryService.GetWpfTextView(textView);
        Contract.ThrowIfNull(wpfTextView, "Could not get IWpfTextView for IVsTextView");

        Debug.Assert(!wpfTextView.Properties.ContainsProperty(typeof(AbstractVsTextViewFilter)));

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
            var options = BlockStructureOptionsStorage.GetBlockStructureOptions(globalOptions, openDocument.Project.Language, isMetadataAsSource: true);
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

    private VsLanguageDebugInfo CreateLanguageDebugInfo()
    {
        var languageServices = this.Workspace.Value.Services.GetLanguageServices(RoslynLanguageName);

        return new VsLanguageDebugInfo(
            this.DebuggerLanguageId,
            (TLanguageService)this,
            languageServices,
            this.Package.ComponentModel.GetService<IUIThreadOperationExecutor>());
    }

    protected virtual IVsContainedLanguage CreateContainedLanguage(
        IVsTextBufferCoordinator bufferCoordinator, ProjectSystemProject project,
        IVsHierarchy hierarchy, uint itemid)
    {
        return new ContainedLanguage(
            bufferCoordinator,
            this.Package.ComponentModel,
            this.Workspace.Value,
            project.Id,
            project,
            this.LanguageServiceId);
    }
}
