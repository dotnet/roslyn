// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public abstract class VirtualDocument : IDisposable
{
    public abstract Uri Uri { get; }

    public abstract ITextBuffer TextBuffer { get; }

    public abstract VirtualDocumentSnapshot CurrentSnapshot { get; }

    public abstract int HostDocumentVersion { get; }

    public abstract VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, int hostDocumentVersion, object? state);

    public abstract void Dispose();
}
