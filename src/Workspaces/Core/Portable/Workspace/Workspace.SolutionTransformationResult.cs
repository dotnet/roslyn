// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial class Workspace
    {
        internal readonly struct SolutionTransformationResult
        {
            /// <summary>
            /// The transformed solution.
            /// </summary>
            public readonly Solution Solution;

            /// <summary>
            /// The kind of workspace change event to raise.
            /// </summary>
            public readonly WorkspaceChangeKind EventChangeKind;

            /// <summary>
            /// The id of the project updated by the transformation to be passed to the workspace change event.
            /// </summary>
            public readonly ProjectId? UpdatedProjectId;

            /// <summary>
            /// The id of the document updated by transformation to be passed to the workspace change event.
            /// </summary>
            public readonly OneOrMany<DocumentId>? UpdatedDocumentIds;

            /// <summary>
            /// Document event to raise. <see cref="DocumentClosedEventName"/> or <see cref="DocumentOpenedEventName"/>.
            /// </summary>
            public readonly string? DocumentEventName;

            internal SolutionTransformationResult(
                Solution solution,
                WorkspaceChangeKind eventChangeKind,
                ProjectId? updatedProjectId = null,
                OneOrMany<DocumentId>? updatedDocumentIds = null,
                string? documentEventName = null)
            {
                Contract.ThrowIfFalse(!updatedDocumentIds.HasValue || updatedProjectId == null);
                Contract.ThrowIfFalse(documentEventName == null || updatedDocumentIds.HasValue);

                switch (eventChangeKind)
                {
                    case WorkspaceChangeKind.SolutionChanged:
                    case WorkspaceChangeKind.SolutionAdded:
                    case WorkspaceChangeKind.SolutionRemoved:
                    case WorkspaceChangeKind.SolutionCleared:
                    case WorkspaceChangeKind.SolutionReloaded:
                        Contract.ThrowIfFalse(updatedProjectId == null);
                        Contract.ThrowIfFalse(!updatedDocumentIds.HasValue);
                        break;

                    case WorkspaceChangeKind.ProjectAdded:
                    case WorkspaceChangeKind.ProjectRemoved:
                    case WorkspaceChangeKind.ProjectChanged:
                    case WorkspaceChangeKind.ProjectReloaded:
                        Contract.ThrowIfNull(updatedProjectId);
                        break;

                    case WorkspaceChangeKind.DocumentAdded:
                    case WorkspaceChangeKind.DocumentRemoved:
                    case WorkspaceChangeKind.DocumentReloaded:
                    case WorkspaceChangeKind.DocumentChanged:
                    case WorkspaceChangeKind.AdditionalDocumentAdded:
                    case WorkspaceChangeKind.AdditionalDocumentRemoved:
                    case WorkspaceChangeKind.AdditionalDocumentReloaded:
                    case WorkspaceChangeKind.AdditionalDocumentChanged:
                    case WorkspaceChangeKind.DocumentInfoChanged:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentAdded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentRemoved:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentReloaded:
                    case WorkspaceChangeKind.AnalyzerConfigDocumentChanged:
                        Contract.ThrowIfFalse(updatedDocumentIds.HasValue);
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(eventChangeKind);
                }

                Solution = solution;
                EventChangeKind = eventChangeKind;
                UpdatedProjectId = updatedProjectId;
                UpdatedDocumentIds = updatedDocumentIds;
                DocumentEventName = documentEventName;
            }
        }

        internal static SolutionTransformationResult TransformationResult(Solution solution, WorkspaceChangeKind eventChangeKind)
            => new SolutionTransformationResult(solution, eventChangeKind);

        internal static SolutionTransformationResult TransformationResult(Solution solution, WorkspaceChangeKind eventChangeKind, ProjectId updatedProjectId)
            => new SolutionTransformationResult(solution, eventChangeKind, updatedProjectId);

        internal static SolutionTransformationResult TransformationResult(Solution solution, WorkspaceChangeKind eventChangeKind, DocumentId updatedDocumentId, string? documentEventName = null)
            => new SolutionTransformationResult(solution, eventChangeKind, updatedDocumentIds: OneOrMany.Create(updatedDocumentId), documentEventName: documentEventName);

        internal static SolutionTransformationResult TransformationResult(Solution solution, WorkspaceChangeKind eventChangeKind, ImmutableArray<DocumentId> updatedDocumentIds)
            => new SolutionTransformationResult(solution, eventChangeKind, updatedDocumentIds: OneOrMany.Create(updatedDocumentIds));
    }
}
