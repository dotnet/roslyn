// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Formatting;

[ExportWorkspaceService(typeof(ILegacyIndentationManagerWorkspaceService)), Shared]
internal sealed class LegacyIndentationManagerWorkspaceService : ILegacyIndentationManagerWorkspaceService
{
    private readonly IIndentationManagerService _indentationManagerService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LegacyIndentationManagerWorkspaceService(IIndentationManagerService indentationManagerService)
    {
        _indentationManagerService = indentationManagerService;
    }

    private static ITextBuffer GetRequiredTextBuffer(SourceText text)
        => text.Container.TryGetTextBuffer() ?? throw new InvalidOperationException(
            "We had an open document but it wasn't associated with a buffer. That meant we couldn't apply formatting settings.");

    public bool UseSpacesForWhitespace(SourceText text)
        => _indentationManagerService.UseSpacesForWhitespace(GetRequiredTextBuffer(text), explicitFormat: false);

    public int GetTabSize(SourceText text)
        => _indentationManagerService.GetTabSize(GetRequiredTextBuffer(text), explicitFormat: false);

    public int GetIndentSize(SourceText text)
        => _indentationManagerService.GetIndentSize(GetRequiredTextBuffer(text), explicitFormat: false);
}
