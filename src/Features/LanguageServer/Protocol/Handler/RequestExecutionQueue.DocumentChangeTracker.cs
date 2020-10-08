// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
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

            internal void StartTracking(Document document)
            {
                var key = (document.Project.Solution.Workspace, document.Id);

                Contract.ThrowIfTrue(_trackedDocuments.ContainsKey(key), "didOpen received for an already open document.");

                _trackedDocuments.Add(key, document);
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

                _trackedDocuments.Remove(key);
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
