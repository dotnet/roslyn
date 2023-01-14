// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote
{
    [ExportWorkspaceService(typeof(IDocumentTrackingService), ServiceLayer.Host)]
    [Shared]
    internal sealed class ServiceHubDocumentTrackingService : IDocumentTrackingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ServiceHubDocumentTrackingService()
        {
        }

        public bool SupportsDocumentTracking => false;

        public event EventHandler<DocumentId?> ActiveDocumentChanged { add { } remove { } }
        public event EventHandler<EventArgs> NonRoslynBufferTextChanged { add { } remove { } }

        public ImmutableArray<DocumentId> GetVisibleDocuments()
        {
            Fail("Code should not be attempting to obtain visible documents from a stateless remote invocation.");
            return ImmutableArray<DocumentId>.Empty;
        }

        public DocumentId? TryGetActiveDocument()
        {
            Fail("Code should not be attempting to obtain active document from a stateless remote invocation.");
            return null;
        }

        private static void Fail(string message)
        {
            // assert in debug builds to hopefully catch problems in CI
            Debug.Fail(message);

            // record NFW to see who violates contract.
            FatalError.ReportAndCatch(new InvalidOperationException(message));
        }
    }
}
