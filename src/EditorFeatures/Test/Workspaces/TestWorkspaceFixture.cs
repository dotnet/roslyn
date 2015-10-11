// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public abstract class TestWorkspaceFixture : IDisposable
    {
        private TestWorkspace _workspace;

        public TestWorkspace Workspace
        {
            get
            {
                _workspace = _workspace ?? CreateWorkspace();
                return _workspace;
            }
        }

        public TestWorkspaceFixture()
        {
        }

        protected abstract TestWorkspace CreateWorkspace();

        public void Dispose()
        {
            if (_workspace != null)
            {
                _workspace.Dispose();
                _workspace = null;
            }
        }

        public Document UpdateDocument(string text, SourceCodeKind sourceCodeKind, bool cleanBeforeUpdate = true)
        {
            var hostDocument = Workspace.Documents.Single();
            var textBuffer = hostDocument.TextBuffer;

            // clear the document
            if (cleanBeforeUpdate)
            {
                UpdateText(hostDocument.TextBuffer, string.Empty);
            }

            // and set the content
            UpdateText(hostDocument.TextBuffer, text);

            Workspace.OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind);

            return Workspace.CurrentSolution.GetDocument(hostDocument.Id);
        }

        private static void UpdateText(ITextBuffer textBuffer, string text)
        {
            using (var edit = textBuffer.CreateEdit())
            {
                edit.Replace(0, textBuffer.CurrentSnapshot.Length, text);
                edit.Apply();
            }
        }

        public void CloseTextView()
        {
            Workspace.Documents.Single().CloseTextView();

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
