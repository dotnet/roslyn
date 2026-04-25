// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;
using System;

namespace Microsoft.AspNetCore.Razor.Test.Common.Editor;

internal class TestVirtualDocument : VirtualDocumentBase<TestVirtualDocumentSnapshot>
{
    public TestVirtualDocument(Uri uri, ITextBuffer textBuffer)
        : base(uri, textBuffer)
    {
    }

    protected override TestVirtualDocumentSnapshot GetUpdatedSnapshot(object? state) => new(Uri, HostDocumentVersion, TextBuffer.CurrentSnapshot, state);
}
