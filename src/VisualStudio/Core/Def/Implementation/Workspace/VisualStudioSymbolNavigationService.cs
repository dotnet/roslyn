// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.FindUsages;
using Microsoft.CodeAnalysis.Editor.Implementation.Structure;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class VisualStudioSymbolNavigationService : ForegroundThreadAffinitizedObject, ISymbolNavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
        private readonly VisualStudio14StructureTaggerProvider _outliningTaggerProvider;

        public VisualStudioSymbolNavigationService(
            SVsServiceProvider serviceProvider,
            VisualStudio14StructureTaggerProvider outliningTaggerProvider)
            : base(outliningTaggerProvider.ThreadingContext)
        {
            _serviceProvider = serviceProvider;
            _outliningTaggerProvider = outliningTaggerProvider;

            var componentModel = _serviceProvider.GetService<SComponentModel, IComponentModel>();
            _editorAdaptersFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _textEditorFactoryService = componentModel.GetService<ITextEditorFactoryService>();
            _textDocumentFactoryService = componentModel.GetService<ITextDocumentFactoryService>();
            _metadataAsSourceFileService = componentModel.GetService<IMetadataAsSourceFileService>();
        }

        public bool TryNavigateToSymbol(ISymbol symbol, Project project, OptionSet options, CancellationToken cancellationToken)
        {
            if (project == null || symbol == null)
            {
                return false;
            }

            options = options ?? project.Solution.Workspace.Options;
            symbol = symbol.OriginalDefinition;

            // Prefer visible source locations if possible.
            var sourceLocations = symbol.Locations.Where(loc => loc.IsInSource);
            var visibleSourceLocations = sourceLocations.Where(loc => loc.IsVisibleSourceLocation());
            var sourceLocation = visibleSourceLocations.FirstOrDefault() ?? sourceLocations.FirstOrDefault();

            if (sourceLocation != null)
            {
                var targetDocument = project.Solution.GetDocument(sourceLocation.SourceTree);
                if (targetDocument != null)
                {
                    var editorWorkspace = targetDocument.Project.Solution.Workspace;
                    var navigationService = editorWorkspace.Services.GetService<IDocumentNavigationService>();
                    return navigationService.TryNavigateToSpan(editorWorkspace, targetDocument.Id, sourceLocation.SourceSpan, options);
                }
            }

            // We don't have a source document, so show the Metadata as Source view in a preview tab.

            var metadataLocation = symbol.Locations.Where(loc => loc.IsInMetadata).FirstOrDefault();
            if (metadataLocation == null || !_metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
            {
                return false;
            }

            // Should we prefer navigating to the Object Browser over metadata-as-source?
            if (options.GetOption(VisualStudioNavigationOptions.NavigateToObjectBrowser, project.Language))
            {
                var libraryService = project.LanguageServices.GetService<ILibraryService>();
                if (libraryService == null)
                {
                    return false;
                }

                var compilation = project.GetCompilationAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                var navInfo = libraryService.NavInfoFactory.CreateForSymbol(symbol, project, compilation);
                if (navInfo == null)
                {
                    navInfo = libraryService.NavInfoFactory.CreateForProject(project);
                }

                if (navInfo != null)
                {
                    var navigationTool = _serviceProvider.GetService<SVsObjBrowser, IVsNavigationTool>();
                    return navigationTool.NavigateToNavInfo(navInfo) == VSConstants.S_OK;
                }

                // Note: we'll fallback to Metadata-As-Source if we fail to get IVsNavInfo, but that should never happen.
            }

            // Generate new source or retrieve existing source for the symbol in question
            var allowDecompilation = false;

            // Check whether decompilation is supported for the project. We currently only support this for C# projects.
            if (project.LanguageServices.GetService<IDecompiledSourceService>() != null)
            {
                allowDecompilation = project.Solution.Workspace.Options.GetOption(FeatureOnOffOptions.NavigateToDecompiledSources) && !symbol.IsFromSource();
                if (allowDecompilation && !project.Solution.Workspace.Options.GetOption(FeatureOnOffOptions.AcceptedDecompilerDisclaimer))
                {
                    var notificationService = project.Solution.Workspace.Services.GetService<INotificationService>();
                    allowDecompilation = notificationService.ConfirmMessageBox(ServicesVSResources.Decompiler_Legal_Notice_Message, ServicesVSResources.Decompiler_Legal_Notice_Title, NotificationSeverity.Warning);
                    if (allowDecompilation)
                    {
                        project.Solution.Workspace.Options = project.Solution.Workspace.Options.WithChangedOption(FeatureOnOffOptions.AcceptedDecompilerDisclaimer, true);
                    }
                }
            }

            var result = _metadataAsSourceFileService.GetGeneratedFileAsync(project, symbol, allowDecompilation, cancellationToken).WaitAndGetResult(cancellationToken);

            var vsRunningDocumentTable4 = _serviceProvider.GetService<SVsRunningDocumentTable, IVsRunningDocumentTable4>();
            var fileAlreadyOpen = vsRunningDocumentTable4.IsMonikerValid(result.FilePath);

            var openDocumentService = _serviceProvider.GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>();
            openDocumentService.OpenDocumentViaProject(result.FilePath, VSConstants.LOGVIEWID.TextView_guid, out var localServiceProvider, out var hierarchy, out var itemId, out var windowFrame);

            var documentCookie = vsRunningDocumentTable4.GetDocumentCookie(result.FilePath);

            // The cast from dynamic to object doesn't change semantics, but avoids loading the dynamic binder
            // which saves us JIT time in this method.
            var vsTextBuffer = (IVsTextBuffer)(object)vsRunningDocumentTable4.GetDocumentData(documentCookie);
            var textBuffer = _editorAdaptersFactory.GetDataBuffer(vsTextBuffer);

            if (!fileAlreadyOpen)
            {
                ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_IsProvisional, true));
                ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_OverrideCaption, result.DocumentTitle));
                ErrorHandler.ThrowOnFailure(windowFrame.SetProperty((int)__VSFPROPID5.VSFPROPID_OverrideToolTip, result.DocumentTooltip));
            }

            windowFrame.Show();

            var openedDocument = textBuffer.AsTextContainer().GetRelatedDocuments().FirstOrDefault();
            if (openedDocument != null)
            {
                var editorWorkspace = openedDocument.Project.Solution.Workspace;
                var navigationService = editorWorkspace.Services.GetService<IDocumentNavigationService>();

                return navigationService.TryNavigateToSpan(
                    workspace: editorWorkspace,
                    documentId: openedDocument.Id,
                    textSpan: result.IdentifierLocation.SourceSpan,
                    options: options.WithChangedOption(NavigationOptions.PreferProvisionalTab, true));
            }

            return true;
        }

        public bool TrySymbolNavigationNotify(ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            return TryNotifyForSpecificSymbol(symbol, project, cancellationToken);
        }

        private bool TryNotifyForSpecificSymbol(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            AssertIsForeground();

            var definitionItem = symbol.ToNonClassifiedDefinitionItem(project, includeHiddenLocations: true);
            definitionItem.Properties.TryGetValue(DefinitionItem.RQNameKey1, out var rqName);

            if (!TryGetNavigationAPIRequiredArguments(
                    definitionItem, rqName, cancellationToken,
                    out var hierarchy, out var itemID, out var navigationNotify))
            {
                return false;
            }

            var returnCode = navigationNotify.OnBeforeNavigateToSymbol(
                hierarchy,
                itemID,
                rqName,
                out var navigationHandled);

            return returnCode == VSConstants.S_OK && navigationHandled == 1;
        }

        public bool WouldNavigateToSymbol(
            DefinitionItem definitionItem, Solution solution, CancellationToken cancellationToken,
            out string filePath, out int lineNumber, out int charOffset)
        {
            definitionItem.Properties.TryGetValue(DefinitionItem.RQNameKey1, out var rqName1);
            definitionItem.Properties.TryGetValue(DefinitionItem.RQNameKey2, out var rqName2);

            if (WouldNotifyToSpecificSymbol(definitionItem, rqName1, solution, cancellationToken, out filePath, out lineNumber, out charOffset) ||
                WouldNotifyToSpecificSymbol(definitionItem, rqName2, solution, cancellationToken, out filePath, out lineNumber, out charOffset))
            {
                return true;
            }

            filePath = null;
            lineNumber = 0;
            charOffset = 0;
            return false;
        }

        public bool WouldNotifyToSpecificSymbol(
            DefinitionItem definitionItem, string rqName, Solution solution, CancellationToken cancellationToken,
            out string filePath, out int lineNumber, out int charOffset)
        {
            AssertIsForeground();

            filePath = null;
            lineNumber = 0;
            charOffset = 0;

            if (rqName == null)
            {
                return false;
            }

            if (!TryGetNavigationAPIRequiredArguments(
                    definitionItem, rqName, cancellationToken,
                    out var hierarchy, out var itemID, out var navigationNotify))
            {
                return false;
            }

            var navigateToTextSpan = new Microsoft.VisualStudio.TextManager.Interop.TextSpan[1];

            var queryNavigateStatusCode = navigationNotify.QueryNavigateToSymbol(
                hierarchy,
                itemID,
                rqName,
                out var navigateToHierarchy,
                out var navigateToItem,
                navigateToTextSpan,
                out var wouldNavigate);

            if (queryNavigateStatusCode == VSConstants.S_OK && wouldNavigate == 1)
            {
                navigateToHierarchy.GetCanonicalName(navigateToItem, out filePath);
                lineNumber = navigateToTextSpan[0].iStartLine;
                charOffset = navigateToTextSpan[0].iStartIndex;
                return true;
            }

            return false;
        }

        private bool TryGetNavigationAPIRequiredArguments(
            DefinitionItem definitionItem,
            string rqName,
            CancellationToken cancellationToken,
            out IVsHierarchy hierarchy,
            out uint itemID,
            out IVsSymbolicNavigationNotify navigationNotify)
        {
            AssertIsForeground();

            hierarchy = null;
            navigationNotify = null;
            itemID = (uint)VSConstants.VSITEMID.Nil;

            if (rqName == null)
            {
                return false;
            }

            var sourceLocations = definitionItem.SourceSpans;
            if (!sourceLocations.Any())
            {
                return false;
            }

            var documents = sourceLocations.SelectAsArray(loc => loc.Document);

            // We can only pass one itemid to IVsSymbolicNavigationNotify, so prefer itemids from
            // documents we consider to be "generated" to give external language services the best
            // chance of participating.

            var generatedDocuments = documents.WhereAsArray(d => d.IsGeneratedCode(cancellationToken));

            var documentToUse = generatedDocuments.FirstOrDefault() ?? documents.First();
            if (!TryGetVsHierarchyAndItemId(documentToUse, out hierarchy, out itemID))
            {
                return false;
            }

            navigationNotify = hierarchy as IVsSymbolicNavigationNotify;
            if (navigationNotify == null)
            {
                return false;
            }

            return true;
        }

        private bool TryGetVsHierarchyAndItemId(Document document, out IVsHierarchy hierarchy, out uint itemID)
        {
            AssertIsForeground();

            if (document.Project.Solution.Workspace is VisualStudioWorkspace visualStudioWorkspace)
            {
                hierarchy = visualStudioWorkspace.GetHierarchy(document.Project.Id);
                itemID = hierarchy.TryGetItemId(document.FilePath);

                if (itemID != VSConstants.VSITEMID_NIL)
                {
                    return true;
                }
            }

            hierarchy = null;
            itemID = (uint)VSConstants.VSITEMID.Nil;
            return false;
        }

        private IVsRunningDocumentTable GetRunningDocumentTable()
        {
            var runningDocumentTable = _serviceProvider.GetService<SVsRunningDocumentTable, IVsRunningDocumentTable>();
            Debug.Assert(runningDocumentTable != null);
            return runningDocumentTable;
        }
    }
}
