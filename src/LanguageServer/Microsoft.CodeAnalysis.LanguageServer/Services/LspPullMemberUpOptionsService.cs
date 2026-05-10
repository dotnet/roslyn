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
        // branches represents a programmer error rather than a user-visible state. Fail loudly so that
        // unexpected call sites produce actionable failures instead of latent null propagation.
        Contract.ThrowIfTrue(selectedNodeSymbols.IsDefaultOrEmpty, "Expected at least one selected member.");

        var containingType = selectedNodeSymbols[0].ContainingType;
        Contract.ThrowIfNull(containingType, "Selected member has no containing type.");

        // Prefer the immediate base type, falling back to the first interface implemented by the containing type.
        var destination = containingType.BaseType is { SpecialType: not SpecialType.System_Object } baseType
            ? baseType
            : containingType.Interfaces.FirstOrDefault();

        Contract.ThrowIfNull(destination, "Containing type has no base type or interface to pull members up to.");

        var members = selectedNodeSymbols.SelectAsArray(static s => (s, makeAbstract: false));
        return PullMembersUpOptionsBuilder.BuildPullMembersUpOptions(destination, members);
    }
}
