// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MoveStaticMembers;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

[ExportWorkspaceService(typeof(IMoveStaticMembersOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspMoveStaticMembersOptionsService() : IMoveStaticMembersOptionsService
{
    public MoveStaticMembersOptions GetMoveMembersToTypeOptions(
        Document document,
        INamedTypeSymbol selectedType,
        ImmutableArray<ISymbol> selectedNodeSymbols)
    {
        var newTypeName = selectedType.Name + "Helpers";
        var ns = selectedType.ContainingNamespace;
        var fullTypeName = ns is null || ns.IsGlobalNamespace
            ? newTypeName
            : $"{ns.ToDisplayString()}.{newTypeName}";

        var extension = document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb";
        var sourceDir = Path.GetDirectoryName(document.FilePath) ?? string.Empty;
        var fileName = Path.Combine(sourceDir, newTypeName + extension);

        return new MoveStaticMembersOptions(
            fileName,
            fullTypeName,
            selectedNodeSymbols);
    }
}
