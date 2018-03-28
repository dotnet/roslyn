// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.VisualStudio.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel
{
    internal class MockInvisibleEditor : IInvisibleEditor
    {
        private readonly DocumentId _documentId;
        private readonly TestWorkspace _workspace;

        public MockInvisibleEditor(DocumentId document, TestWorkspace workspace)
        {
            _documentId = document;
            _workspace = workspace;
        }

        public Microsoft.VisualStudio.Text.ITextBuffer TextBuffer
        {
            get
            {
                return _workspace.GetTestDocument(_documentId).GetTextBuffer();
            }
        }

        public void Dispose()
        {
        }
    }
}
