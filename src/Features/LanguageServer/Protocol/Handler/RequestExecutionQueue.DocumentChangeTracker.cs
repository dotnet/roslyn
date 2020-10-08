// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        /// <summary>
        /// Keeps track of changes to documents that are opened in the LSP client. Calls MUST not overlap, so this
        /// should be called from a mutating request handler. See <see cref="RequestExecutionQueue"/> for more details.
        /// </summary>
        internal class DocumentChangeTracker : IWorkspaceService
        {
            private readonly Dictionary<(Workspace, DocumentId), Document> _trackedDocuments = new();

            internal async Task StartTrackingAsync(Document document, string contents, CancellationToken cancellationToken)
            {
                // Add the document and ensure the text we have matches whats on the client
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

                var sourceText = SourceText.From(contents, text.Encoding, text.ChecksumAlgorithm);
                var newDocument = document.WithText(sourceText);

                var key = (document.Project.Solution.Workspace, document.Id);

                Contract.ThrowIfTrue(_trackedDocuments.ContainsKey(key), "didOpen received for an already open document.");

                _trackedDocuments.Add(key, newDocument);
            }

            internal void UpdateTrackedDocument(Document document)
            {
                var key = (document.Project.Solution.Workspace, document.Id);

                Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(key), "didChange received for a document that isn't open.");

                _trackedDocuments[key] = document;
            }

            internal void StopTracking(Document document)
            {
                var key = (document.Project.Solution.Workspace, document.Id);

                Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(key), "didClose received for a document that isn't open.");

                _trackedDocuments.Remove((document.Project.Solution.Workspace, document.Id));
            }

            internal IEnumerable<Document> GetTrackedDocuments()
                => _trackedDocuments.Values;
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly RequestExecutionQueue _queue;

            public TestAccessor(RequestExecutionQueue queue)
                => _queue = queue;

            public List<Document> GetTrackedDocuments()
                => _queue._documentChangeTracker.GetTrackedDocuments().ToList();
        }
    }
}
