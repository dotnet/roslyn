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

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

[ExportWorkspaceService(typeof(IPullMemberUpOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspPullMemberUpOptionsService() : IPullMemberUpOptionsService
{
    public PullMembersUpOptions GetPullMemberUpOptions(Document document, ImmutableArray<ISymbol> selectedNodeSymbols)
    {
        if (selectedNodeSymbols.IsDefaultOrEmpty)
            return null!;

        var containingType = selectedNodeSymbols[0].ContainingType;
        if (containingType is null)
            return null!;

        // Prefer the immediate base type, falling back to the first interface implemented by the containing type.
        var destination = containingType.BaseType is { SpecialType: not SpecialType.System_Object } baseType
            ? baseType
            : containingType.Interfaces.FirstOrDefault();

        if (destination is null)
            return null!;

        var members = selectedNodeSymbols.SelectAsArray(static s => (s, makeAbstract: false));
        return PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(destination, members);
    }
}
