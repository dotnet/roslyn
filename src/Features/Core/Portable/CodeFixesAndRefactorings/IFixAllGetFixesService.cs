// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

internal interface IFixAllGetFixesService : IWorkspaceService
{
    /// <summary>
    /// Computes the fix all occurrences code fix, brings up the preview changes dialog for the fix and
    /// returns the code action operations corresponding to the fix.
    /// </summary>
    Task<ImmutableArray<CodeActionOperation>> GetFixAllOperationsAsync(IFixAllContext fixAllContext, bool showPreviewChangesDialog);

    /// <summary>
    /// Computes the fix all occurrences code fix and returns the changed solution.
    /// </summary>
    Task<Solution?> GetFixAllChangedSolutionAsync(IFixAllContext fixAllContext);

    /// <summary>
    /// Previews the changes that would occur after a code fix and returns the updated solution with those changes.
    /// </summary>
    Solution? PreviewChanges(
        Workspace workspace,
        Solution currentSolution,
        Solution newSolution,
        FixAllKind fixAllKind,
        string previewChangesTitle,
        string topLevelHeader,
        string? language,
        int? correlationId,
        CancellationToken cancellationToken);
}
