// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public abstract class VirtualDocumentSnapshot
{
    public abstract Uri Uri { get; }

    public abstract ITextSnapshot Snapshot { get; }

    public abstract long? HostDocumentSyncVersion { get; }
}
