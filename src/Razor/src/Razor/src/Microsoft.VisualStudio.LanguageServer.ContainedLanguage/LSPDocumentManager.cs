// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public abstract class LSPDocumentManager
{
    public abstract bool TryGetDocument(Uri uri, [NotNullWhen(returnValue: true)] out LSPDocumentSnapshot? lspDocumentSnapshot);

    /// <summary>
    /// Tells each <see cref="LSPDocument" /> to try and refresh the number of virtual documents it contains
    /// if necessary.
    /// </summary>
    public virtual void RefreshVirtualDocuments()
    {
        // No-op in the default implementation.
    }
}
