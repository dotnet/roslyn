// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PullMemberUp;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.LanguageServer.Services;

[ExportWorkspaceService(typeof(IPullMemberUpOptionsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspPullMemberUpOptionsService() : IPullMemberUpOptionsService
{
    public PullMembersUpOptions? GetPullMemberUpOptions(Document document, ImmutableArray<ISymbol> selectedNodeSymbols)
    {
        // Pick the first valid destination: any base type for fields, plus interfaces for non-fields.
        // Mirrors AbstractPullMemberUpRefactoringProvider.FindAllValidDestinations but takes the first
        // hit instead of presenting a dialog.
        var containingType = selectedNodeSymbols.FirstOrDefault()?.ContainingType;
        if (containingType is null)
            return null;
        var allDestinations = selectedNodeSymbols.All(m => m.IsKind(SymbolKind.Field))
            ? containingType.GetBaseTypes().ToImmutableArray()
            : [.. containingType.AllInterfaces, .. containingType.GetBaseTypes()];

        var destination = allDestinations.FirstOrDefault(
            d => MemberAndDestinationValidator.IsDestinationValid(document.Project.Solution, d, CancellationToken.None));

        if (destination is null)
            return null;

        var members = selectedNodeSymbols.SelectAsArray(s => (s, makeAbstract: false));
        return PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(destination, members);
    }
}
