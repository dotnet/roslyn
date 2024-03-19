// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CS0618 // Type or member is obsolete

using System;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.Venus;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.VSTypeScript.Api;

internal readonly struct VSTypeScriptContainedDocumentWrapper
{
    private readonly ContainedDocument _underlyingObject;

    public VSTypeScriptContainedDocumentWrapper(ContainedDocument underlyingObject)
        => _underlyingObject = underlyingObject;

    public bool IsDefault => _underlyingObject == null;

    public static bool TryGetContainedDocument(DocumentId documentId, out VSTypeScriptContainedDocumentWrapper document)
    {
        // TypeScript only calls this to immediately check if the document is a ContainedDocument. Because of that we can just check for
        // ContainedDocuments
        var containedDocument = ContainedDocument.TryGetContainedDocument(documentId);
        if (containedDocument != null)
        {
            document = new VSTypeScriptContainedDocumentWrapper(containedDocument);
            return true;
        }

        document = default;
        return false;
    }

    public void Dispose()
        => _underlyingObject.Dispose();

    public ITextBuffer SubjectBuffer
        => _underlyingObject.SubjectBuffer;

    public IVsContainedLanguageHost Host
        => _underlyingObject.ContainedLanguageHost;
}
