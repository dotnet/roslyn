// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        internal interface IDocumentChangeTracker
        {
            void StartTracking(Uri documentUri, SourceText initialText);
            void UpdateTrackedDocument(Uri documentUri, SourceText text);
            void StopTracking(Uri documentUri);
            bool IsTracking(Uri documentUri);
            IEnumerable<(Uri DocumentUri, SourceText Text)> GetTrackedDocuments();
            SourceText GetTrackedDocumentSourceText(Uri documentUri);
        }

        private class NonMutatingDocumentChangeTracker : IDocumentChangeTracker
        {
            private readonly DocumentChangeTracker _tracker;

            public NonMutatingDocumentChangeTracker(DocumentChangeTracker tracker)
            {
                _tracker = tracker;
            }

            public IEnumerable<(Uri DocumentUri, SourceText Text)> GetTrackedDocuments()
                => _tracker.GetTrackedDocuments();

            public SourceText GetTrackedDocumentSourceText(Uri documentUri)
            {
                Contract.Fail("Mutating documents not allowed in a non-mutating request handler");
                throw new NotImplementedException();
            }

            public bool IsTracking(Uri documentUri)
                => _tracker.IsTracking(documentUri);

            public void StartTracking(Uri documentUri, SourceText initialText)
            {
                Contract.Fail("Mutating documents not allowed in a non-mutating request handler");
                throw new NotImplementedException();
            }

            public void StopTracking(Uri documentUri)
            {
                Contract.Fail("Mutating documents not allowed in a non-mutating request handler");
                throw new NotImplementedException();
            }

            public void UpdateTrackedDocument(Uri documentUri, SourceText text)
            {
                Contract.Fail("Mutating documents not allowed in a non-mutating request handler");
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Keeps track of changes to documents that are opened in the LSP client. Calls MUST not overlap, so this
        /// should be called from a mutating request handler. See <see cref="RequestExecutionQueue"/> for more details.
        /// </summary>
        internal class DocumentChangeTracker : IWorkspaceService, IDocumentChangeTracker
        {
            private readonly Dictionary<Uri, SourceText> _trackedDocuments = new();
            private readonly LspMiscellaneousFilesWorkspace _lspMiscellaneousFilesWorkspace;
            private readonly ILspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

            public DocumentChangeTracker(LspMiscellaneousFilesWorkspace lspMiscellaneousFilesWorkspace, ILspWorkspaceRegistrationService lspWorkspaceRegistrationService)
            {
                _lspMiscellaneousFilesWorkspace = lspMiscellaneousFilesWorkspace;
                _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
            }

            public bool IsTracking(Uri documentUri)
                => _trackedDocuments.ContainsKey(documentUri);

            public void StartTracking(Uri documentUri, SourceText initialText)
            {
                Contract.ThrowIfTrue(_trackedDocuments.ContainsKey(documentUri), $"didOpen received for {documentUri} which is already open.");

                _trackedDocuments.Add(documentUri, initialText);

                // If we can't find the document in any of the registered workspaces, add it to our loose files workspace.
                if (!IsPresentInRegisteredWorkspaces(documentUri, _lspWorkspaceRegistrationService))
                {
                    _lspMiscellaneousFilesWorkspace.AddMiscellaneousDocument(documentUri);
                }
            }

            public void UpdateTrackedDocument(Uri documentUri, SourceText text)
            {
                Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(documentUri), $"didChange received for {documentUri} which is not open.");

                _trackedDocuments[documentUri] = text;

                // If we see the document has been moved to a registered workspace, remove it from our loose files workspace.
                if (IsPresentInRegisteredWorkspaces(documentUri, _lspWorkspaceRegistrationService))
                {
                    _lspMiscellaneousFilesWorkspace.TryRemoveMiscellaneousDocument(documentUri);
                }
            }

            public SourceText GetTrackedDocumentSourceText(Uri documentUri)
            {
                Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(documentUri), "didChange received for a document that isn't open.");

                return _trackedDocuments[documentUri];
            }

            public void StopTracking(Uri documentUri)
            {
                Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(documentUri), $"didClose received for {documentUri} which is not open.");

                _trackedDocuments.Remove(documentUri);

                // Remove from the lsp misc files workspace if it was added there.
                _lspMiscellaneousFilesWorkspace.TryRemoveMiscellaneousDocument(documentUri);
            }

            public IEnumerable<(Uri DocumentUri, SourceText Text)> GetTrackedDocuments()
                => _trackedDocuments.Select(k => (k.Key, k.Value));

            private static bool IsPresentInRegisteredWorkspaces(Uri uri, ILspWorkspaceRegistrationService lspWorkspaceRegistrationService)
            {
                return lspWorkspaceRegistrationService.GetAllRegistrations().Any(workspace => workspace.CurrentSolution.GetDocuments(uri).Any());
            }
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly RequestExecutionQueue _queue;

            public TestAccessor(RequestExecutionQueue queue)
                => _queue = queue;

            public List<SourceText> GetTrackedTexts()
                => _queue._documentChangeTracker.GetTrackedDocuments().Select(i => i.Text).ToList();

            public LspMiscellaneousFilesWorkspace GetLspMiscellaneousFilesWorkspace() => _queue._lspMiscellaneousFilesWorkspace;

            public bool IsComplete() => _queue._queue.IsCompleted && _queue._queue.IsEmpty;
        }
    }
}
