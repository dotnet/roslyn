// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
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

    public bool? UseSpacesForWhitespace(SourceText text)
        => text.Container.TryGetTextBuffer() is { } buffer ? _indentationManagerService.UseSpacesForWhitespace(buffer, explicitFormat: false) : null;

    public int? GetTabSize(SourceText text)
        => text.Container.TryGetTextBuffer() is { } buffer ? _indentationManagerService.GetTabSize(buffer, explicitFormat: false) : null;

    public int? GetIndentSize(SourceText text)
        => text.Container.TryGetTextBuffer() is { } buffer ? _indentationManagerService.GetIndentSize(buffer, explicitFormat: false) : null;
}
