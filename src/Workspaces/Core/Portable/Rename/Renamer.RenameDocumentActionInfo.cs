// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Information about rename document calls that allows them to be applied as individual actions.
        /// </summary>
        public sealed class RenameDocumentActionInfo
        {
            internal RenameDocumentActionInfo(ImmutableArray<RenameDocumentAction> actions, Solution solution)
            {
                Solution = solution;
                ApplicableActions = actions;
            }

            /// <summary>
            /// The solution that was used to compute the rename actions
            /// </summary>
            public Solution Solution { get; }

            /// <summary>
            /// All applicable actions computed for the action
            /// </summary>
            public ImmutableArray<RenameDocumentAction> ApplicableActions { get; }

            /// <summary>
            /// Same as calling <see cref="GetSolutionAsync(ImmutableArray{RenameDocumentAction})"/> with 
            /// <see cref="ApplicableActions"/> as the argument
            /// </summary>
            public Task<Solution> GetSolutionAsync()
            => GetSolutionAsync(ApplicableActions);

            /// <summary>
            /// Applies each <see cref="RenameDocumentAction"/> in order and returns the final solution. 
            /// All actions must be contained in <see cref="ApplicableActions" />
            /// </summary>
            public async Task<Solution> GetSolutionAsync(ImmutableArray<RenameDocumentAction> actions)
            {
                var solution = Solution;

                if (actions.Any(a => !ApplicableActions.Contains(a)))
                {
                    throw new InvalidOperationException("Cannot apply action that is not in ApplicableActions");
                }

                foreach (var action in actions)
                {
                    solution = await action.GetModifiedSolutionAsync(solution).ConfigureAwait(false);
                }

                return solution;
            }
        }

    }
}
