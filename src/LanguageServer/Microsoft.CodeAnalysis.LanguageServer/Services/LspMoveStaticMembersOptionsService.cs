// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        var typeName = selectedType.Name + "Helpers";
        var namespaceDisplay = selectedType.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : selectedType.ContainingNamespace.ToDisplayString();
        var fullTypeName = string.IsNullOrEmpty(namespaceDisplay)
            ? typeName
            : namespaceDisplay + "." + typeName;

        var extension = document.Project.Language == LanguageNames.CSharp ? ".cs" : ".vb";
        var directory = document.FilePath is { } path ? Path.GetDirectoryName(path) : null;
        var fileName = string.IsNullOrEmpty(directory)
            ? typeName + extension
            : Path.Combine(directory, typeName + extension);

        return new MoveStaticMembersOptions(
            fileName,
            fullTypeName,
            selectedNodeSymbols);
    }
}
