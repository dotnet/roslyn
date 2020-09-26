// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests
{
    using Workspace = CodeAnalysis.Workspace;

    [ExportWorkspaceServiceFactory(typeof(IDocumentNavigationService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    internal class MockDocumentNavigationServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MockDocumentNavigationServiceFactory()
        {
        }

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
