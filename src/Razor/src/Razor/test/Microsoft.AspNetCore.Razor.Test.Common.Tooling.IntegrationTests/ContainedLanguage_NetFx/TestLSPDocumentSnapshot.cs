// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.Editor;

internal class TestLSPDocumentSnapshot : LSPDocumentSnapshot
{
    public TestLSPDocumentSnapshot(Uri uri, int version, params VirtualDocumentSnapshot[] virtualDocuments)
        : this(uri, version, snapshotContent: "Hello World", virtualDocuments)
    {
    }

    public TestLSPDocumentSnapshot(Uri uri, int version, string snapshotContent, params VirtualDocumentSnapshot[] virtualDocuments)
    {
        Uri = uri;
        Version = version;
        VirtualDocuments = virtualDocuments;
        var snapshot = new StringTextSnapshot(snapshotContent);
        _ = new TestTextBuffer(snapshot);
        Snapshot = snapshot;
    }

    public override int Version { get; }

    public override Uri Uri { get; }

    public override ITextSnapshot Snapshot { get; }

    public override IReadOnlyList<VirtualDocumentSnapshot> VirtualDocuments { get; }

    public TestLSPDocumentSnapshot Fork(int version, params VirtualDocumentSnapshot[] virtualDocuments) => new(Uri, version, virtualDocuments);
}
