// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            return (await GetWorkspaceAsync()).UpdateSingleDocument(text, sourceCodeKind, cleanBeforeUpdate);
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
