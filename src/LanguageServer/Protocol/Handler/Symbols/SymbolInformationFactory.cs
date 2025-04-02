// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal static class SymbolInformationFactory
{
    public static SymbolInformation Create(string name, string? containerName, LSP.SymbolKind kind, LSP.Location location, Glyph glyph, bool supportsVSExtensions)
    {
        if (supportsVSExtensions)
        {
#pragma warning disable CS0618 // SymbolInformation is obsolete, need to switch to DocumentSymbol/WorkspaceSymbol
            return new VSSymbolInformation
            {
                Name = name,
                ContainerName = containerName,
                Kind = kind,
                Location = location,
                Icon = glyph.ToVSImageId(),
            };
#pragma warning restore CS0618
        }
        else
        {
#pragma warning disable CS0618 // SymbolInformation is obsolete, need to switch to DocumentSymbol/WorkspaceSymbol
            return new SymbolInformation()
            {
                Name = name,
                ContainerName = containerName,
                Kind = kind,
                Location = location
            };
#pragma warning restore CS0618
        }
    }
}
