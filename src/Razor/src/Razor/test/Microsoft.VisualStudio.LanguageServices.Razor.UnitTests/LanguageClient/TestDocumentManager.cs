// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal sealed class TestDocumentManager : TrackingLSPDocumentManager
{
    private readonly Dictionary<Uri, LSPDocumentSnapshot> _documents = [];

    public int UpdateVirtualDocumentCallCount { get; private set; }

    public override bool TryGetDocument(Uri uri, out LSPDocumentSnapshot lspDocumentSnapshot)
    {
        return _documents.TryGetValue(uri, out lspDocumentSnapshot);
    }

    public void AddDocument(Uri uri, LSPDocumentSnapshot documentSnapshot)
    {
        _documents.Add(uri, documentSnapshot);
    }

    public override void TrackDocument(ITextBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public override void UntrackDocument(ITextBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public override void UpdateVirtualDocument<TVirtualDocument>(Uri hostDocumentUri, IReadOnlyList<ITextChange> changes, int hostDocumentVersion, object? state)
    {
        if (!_documents.TryGetValue(hostDocumentUri, out _))
        {
            return;
        }

        UpdateVirtualDocumentCallCount++;
    }
}
