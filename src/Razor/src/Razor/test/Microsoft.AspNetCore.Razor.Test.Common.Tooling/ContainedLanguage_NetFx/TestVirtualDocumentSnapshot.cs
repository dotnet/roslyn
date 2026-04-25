// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.AspNetCore.Razor.Test.Common.Editor;

internal class TestVirtualDocumentSnapshot : VirtualDocumentSnapshot
{
    private readonly long? _hostDocumentSyncVersion;

    public TestVirtualDocumentSnapshot(Uri uri, long? hostDocumentVersion) : this(uri, hostDocumentVersion, snapshot: null, state: null)
    {
    }

    public TestVirtualDocumentSnapshot(Uri uri, long? hostDocumentVersion, ITextSnapshot snapshot, object state)
    {
        Uri = uri;
        _hostDocumentSyncVersion = hostDocumentVersion;
        Snapshot = snapshot;
        State = state;
    }

    public override Uri Uri { get; }

    public override ITextSnapshot Snapshot { get; }

    public override long? HostDocumentSyncVersion => _hostDocumentSyncVersion;

    public object State { get; }

    public TestVirtualDocumentSnapshot Fork(int hostDocumentVersion) => new(Uri, hostDocumentVersion);
}
