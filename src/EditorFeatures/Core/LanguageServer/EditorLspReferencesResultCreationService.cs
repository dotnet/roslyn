// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Text.Adornments;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

[ExportWorkspaceService(typeof(ILspReferencesResultCreationService), ServiceLayer.Editor), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditorLspReferencesResultCreationService() : ILspReferencesResultCreationService
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
        // TO-DO: The Origin property should be added once Rich-Nav is completed.
        // https://github.com/dotnet/roslyn/issues/42847
        var result = new VSInternalReferenceItem
        {
            DefinitionId = definitionId,
            DefinitionText = definitionText,    // Only definitions should have a non-null DefinitionText
            DefinitionIcon = new ImageElement(definitionGlyph.ToLSPImageId()),
            DisplayPath = location?.Uri.LocalPath,
            Id = id,
            Kind = symbolUsageInfo.HasValue ? ProtocolConversions.SymbolUsageInfoToReferenceKinds(symbolUsageInfo.Value) : [],
            ResolutionStatus = VSInternalResolutionStatusKind.ConfirmedAsReference,
            Text = text,
        };

        // There are certain items that may not have locations, such as namespace definitions.
        if (location != null)
            result.Location = location;

        if (documentSpan is var (document, _))
        {
            result.DocumentName = document.Name;
            result.ProjectName = document.Project.Name;
        }

        foreach (var (key, value) in properties)
        {
            if (key == AbstractReferenceFinder.ContainingMemberInfoPropertyName)
                result.ContainingMember = value;
            else if (key == AbstractReferenceFinder.ContainingTypeInfoPropertyName)
                result.ContainingType = value;
        }

        return result;
    }
}
