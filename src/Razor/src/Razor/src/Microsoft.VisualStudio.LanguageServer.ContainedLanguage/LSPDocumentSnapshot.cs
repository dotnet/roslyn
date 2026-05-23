// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public abstract class LSPDocumentSnapshot
{
    public abstract int Version { get; }

    public abstract Uri Uri { get; }

    public abstract ITextSnapshot Snapshot { get; }

    public abstract IReadOnlyList<VirtualDocumentSnapshot> VirtualDocuments { get; }

    public bool TryGetVirtualDocument<TVirtualDocument>([NotNullWhen(true)] out TVirtualDocument? virtualDocument)
        where TVirtualDocument : VirtualDocumentSnapshot
    {
        virtualDocument = null;

        for (var i = 0; i < VirtualDocuments.Count; i++)
        {
            if (VirtualDocuments[i] is TVirtualDocument actualVirtualDocument)
            {
                virtualDocument = actualVirtualDocument;
                return true;
            }
        }

        return false;
    }

    public bool TryGetAllVirtualDocuments<TVirtualDocument>([NotNullWhen(true)] out TVirtualDocument[]? virtualDocuments)
        where TVirtualDocument : VirtualDocumentSnapshot
    {
        List<TVirtualDocument>? actualVirtualDocuments = null;

        for (var i = 0; i < VirtualDocuments.Count; i++)
        {
            if (VirtualDocuments[i] is TVirtualDocument actualVirtualDocument)
            {
                actualVirtualDocuments ??= [];
                actualVirtualDocuments.Add(actualVirtualDocument);
            }
        }

        virtualDocuments = actualVirtualDocuments?.ToArray();
        return virtualDocuments is not null;
    }

    internal bool TryGetAllVirtualDocumentsAsArray<TVirtualDocument>([NotNullWhen(true)] out ImmutableArray<TVirtualDocument> virtualDocuments)
        where TVirtualDocument : VirtualDocumentSnapshot
    {
        var documents = VirtualDocuments;
        using var actualVirtualDocuments = new PooledArrayBuilder<TVirtualDocument>(documents.Count);

        for (var i = 0; i < documents.Count; i++)
        {
            if (documents[i] is TVirtualDocument actualVirtualDocument)
            {
                actualVirtualDocuments.Add(actualVirtualDocument);
            }
        }

        virtualDocuments = actualVirtualDocuments.ToImmutableAndClear();
        return virtualDocuments.Length > 0;
    }
}
