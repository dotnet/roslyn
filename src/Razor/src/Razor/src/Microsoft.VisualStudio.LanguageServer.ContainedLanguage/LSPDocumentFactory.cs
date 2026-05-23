// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

internal abstract class LSPDocumentFactory
{
    public abstract LSPDocument Create(ITextBuffer buffer);

    internal virtual bool TryRefreshVirtualDocuments(LSPDocument document)
    {
        // No-op in the default implementation.
        return false;
    }
}
