// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.LegacySolutionEvents
{
    [DataContract]
    internal sealed class SerializableWorkspaceChangeEventArgs
    {
        /// <inheritdoc cref="WorkspaceChangeEventArgs.Kind"/>
        [DataMember(Order = 0)]
        public WorkspaceChangeKind Kind { get; }

        /// <inheritdoc cref="WorkspaceChangeEventArgs.OldSolution"/>
        [DataMember(Order = 1)]
        public Checksum OldSolutionChecksum { get; }

        /// <inheritdoc cref="WorkspaceChangeEventArgs.NewSolution"/>
        [DataMember(Order = 2)]
        public Checksum NewSolutionChecksum { get; }

        /// <inheritdoc cref="WorkspaceChangeEventArgs.ProjectId"/>
        [DataMember(Order = 3)]
        public ProjectId? ProjectId { get; }

        /// <inheritdoc cref="WorkspaceChangeEventArgs.DocumentId"/>
        [DataMember(Order = 4)]
        public DocumentId? DocumentId { get; }

        public SerializableWorkspaceChangeEventArgs(
            WorkspaceChangeKind kind,
            Checksum oldSolutionChecksum,
            Checksum newSolutionChecksum,
            ProjectId? projectId,
            DocumentId? documentId)
        {
            Kind = kind;
            OldSolutionChecksum = oldSolutionChecksum;
            NewSolutionChecksum = newSolutionChecksum;
            ProjectId = projectId;
            DocumentId = documentId;
        }
    }

    /// <summary>
    /// This is a legacy api intended only for existing SolutionCrawler partners to continue to function (albeit with
    /// ownership of that crawling task now belonging to the partner team, not roslyn).  It should not be used for any
    /// new services.
    /// </summary>
    internal interface IRemoteLegacySolutionEventsAggregationService
    {
        ValueTask OnWorkspaceChangedEventAsync(SerializableWorkspaceChangeEventArgs args, CancellationToken cancellationToken);
        ValueTask OnTextDocumentOpenedAsync(Checksum solutionChecksum, DocumentId documentId, CancellationToken cancellationToken);
        ValueTask OnTextDocumentClosedAsync(Checksum solutionChecksum, DocumentId documentId, CancellationToken cancellationToken);
    }
}
