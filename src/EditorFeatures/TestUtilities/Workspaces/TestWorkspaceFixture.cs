// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Text;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public abstract class TestWorkspaceFixture : IDisposable
    {
        public int Position;
        public string Code;

        private TestWorkspace _workspace;
        private TestHostDocument _currentDocument;

        public TestHostDocument CurrentDocument => _currentDocument ?? _workspace.Documents.Single();

        public TestWorkspace GetWorkspace(TestComposition composition = null)
        {
            _workspace ??= CreateWorkspace(composition);
            return _workspace;
        }

        public TestWorkspace GetWorkspace(string markup, TestComposition composition = null, string workspaceKind = null)
        {
            // If it looks like XML, we'll treat it as XML; any parse error would be rejected and will throw.
            // We'll do a case insensitive search here so if somebody has a lowercase W it'll be tried (and
            // rejected by the XML parser) rather than treated as regular text.
            if (markup.TrimStart().StartsWith("<Workspace>", StringComparison.OrdinalIgnoreCase))
            {
                CloseTextView();
                _workspace?.Dispose();

                _workspace = TestWorkspace.CreateWorkspace(XElement.Parse(markup), composition: composition, workspaceKind: workspaceKind);
                _currentDocument = _workspace.Documents.First(d => d.CursorPosition.HasValue);
                Position = _currentDocument.CursorPosition.Value;
                Code = _currentDocument.GetTextBuffer().CurrentSnapshot.GetText();
                return _workspace;
            }
            else
            {
                MarkupTestFile.GetPosition(markup.NormalizeLineEndings(), out Code, out Position);
                var workspace = GetWorkspace(composition);
                _currentDocument = workspace.Documents.Single();
                return workspace;
            }
        }

        protected abstract TestWorkspace CreateWorkspace(TestComposition composition);

        public void Dispose()
        {
            if (_workspace is null)
                return;

            try
            {
                CloseTextView();
                _currentDocument = null;
                Code = null;
                Position = 0;
                _workspace?.Dispose();
            }
            finally
            {
                _workspace = null;
            }
        }

        public Document UpdateDocument(string text, SourceCodeKind sourceCodeKind, bool cleanBeforeUpdate = true)
        {
            var hostDocument = _currentDocument ?? (GetWorkspace()).Documents.Single();

            // clear the document
            if (cleanBeforeUpdate)
            {
                UpdateText(hostDocument.GetTextBuffer(), string.Empty);
            }

            // and set the content
            UpdateText(hostDocument.GetTextBuffer(), text);

            GetWorkspace().OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind);

            return GetWorkspace().CurrentSolution.GetDocument(hostDocument.Id);
        }

        private static void UpdateText(ITextBuffer textBuffer, string text)
        {
            using (var edit = textBuffer.CreateEdit())
            {
                edit.Replace(0, textBuffer.CurrentSnapshot.Length, text);
                edit.Apply();
            }
        }

        private void CloseTextView()
        {
            // The standard use for TestWorkspaceFixture is to call this method in the test's dispose to make sure it's ready to be used for
            // the next test. But some tests in a test class won't use it, so _workspace might still be null.
            if (_workspace?.Documents != null)
            {
                foreach (var document in _workspace?.Documents)
                {
                    document.CloseTextView();
                }
            }

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
