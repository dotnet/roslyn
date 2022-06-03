// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class CodeActionExtensions
    {
        public static async Task<Solution> GetRequiredChangedSolutionAsync(this CodeAction codeAction, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var solution = await codeAction.GetChangedSolutionAsync(progressTracker, cancellationToken).ConfigureAwait(false);
            if (solution is null)
            {
                throw new InvalidOperationException(string.Format(WorkspacesResources.CodeAction__0__did_not_produce_a_changed_solution, codeAction.Title));
            }

            return solution;
        }
    }
}
