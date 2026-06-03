// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

internal abstract class LSPDocumentChangeListener
{
    public abstract void Changed(
        LSPDocumentSnapshot? old,
        LSPDocumentSnapshot? @new,
        VirtualDocumentSnapshot? virtualOld,
        VirtualDocumentSnapshot? virtualNew,
        LSPDocumentChangeKind kind);
}
