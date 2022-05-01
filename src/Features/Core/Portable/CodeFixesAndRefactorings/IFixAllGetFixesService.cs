// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings
{
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
        Task<Solution> GetFixAllChangedSolutionAsync(IFixAllContext fixAllContext);
    }
}
