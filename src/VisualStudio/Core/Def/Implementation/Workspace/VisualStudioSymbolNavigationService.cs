// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class VisualStudioSymbolNavigationService : ForegroundThreadAffinitizedObject, ISymbolNavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
        private readonly ITextEditorFactoryService _textEditorFactoryService;
        private readonly ITextDocumentFactoryService _textDocumentFactoryService;
        private readonly IMetadataAsSourceFileService _metadataAsSourceFileService;
        private readonly OutliningTaggerProvider _outliningTaggerProvider;

        public VisualStudioSymbolNavigationService(
            SVsServiceProvider serviceProvider,
            OutliningTaggerProvider outliningTaggerProvider)
        {
            _serviceProvider = serviceProvider;
            _outliningTaggerProvider = outliningTaggerProvider;

            var componentModel = GetService<SComponentModel, IComponentModel>();
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
            var sourceLocation = visibleSourceLocations.Any() ? visibleSourceLocations.First() : sourceLocations.FirstOrDefault();

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
                var navInfo = libraryService.NavInfo.CreateForSymbol(symbol, project, compilation);
                if (navInfo == null)
                {
                    navInfo = libraryService.NavInfo.CreateForProject(project);
                }

                if (navInfo == null)
                {
                    return false;
                }

                var navigationTool = GetService<SVsObjBrowser, IVsNavigationTool>();
                return navigationTool.NavigateToNavInfo(navInfo) == VSConstants.S_OK;
            }

            // Generate new source or retrieve existing source for the symbol in question
            var result = _metadataAsSourceFileService.GetGeneratedFileAsync(project, symbol, cancellationToken).WaitAndGetResult(cancellationToken);

            var vsRunningDocumentTable4 = GetService<SVsRunningDocumentTable, IVsRunningDocumentTable4>();
            var fileAlreadyOpen = vsRunningDocumentTable4.IsMonikerValid(result.FilePath);

            var openDocumentService = GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>();

            IVsUIHierarchy hierarchy;
            uint itemId;
            IOleServiceProvider localServiceProvider;
            IVsWindowFrame windowFrame;
            openDocumentService.OpenDocumentViaProject(result.FilePath, VSConstants.LOGVIEWID.TextView_guid, out localServiceProvider, out hierarchy, out itemId, out windowFrame);

            var documentCookie = vsRunningDocumentTable4.GetDocumentCookie(result.FilePath);

            var vsTextBuffer = (IVsTextBuffer)vsRunningDocumentTable4.GetDocumentData(documentCookie);
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
                    options: options.WithChangedOption(NavigationOptions.UsePreviewTab, true));
            }

            return true;
        }

        public bool TrySymbolNavigationNotify(ISymbol symbol, Solution solution)
        {
            return TryNotifyForSpecificSymbol(symbol, solution);
        }

        private bool TryNotifyForSpecificSymbol(ISymbol symbol, Solution solution)
        {
            AssertIsForeground();

            IVsHierarchy hierarchy;
            IVsSymbolicNavigationNotify navigationNotify;
            string rqname;
            uint itemID;
            if (!TryGetNavigationAPIRequiredArguments(symbol, solution, out hierarchy, out itemID, out navigationNotify, out rqname))
            {
                return false;
            }

            int navigationHandled;
            int returnCode = navigationNotify.OnBeforeNavigateToSymbol(
                hierarchy,
                itemID,
                rqname,
                out navigationHandled);

            if (returnCode == VSConstants.S_OK && navigationHandled == 1)
            {
                return true;
            }

            return false;
        }

        public bool WouldNavigateToSymbol(ISymbol symbol, Solution solution, out string filePath, out int lineNumber, out int charOffset)
        {
            if (WouldNotifyToSpecificSymbol(symbol, solution, out filePath, out lineNumber, out charOffset))
            {
                return true;
            }

            // If the symbol being considered is a constructor and no third parties choose to
            // navigate to the constructor, then try the constructor's containing type.
            if (symbol.IsConstructor() && WouldNotifyToSpecificSymbol(symbol.ContainingType, solution, out filePath, out lineNumber, out charOffset))
            {
                return true;
            }

            filePath = null;
            lineNumber = 0;
            charOffset = 0;
            return false;
        }

        public bool WouldNotifyToSpecificSymbol(ISymbol symbol, Solution solution, out string filePath, out int lineNumber, out int charOffset)
        {
            AssertIsForeground();

            filePath = null;
            lineNumber = 0;
            charOffset = 0;

            IVsHierarchy hierarchy;
            IVsSymbolicNavigationNotify navigationNotify;
            string rqname;
            uint itemID;
            if (!TryGetNavigationAPIRequiredArguments(symbol, solution, out hierarchy, out itemID, out navigationNotify, out rqname))
            {
                return false;
            }

            IVsHierarchy navigateToHierarchy;
            uint navigateToItem;
            int wouldNavigate;
            var navigateToTextSpan = new Microsoft.VisualStudio.TextManager.Interop.TextSpan[1];

            int queryNavigateStatusCode = navigationNotify.QueryNavigateToSymbol(
                hierarchy,
                itemID,
                rqname,
                out navigateToHierarchy,
                out navigateToItem,
                navigateToTextSpan,
                out wouldNavigate);

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
            ISymbol symbol,
            Solution solution,
            out IVsHierarchy hierarchy,
            out uint itemID,
            out IVsSymbolicNavigationNotify navigationNotify,
            out string rqname)
        {
            AssertIsForeground();

            hierarchy = null;
            navigationNotify = null;
            rqname = null;
            itemID = (uint)VSConstants.VSITEMID.Nil;

            if (!symbol.Locations.Any())
            {
                return false;
            }

            var sourceLocations = symbol.Locations.Where(loc => loc.IsInSource);
            if (!sourceLocations.Any())
            {
                return false;
            }

            var documents = sourceLocations.Select(loc => solution.GetDocument(loc.SourceTree)).WhereNotNull();
            if (!documents.Any())
            {
                return false;
            }

            // We can only pass one itemid to IVsSymbolicNavigationNotify, so prefer itemids from
            // documents we consider to be "generated" to give external language services the best
            // chance of participating.

            var generatedCodeRecognitionService = solution.Workspace.Services.GetService<IGeneratedCodeRecognitionService>();
            var generatedDocuments = documents.Where(d => generatedCodeRecognitionService.IsGeneratedCode(d));

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

            rqname = LanguageServices.RQName.From(symbol);
            return rqname != null;
        }

        private bool TryGetVsHierarchyAndItemId(Document document, out IVsHierarchy hierarchy, out uint itemID)
        {
            AssertIsForeground();

            var visualStudioWorkspace = document.Project.Solution.Workspace as VisualStudioWorkspaceImpl;
            if (visualStudioWorkspace != null)
            {
                var hostProject = visualStudioWorkspace.GetHostProject(document.Project.Id);
                hierarchy = hostProject.Hierarchy;
                itemID = hostProject.GetDocumentOrAdditionalDocument(document.Id).GetItemId();

                return true;
            }

            hierarchy = null;
            itemID = (uint)VSConstants.VSITEMID.Nil;
            return false;
        }

        private TInterface GetService<TService, TInterface>()
        {
            var service = (TInterface)_serviceProvider.GetService(typeof(TService));
            Debug.Assert(service != null);
            return service;
        }

        private IVsRunningDocumentTable GetRunningDocumentTable()
        {
            var runningDocumentTable = GetService<SVsRunningDocumentTable, IVsRunningDocumentTable>();
            Debug.Assert(runningDocumentTable != null);
            return runningDocumentTable;
        }
    }
}
