// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal partial class RequestExecutionQueue
    {
        /// <summary>
        /// Associates LSP document URIs with the roslyn source text containing the LSP document text.
        /// Called via <see cref="DidOpenHandler"/>, <see cref="DidChangeHandler"/> and <see cref="DidCloseHandler"/>
        /// </summary>
        internal interface IDocumentChangeTracker
        {
            void StartTracking(Uri documentUri, SourceText initialText);
            void UpdateTrackedDocument(Uri documentUri, SourceText text);
            void StopTracking(Uri documentUri);
        }

        private class NonMutatingDocumentChangeTracker : IDocumentChangeTracker
        {
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

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly RequestExecutionQueue _queue;

            public TestAccessor(RequestExecutionQueue queue)
                => _queue = queue;

            public List<SourceText> GetTrackedTexts()
                => _queue._lspWorkspaceManager.GetTrackedLspText().Select(i => i.Value).ToList();

            public LspWorkspaceManager GetLspWorkspaceManager() => _queue._lspWorkspaceManager;

            public bool IsComplete() => _queue._queue.IsCompleted && _queue._queue.IsEmpty;
        }
    }
}
