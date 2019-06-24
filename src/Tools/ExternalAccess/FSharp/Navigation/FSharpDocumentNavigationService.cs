// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation
{
    [ExportWorkspaceService(typeof(IFSharpDocumentNavigationService)), Shared]
    internal class FSharpDocumentNavigationService : IFSharpDocumentNavigationService
    {
        /// <summary>
        /// Determines whether it is possible to navigate to the given position in the specified document.
        /// </summary>
        public bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan)
        {
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToSpan(workspace, documentId, textSpan);
        }

        /// <summary>
        /// Determines whether it is possible to navigate to the given line/offset in the specified document.
        /// </summary>
        public bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset)
        {
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToLineAndOffset(workspace, documentId, lineNumber, offset);
        }

        /// <summary>
        /// Determines whether it is possible to navigate to the given virtual position in the specified document.
        /// </summary>
        public bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0)
        {
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.CanNavigateToPosition(workspace, documentId, position, virtualSpace);
        }

        /// <summary>
        /// Navigates to the given position in the specified document, opening it if necessary.
        /// </summary>
        public bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options = null)
        {
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.TryNavigateToSpan(workspace, documentId, textSpan, options);
        }

        /// <summary>
        /// Navigates to the given line/offset in the specified document, opening it if necessary.
        /// </summary>
        public bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options = null)
        {
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.TryNavigateToLineAndOffset(workspace, documentId, lineNumber, offset, options);
        }

        /// <summary>
        /// Navigates to the given virtual position in the specified document, opening it if necessary.
        /// </summary>
        public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0, OptionSet options = null)
        {
            var service = workspace.Services.GetService<IDocumentNavigationService>();
            return service.TryNavigateToPosition(workspace, documentId, position, virtualSpace, options);
        }
    }
}
