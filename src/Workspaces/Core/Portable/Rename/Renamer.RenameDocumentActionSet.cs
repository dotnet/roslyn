// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        /// <summary>
        /// Information about rename document calls that allows them to be applied as individual actions. Actions are individual units of work
        /// that can change the contents of one or more document in the solution. Even if the <see cref="ApplicableActions"/> is empty, the 
        /// document metadata will still be updated by calling <see cref="UpdateSolutionAsync(Solution, ImmutableArray{RenameDocumentAction}, CancellationToken)"/>
        /// <para />
        /// To apply all actions use <see cref="UpdateSolutionAsync(Solution, CancellationToken)"/>, or use a subset
        /// of the actions by calling <see cref="UpdateSolutionAsync(Solution, ImmutableArray{RenameDocumentAction}, CancellationToken)"/>. 
        /// Actions can be applied in any order.
        /// Each action has a description of the changes that it will apply that can be presented to a user.
        /// </summary>
        public sealed class RenameDocumentActionSet
        {
            private readonly DocumentId _documentId;
            private readonly string _documentName;
            private readonly ImmutableArray<string> _documentFolders;
            private readonly DocumentRenameOptions _options;

            internal RenameDocumentActionSet(
                ImmutableArray<RenameDocumentAction> actions,
                DocumentId documentId,
                string documentName,
                ImmutableArray<string> documentFolders,
                DocumentRenameOptions options)
            {
                ApplicableActions = actions;
                _documentFolders = documentFolders;
                _documentId = documentId;
                _documentName = documentName;
                _options = options;
            }

            /// <summary>
            /// All applicable actions computed for the action. Action set may be empty, which represents updates to document 
            /// contents rather than metadata. Document metadata will still not be updated unless <see cref="UpdateSolutionAsync(Solution, ImmutableArray{RenameDocumentAction}, CancellationToken)" /> 
            /// is called.
            /// </summary>
            public ImmutableArray<RenameDocumentAction> ApplicableActions { get; }

            /// <summary>
            /// Same as calling <see cref="UpdateSolutionAsync(Solution, ImmutableArray{RenameDocumentAction}, CancellationToken)"/> with 
            /// <see cref="ApplicableActions"/> as the argument
            /// </summary>
            public Task<Solution> UpdateSolutionAsync(Solution solution, CancellationToken cancellationToken)
                => UpdateSolutionAsync(solution, ApplicableActions, cancellationToken);

            /// <summary>
            /// Applies each <see cref="RenameDocumentAction"/> in order and returns the final solution. 
            /// All actions must be contained in <see cref="ApplicableActions" />
            /// </summary>
            /// <remarks>
            /// An empty action set is still allowed and will return a modified solution
            /// that will update the document properties as appropriate. This means we 
            /// can still support when <see cref="ApplicableActions"/> is empty. It's desirable
            /// that consumers can call a rename API to produce a <see cref="RenameDocumentActionSet"/> and
            /// immediately call <see cref="UpdateSolutionAsync(Solution, ImmutableArray{RenameDocumentAction}, CancellationToken)"/> without
            /// having to inspect the returned <see cref="ApplicableActions"/>.
            /// </remarks>
            public async Task<Solution> UpdateSolutionAsync(Solution solution, ImmutableArray<RenameDocumentAction> actions, CancellationToken cancellationToken)
            {
                if (solution is null)
                {
                    throw new ArgumentNullException(nameof(solution));
                }

                if (actions.Any(a => !ApplicableActions.Contains(a)))
                {
                    throw new ArgumentException(string.Format(WorkspacesResources.Cannot_apply_action_that_is_not_in_0, nameof(ApplicableActions)));
                }

                // Prior to updating the solution it's possible the document id has changed between the time 
                // the document action info was generated and when the solution update is applied. We
                // do a best effort to still locate the document if the id has changed.
                var document = GetDocument(solution);

                // If the document was found in the solution then the current id will be durable across actions
                // since we own the solution snapshot at this point. 
                var documentId = document.Id;

                // Make sure that the document name and folders are updated to what we expect them to be
                solution = solution
                    .WithDocumentName(documentId, _documentName)
                    .WithDocumentFolders(documentId, _documentFolders);

                // Apply each action individually. Order should not matter
                foreach (var action in actions)
                {
                    document = solution.GetRequiredDocument(documentId);
                    solution = await action.GetModifiedSolutionAsync(document, _options, cancellationToken).ConfigureAwait(false);
                }

                return solution;
            }

            /// <summary>
            /// Attempts to find the document in the solution. Tries by documentId first, but 
            /// that's not always reliable between analysis and application of the rename actions
            /// </summary>
            private Document GetDocument(Solution solution)
            {
                // DocumentId is the best bet for finding a document,
                // but it's possible the document was renamed or moved
                // before actions were applied.
                if (solution.ContainsDocument(_documentId))
                {
                    return solution.GetRequiredDocument(_documentId);
                }

                // There are cases where we expect work to be done between when the ActionSet is first generated
                // and when the solution can be worked on. This work can remove and add documents as part of the rename
                // and thus won't have the same DocumentId. 
                // 
                // 1. Right click solution explorer > rename
                // 2. Call Renamer.RenameDocument to generate dialog for user (near synchronous) 
                // 3. CPS changes file on disk
                // 4. CPS updates project file if necessary
                // 5. In dotnet project system, a new design time build is started
                // 6. Re-evaluates what files need to be passed to Roslyn. Tell Roslyn of file changed. This is a remove then add. (Asynchronous on project-system side)
                // 7. We update the workspace snapshot in the VS Workspace. Synchronous and controlled by project system.
                // 8. RenameDocumentActionSet should be applied to the current Workspace Solution
                // 
                // Since step 6 and 7 remove and add the document, step 8 can't depend on the DocumentId being available and the same document.
                // We are guaranateed that the project is the same and we know what the document name will be. 
                // https://github.com/dotnet/roslyn/issues/43729 tracks designing a more elagent system that can help alleviate
                // this issue. 
                var project = solution.GetRequiredProject(_documentId.ProjectId);
                return project.Documents.FirstOrDefault(d => d.Name == _documentName && d.Folders.SequenceEqual(_documentFolders))
                    ?? throw new InvalidOperationException(WorkspaceExtensionsResources.The_solution_does_not_contain_the_specified_document);
            }
        }
    }
}
