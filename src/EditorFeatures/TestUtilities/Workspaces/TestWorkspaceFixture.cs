// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public abstract class TestWorkspaceFixture : IDisposable
    {
        private Task<TestWorkspace> _workspaceTask;

        public Task<TestWorkspace> GetWorkspaceAsync()
        {
            _workspaceTask = _workspaceTask ?? CreateWorkspaceAsync();
            return _workspaceTask;
        }

        public TestWorkspaceFixture()
        {
        }

        protected abstract Task<TestWorkspace> CreateWorkspaceAsync();

        public void Dispose()
        {
            if (_workspaceTask != null)
            {
                _workspaceTask.Result.Dispose();
                _workspaceTask = null;
            }
        }

        public async Task<Document> UpdateDocumentAsync(string text, SourceCodeKind sourceCodeKind, bool cleanBeforeUpdate = true)
        {
            var hostDocument = (await GetWorkspaceAsync()).Documents.Single();
            var textBuffer = hostDocument.TextBuffer;

            // clear the document
            if (cleanBeforeUpdate)
            {
                UpdateText(hostDocument.TextBuffer, string.Empty);
            }

            // and set the content
            UpdateText(hostDocument.TextBuffer, text);

            (await GetWorkspaceAsync()).OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind);

            return (await GetWorkspaceAsync()).CurrentSolution.GetDocument(hostDocument.Id);
        }

        private static void UpdateText(ITextBuffer textBuffer, string text)
        {
            using (var edit = textBuffer.CreateEdit())
            {
                edit.Replace(0, textBuffer.CurrentSnapshot.Length, text);
                edit.Apply();
            }
        }

        public async Task CloseTextViewAsync()
        {
            (await GetWorkspaceAsync()).Documents.Single().CloseTextView();

            // The editor caches TextFormattingRunProperties instances for better perf, but since things like
            // Brushes are DispatcherObjects, they are tied to the thread they are created on. Since we're going
            // to be run on a different thread, clear out their collection.
            var textFormattingRunPropertiesType = typeof(VisualStudio.Text.Formatting.TextFormattingRunProperties);
            var existingPropertiesField = textFormattingRunPropertiesType.GetField("ExistingProperties", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var existingProperties = (List<VisualStudio.Text.Formatting.TextFormattingRunProperties>)existingPropertiesField.GetValue(null);
            existingProperties.Clear();
        }
    }
}
