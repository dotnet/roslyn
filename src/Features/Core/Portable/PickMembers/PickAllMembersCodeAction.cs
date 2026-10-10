// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PickMembers;

/// <summary>
/// Wraps a member-picking <see cref="CodeActionWithOptions"/> for hosts that have no member-picking UI (headless LSP).
/// It runs the same generation the dialog would, but over <em>all</em> members
/// </summary>
internal sealed class PickAllMembersCodeAction(
    CodeActionWithOptions pickMembersAction,
    string title,
    Solution solution,
    ImmutableArray<ISymbol> members,
    ImmutableArray<PickMembersOption> options) : CodeAction
{
    public override string Title { get; } = title;

    public override string EquivalenceKey => Title;

    protected override async Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
        IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
    {
        var allMembers = new PickMembersResult(members, options, selectedAll: true);
        var operations = await pickMembersAction.GetOperationsAsync(solution, allMembers, progress, cancellationToken).ConfigureAwait(false);
        return operations.ToImmutableArrayOrEmpty();
    }
}
