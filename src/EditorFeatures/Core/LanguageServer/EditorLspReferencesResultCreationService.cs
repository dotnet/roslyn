// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Text.Adornments;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

[ExportWorkspaceService(typeof(ILspReferencesResultCreationService), ServiceLayer.Editor), Shared]
internal sealed class EditorLspReferencesResultCreationService : ILspReferencesResultCreationService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EditorLspReferencesResultCreationService()
    {
    }

    public SumType<VSInternalReferenceItem, Roslyn.LanguageServer.Protocol.Location>? CreateReference(
        int definitionId,
        int id,
        ClassifiedTextElement text,
        DocumentSpan? documentSpan,
        ImmutableDictionary<string, string> properties,
        ClassifiedTextElement? definitionText,
        Glyph definitionGlyph,
        SymbolUsageInfo? symbolUsageInfo,
        Roslyn.LanguageServer.Protocol.Location? location)
    {
        // TO-DO: The Origin property should be added once Rich-Nav is completed.
        // https://github.com/dotnet/roslyn/issues/42847
        var imageId = definitionGlyph.GetImageId();
        var result = new VSInternalReferenceItem
        {
            DefinitionId = definitionId,
            DefinitionText = definitionText,    // Only definitions should have a non-null DefinitionText
            DefinitionIcon = new ImageElement(imageId.ToLSPImageId()),
            DisplayPath = location?.Uri.LocalPath,
            Id = id,
            Kind = symbolUsageInfo.HasValue ? ProtocolConversions.SymbolUsageInfoToReferenceKinds(symbolUsageInfo.Value) : [],
            ResolutionStatus = VSInternalResolutionStatusKind.ConfirmedAsReference,
            Text = text,
        };

        // There are certain items that may not have locations, such as namespace definitions.
        if (location != null)
            result.Location = location;

        if (documentSpan != null)
        {
            result.DocumentName = documentSpan.Value.Document.Name;
            result.ProjectName = documentSpan.Value.Document.Project.Name;
        }

        if (properties.TryGetValue(AbstractReferenceFinder.ContainingMemberInfoPropertyName, out var referenceContainingMember))
            result.ContainingMember = referenceContainingMember;

        if (properties.TryGetValue(AbstractReferenceFinder.ContainingTypeInfoPropertyName, out var referenceContainingType))
            result.ContainingType = referenceContainingType;

        return result;
    }
}
