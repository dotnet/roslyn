// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal sealed class RazorLSPContentTypeDefinition
{
    /// <summary>
    /// Exports the Razor LSP content type
    /// </summary>
    [Export]
    [Name(RazorConstants.RazorLSPContentTypeName)]
    [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
    public ContentTypeDefinition? RazorLSPContentType { get; set; }

    // We can't associate the Razor LSP content type with the above file extensions because there's already a content type
    // associated with them. Instead, we utilize our RazorEditorFactory to assign the RazorLSPContentType to .razor/.cshtml
    // files.
}
