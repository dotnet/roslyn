// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

[ExportWorkspaceService(typeof(IPullMemberUpOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspPullMemberUpOptionsService() : IPullMemberUpOptionsService
{
    public PullMembersUpOptions GetPullMemberUpOptions(Document document, ImmutableArray<ISymbol> selectedNodeSymbols)
    {
        // The refactoring provider only offers this action after validating that there is at least one
        // selected member, a containing type, and at least one valid destination, so each of these
        // branches represents a programmer error rather than a user-visible state. Throw a contextual
        // exception so unexpected LSP failures remain diagnosable.
        if (selectedNodeSymbols.IsDefaultOrEmpty)
            throw CreateInvalidPullMemberUpStateException("Expected at least one selected member.", document, selectedNodeSymbols);

        var containingType = selectedNodeSymbols[0].ContainingType;
        if (containingType is null)
        {
            var selectedSymbol = selectedNodeSymbols[0].ToDisplayString();
            throw CreateInvalidPullMemberUpStateException($"Selected member '{selectedSymbol}' has no containing type.", document, selectedNodeSymbols);
        }

        // Prefer the immediate base type, falling back to the first interface implemented by the containing type.
        var destination = containingType.BaseType is { SpecialType: not SpecialType.System_Object } baseType
            ? baseType
            : containingType.Interfaces.FirstOrDefault();

        if (destination is null)
        {
            throw CreateInvalidPullMemberUpStateException(
                $"Containing type '{containingType.ToDisplayString()}' has no base type or interface to pull members up to.",
                document,
                selectedNodeSymbols);
        }

        var members = selectedNodeSymbols.SelectAsArray(static s => (s, makeAbstract: false));
        return PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(destination, members);
    }

    private static InvalidOperationException CreateInvalidPullMemberUpStateException(
        string message, Document document, ImmutableArray<ISymbol> selectedNodeSymbols)
    {
        var documentName = document.Name;
        var selectedSymbols = FormatSelectedSymbols(selectedNodeSymbols);
        return new InvalidOperationException($"{message} Document='{documentName}', SelectedSymbols=[{selectedSymbols}]");
    }

    private static string FormatSelectedSymbols(ImmutableArray<ISymbol> selectedNodeSymbols)
        => selectedNodeSymbols.IsDefaultOrEmpty
            ? "<none>"
            : string.Join(", ", selectedNodeSymbols.Select(static symbol => symbol.ToDisplayString()));
}
