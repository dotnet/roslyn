// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    [Shared]
    [ExportWorkspaceServiceFactory(typeof(IDocumentNavigationService), WorkspaceKind.Test)]
    [PartNotDiscoverable]
    internal class MockDocumentNavigationServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new MockDocumentNavigationService();
        }

        private class MockDocumentNavigationService : IDocumentNavigationService
        {
            public bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset) => true;

            public bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0) => true;

            public bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan) => true;

            public bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options = null) => true;

            public bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0, OptionSet options = null) => true;

            public bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options = null) => true;
        }
    }
}
