// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

/// <summary>
/// The <see cref="VirtualDocumentFactory"/>'s purpose is to create a <see cref="VirtualDocument"/> for a given <see cref="ITextBuffer"/>.
/// These <see cref="VirtualDocument"/>s are addressable via their <see cref="VirtualDocument.Uri"/>'s and represent an embedded, addressable LSP
/// document for a provided <see cref="ITextBuffer"/>.
/// </summary>
public abstract class VirtualDocumentFactory
{
    /// <summary>
    /// Attempts to create a <see cref="VirtualDocument"/> for the provided <paramref name="hostDocumentBuffer"/>.
    /// </summary>
    /// <param name="hostDocumentBuffer">The top-level LSP document buffer.</param>
    /// <param name="virtualDocument">The resultant <see cref="VirtualDocument"/> for the top-level <paramref name="hostDocumentBuffer"/>.</param>
    /// <returns><c>true</c> if a <see cref="VirtualDocument"/> could be created, <c>false</c> otherwise. A result of <c>false</c> typically indicates
    /// that a <see cref="VirtualDocumentFactory"/> was not meant to be called for the given <paramref name="hostDocumentBuffer"/>.</returns>
    public abstract bool TryCreateFor(ITextBuffer hostDocumentBuffer, [NotNullWhen(returnValue: true)] out VirtualDocument? virtualDocument);

    /// <summary>
    /// Attempts to create one or more <see cref="VirtualDocument"/>s for the provided <paramref name="hostDocumentBuffer"/>.
    /// </summary>
    /// <remarks>
    /// If this method returns true, the <see cref="TryCreateFor(ITextBuffer, out VirtualDocument?)"/> method will not be called.
    /// </remarks>
    /// <param name="hostDocumentBuffer">The top-level LSP document buffer.</param>
    /// <param name="virtualDocuments">The resultant <see cref="VirtualDocument"/> array for the top-level <paramref name="hostDocumentBuffer"/>.</param>
    /// <returns><c>true</c> if a <see cref="VirtualDocument"/> could be created, <c>false</c> otherwise. A result of <c>false</c> typically indicates
    /// that a <see cref="VirtualDocumentFactory"/> does not support multiple virtual documents for a single <paramref name="hostDocumentBuffer"/>.</returns>
    public virtual bool TryCreateMultipleFor(ITextBuffer hostDocumentBuffer, [NotNullWhen(returnValue: true)] out VirtualDocument[]? virtualDocuments)
    {
        virtualDocuments = null;
        return false;
    }

    /// <summary>
    /// Refreshes the virtual documents for a given <see cref="LSPDocument"/>. This method is called to allow for factories that support
    /// multiple virtual documents to also have a dynamic number of virtual documents. Only virtual documents owned by the factory should
    /// be refreshed, anything else should be ignored, and added to <paramref name="newVirtualDocuments" /> as-is.
    /// </summary>
    internal virtual bool TryRefreshVirtualDocuments(LSPDocument document, [NotNullWhen(returnValue: true)] out IReadOnlyList<VirtualDocument>? newVirtualDocuments)
    {
        newVirtualDocuments = null;
        return false;
    }
}
