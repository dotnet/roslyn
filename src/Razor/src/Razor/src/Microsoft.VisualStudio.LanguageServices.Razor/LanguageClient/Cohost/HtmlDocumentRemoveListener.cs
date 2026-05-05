// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[ContentType(RazorConstants.RazorLSPContentTypeName)]
[Export(typeof(LSPDocumentChangeListener))]
[method: ImportingConstructor]
internal sealed partial class HtmlDocumentRemoveListener(
    IHtmlDocumentSynchronizer htmlDocumentSynchronizer)
    : LSPDocumentChangeListener
{
    private readonly IHtmlDocumentSynchronizer _htmlDocumentSynchronizer = htmlDocumentSynchronizer;

    public override void Changed(LSPDocumentSnapshot? old, LSPDocumentSnapshot? @new, VirtualDocumentSnapshot? virtualOld, VirtualDocumentSnapshot? virtualNew, LSPDocumentChangeKind kind)
    {
        if (kind == LSPDocumentChangeKind.Removed && old is not null)
        {
            _htmlDocumentSynchronizer.DocumentRemoved(old.Uri, CancellationToken.None);
        }
    }
}
