// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.UnitTests.CodeModel;

internal sealed class MockInvisibleEditor : IInvisibleEditor
{
    private readonly DocumentId _documentId;
    private readonly EditorTestWorkspace _workspace;

    public MockInvisibleEditor(DocumentId document, EditorTestWorkspace workspace)
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
