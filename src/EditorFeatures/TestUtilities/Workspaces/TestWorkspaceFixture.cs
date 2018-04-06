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
        private TestWorkspace _workspace;

        public TestWorkspace GetWorkspace()
        {
            _workspace = _workspace ?? CreateWorkspace();
            return _workspace;
        }

        public TestWorkspaceFixture()
        {
        }

        protected abstract TestWorkspace CreateWorkspace();

        public void Dispose()
        {
            if (_workspace != null)
            {
                throw new InvalidOperationException($"Tests which use {nameof(TestWorkspaceFixture)}.{nameof(GetWorkspace)} must call {nameof(DisposeAfterTest)} after each test.");
            }
        }

        public Document UpdateDocument(string text, SourceCodeKind sourceCodeKind, bool cleanBeforeUpdate = true)
        {
            var hostDocument = (GetWorkspace()).Documents.Single();
            var textBuffer = hostDocument.TextBuffer;

            // clear the document
            if (cleanBeforeUpdate)
            {
                UpdateText(hostDocument.TextBuffer, string.Empty);
            }

            // and set the content
            UpdateText(hostDocument.TextBuffer, text);

            (GetWorkspace()).OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind);

            return (GetWorkspace()).CurrentSolution.GetDocument(hostDocument.Id);
        }

        private static void UpdateText(ITextBuffer textBuffer, string text)
        {
            using (var edit = textBuffer.CreateEdit())
            {
                edit.Replace(0, textBuffer.CurrentSnapshot.Length, text);
                edit.Apply();
            }
        }

        public void DisposeAfterTest()
        {
            try
            {
                CloseTextView();
                _workspace?.Dispose();
            }
            finally
            {
                _workspace = null;
            }
        }

        private void CloseTextView()
        {
            // The standard use for TestWorkspaceFixture is to call this method in the test's dispose to make sure it's ready to be used for
            // the next test. But some tests in a test class won't use it, so _workspace might still be null.
            _workspace?.Documents.Single().CloseTextView();

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
