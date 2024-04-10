// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface ILspSymbolInformationCreationService : IWorkspaceService
    {
        SymbolInformation Create(
            string name, string? containerName, LSP.SymbolKind kind, LSP.Location location, Glyph glyph);
    }

    [ExportWorkspaceService(typeof(ILspSymbolInformationCreationService)), Shared]
    internal sealed class DefaultLspSymbolInformationCreationService : ILspSymbolInformationCreationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultLspSymbolInformationCreationService()
        {
        }

        public SymbolInformation Create(string name, string? containerName, LSP.SymbolKind kind, LSP.Location location, Glyph glyph)
            => new()
            {
                Name = name,
                ContainerName = containerName,
                Kind = kind,
                Location = location,
            };
    }
}
