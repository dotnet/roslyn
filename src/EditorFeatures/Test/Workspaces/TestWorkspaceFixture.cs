// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public abstract class TestWorkspaceFixture : IDisposable
    {
        public TestWorkspace Workspace { get; protected set; }

        public TestWorkspaceFixture()
        {
        }

        public void Dispose()
        {
            if (this.Workspace != null)
            {
                this.Workspace.Dispose();
                this.Workspace = null;
            }
        }

        public Document UpdateDocument(string text, SourceCodeKind sourceCodeKind, bool cleanBeforeUpdate = true)
        {
            var hostDocument = this.Workspace.Documents.Single();
            var textBuffer = hostDocument.TextBuffer;

            // clear the document
            if (cleanBeforeUpdate)
            {
                UpdateText(hostDocument.TextBuffer, string.Empty);
            }

            // and set the content
            UpdateText(hostDocument.TextBuffer, text);

            this.Workspace.OnDocumentSourceCodeKindChanged(hostDocument.Id, sourceCodeKind);

            return this.Workspace.CurrentSolution.GetDocument(hostDocument.Id);
        }

        private static void UpdateText(ITextBuffer textBuffer, string text)
        {
            using (var edit = textBuffer.CreateEdit())
            {
                edit.Replace(0, textBuffer.CurrentSnapshot.Length, text);
                edit.Apply();
            }
        }
    }
}
