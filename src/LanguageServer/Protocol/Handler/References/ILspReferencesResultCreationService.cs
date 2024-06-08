// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Text.Adornments;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal interface ILspReferencesResultCreationService : IWorkspaceService
{
    SumType<VSInternalReferenceItem, LSP.Location>? CreateReference(
        int definitionId,
        int id,
        ClassifiedTextElement text,
        DocumentSpan? documentSpan,
        ImmutableArray<(string key, string value)> properties,
        ClassifiedTextElement? definitionText,
        Glyph definitionGlyph,
        SymbolUsageInfo? symbolUsageInfo,
        LSP.Location? location);
}

[ExportWorkspaceService(typeof(ILspReferencesResultCreationService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultLspReferencesResultCreationService() : ILspReferencesResultCreationService
{
    public SumType<VSInternalReferenceItem, LSP.Location>? CreateReference(
        int definitionId,
        int id,
        ClassifiedTextElement text,
        DocumentSpan? documentSpan,
        ImmutableArray<(string key, string value)> properties,
        ClassifiedTextElement? definitionText,
        Glyph definitionGlyph,
        SymbolUsageInfo? symbolUsageInfo,
        LSP.Location? location)
    {
        if (location is null)
            return null;

        return location;
    }
}
