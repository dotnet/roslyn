// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal class HtmlVirtualDocumentSnapshot : VirtualDocumentSnapshot
{
    public HtmlVirtualDocumentSnapshot(
        Uri uri,
        ITextSnapshot snapshot,
        long? hostDocumentSyncVersion,
        object? state)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (snapshot is null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        Uri = uri;
        Snapshot = snapshot;
        HostDocumentSyncVersion = hostDocumentSyncVersion;
        State = state;
    }

    public override Uri Uri { get; }

    public override ITextSnapshot Snapshot { get; }

    public override long? HostDocumentSyncVersion { get; }

    public object? State { get; }
}
