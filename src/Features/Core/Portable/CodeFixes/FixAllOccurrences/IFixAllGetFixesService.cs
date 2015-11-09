// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal interface IFixAllGetFixesService : IWorkspaceService
    {
        /// <summary>
        /// Computes the fix all occurrences code fix, brings up the preview changes dialog for the fix and
        /// returns the code action operations corresponding to the fix.
        /// </summary>
        Task<IEnumerable<CodeActionOperation>> GetFixAllOperationsAsync(FixAllProvider fixAllProvider, FixAllContext fixAllContext, string fixAllTitle, string waitDialogMessage, bool showPreviewChangesDialog);

        /// <summary>
        /// Computes the fix all occurrences code fix and returns the changed solution.
        /// </summary>
        Task<Solution> GetFixAllChangedSolutionAsync(FixAllProvider fixAllProvider, FixAllContext fixAllContext, string fixAllTitle, string waitDialogMessage);
    }
}
