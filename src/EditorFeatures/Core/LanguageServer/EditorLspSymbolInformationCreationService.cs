// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

[ExportWorkspaceService(typeof(ILspSymbolInformationCreationService), ServiceLayer.Editor), Shared]
internal sealed class EditorLspSymbolInformationCreationService : ILspSymbolInformationCreationService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public EditorLspSymbolInformationCreationService()
    {
    }

    public SymbolInformation Create(string name, string? containerName, LSP.SymbolKind kind, LSP.Location location, Glyph glyph)
    {
        var (guid, id) = glyph.GetVsImageData();
#pragma warning disable CS0618 // SymbolInformation is obsolete, need to switch to DocumentSymbol/WorkspaceSymbol
        return new VSSymbolInformation
        {
            Name = name,
            ContainerName = containerName,
            Kind = kind,
            Location = location,
            Icon = new VSImageId { Guid = guid, Id = id },
        };
#pragma warning restore CS0618
    }
}
