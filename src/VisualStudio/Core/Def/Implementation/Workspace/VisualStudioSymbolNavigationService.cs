// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.RQName;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal partial class VisualStudioSymbolNavigationService : ISymbolNavigationService
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

        public bool TryNavigateToSymbol(ISymbol symbol, Project project, bool usePreviewTab = false)
        {
            if (project == null || symbol == null)
            {
                return false;
            }

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
                    return navigationService.TryNavigateToSpan(editorWorkspace, targetDocument.Id, sourceLocation.SourceSpan, usePreviewTab);
                }
            }

            // We don't have a source document, so show the Metadata as Source view in a preview tab.

            var metadataLocation = symbol.Locations.Where(loc => loc.IsInMetadata).FirstOrDefault();
            if (metadataLocation == null || !_metadataAsSourceFileService.IsNavigableMetadataSymbol(symbol))
            {
                return false;
            }

            // Generate new source or retrieve existing source for the symbol in question
            var result = _metadataAsSourceFileService.GetGeneratedFileAsync(project, symbol).WaitAndGetResult(CancellationToken.None);

            var vsRunningDocumentTable4 = GetService<SVsRunningDocumentTable, IVsRunningDocumentTable4>();
            var fileAlreadyOpen = vsRunningDocumentTable4.IsMonikerValid((string)result.FilePath);

            var openDocumentService = GetService<SVsUIShellOpenDocument, IVsUIShellOpenDocument>();

            IVsUIHierarchy hierarchy;
            uint itemId;
            IOleServiceProvider localServiceProvider;
            IVsWindowFrame windowFrame;
            openDocumentService.OpenDocumentViaProject(result.FilePath, VSConstants.LOGVIEWID.TextView_guid, out localServiceProvider, out hierarchy, out itemId, out windowFrame);

            var documentCookie = vsRunningDocumentTable4.GetDocumentCookie(result.FilePath);

            var vsTextBuffer = (IVsTextBuffer)vsRunningDocumentTable4.GetDocumentData(documentCookie);
            var textBuffer = _editorAdaptersFactory.GetDataBuffer((IVsTextBuffer)vsTextBuffer);

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
                return navigationService.TryNavigateToSpan(editorWorkspace, openedDocument.Id, result.IdentifierLocation.SourceSpan, usePreviewTab: true);
            }

            return true;
        }

        public bool TrySymbolNavigationNotify(ISymbol symbol, Solution solution)
        {
            foreach (var s in GetAllNavigationSymbols(symbol))
            {
                if (TryNotifyForSpecificSymbol(s, solution))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// If the symbol being navigated to is a constructor, then try navigating to the
        /// constructor itself. If no third parties choose to navigate to the constructor, then try
        /// the constructor's containing type.
        /// </summary>
        private IEnumerable<ISymbol> GetAllNavigationSymbols(ISymbol symbol)
        {
            yield return symbol;

            if (symbol.IsConstructor())
            {
                yield return symbol.ContainingType;
            }
        }

        private bool TryNotifyForSpecificSymbol(ISymbol symbol, Solution solution)
        {
            IVsHierarchy hierarchy;
            IVsSymbolicNavigationNotify navigationNotify;
            string rqname;
            if (!TryGetNavigationAPIRequiredArguments(symbol, solution, out hierarchy, out navigationNotify, out rqname))
            {
                return false;
            }

            int navigationHandled;
            int returnCode = navigationNotify.OnBeforeNavigateToSymbol(
                hierarchy,
                (uint)VSConstants.VSITEMID.Nil,
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
            foreach (var s in GetAllNavigationSymbols(symbol))
            {
                if (WouldNotifyToSpecificSymbol(s, solution, out filePath, out lineNumber, out charOffset))
                {
                    return true;
                }
            }

            filePath = null;
            lineNumber = 0;
            charOffset = 0;
            return false;
        }

        public bool WouldNotifyToSpecificSymbol(ISymbol symbol, Solution solution, out string filePath, out int lineNumber, out int charOffset)
        {
            filePath = null;
            lineNumber = 0;
            charOffset = 0;

            IVsHierarchy hierarchy;
            IVsSymbolicNavigationNotify navigationNotify;
            string rqname;
            if (!TryGetNavigationAPIRequiredArguments(symbol, solution, out hierarchy, out navigationNotify, out rqname))
            {
                return false;
            }

            IVsHierarchy navigateToHierarchy;
            uint navigateToItem;
            int wouldNavigate;
            var navigateToTextSpan = new Microsoft.VisualStudio.TextManager.Interop.TextSpan[1];

            int queryNavigateStatusCode = navigationNotify.QueryNavigateToSymbol(
                hierarchy,
                (uint)VSConstants.VSITEMID.Nil,
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

        private bool TryGetNavigationAPIRequiredArguments(ISymbol symbol, Solution solution, out IVsHierarchy hierarchy, out IVsSymbolicNavigationNotify navigationNotify, out string rqname)
        {
            hierarchy = null;
            navigationNotify = null;
            rqname = null;

            if (!symbol.Locations.Any() || !symbol.Locations[0].IsInSource)
            {
                return false;
            }

            var document = solution.GetDocument(symbol.Locations[0].SourceTree);
            if (document == null)
            {
                return false;
            }

            hierarchy = GetVsHierarchy(document.Project);

            navigationNotify = hierarchy as IVsSymbolicNavigationNotify;
            if (navigationNotify == null)
            {
                return false;
            }

            rqname = LanguageServices.RQName.From(symbol);
            return rqname != null;
        }

        private IVsHierarchy GetVsHierarchy(Project project)
        {
            var visualStudioWorkspace = project.Solution.Workspace as VisualStudioWorkspaceImpl;
            if (visualStudioWorkspace != null)
            {
                var hierarchy = visualStudioWorkspace.GetHierarchy(project.Id);
                return hierarchy;
            }

            return null;
        }

        private I GetService<S, I>()
        {
            var service = (I)_serviceProvider.GetService(typeof(S));
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
