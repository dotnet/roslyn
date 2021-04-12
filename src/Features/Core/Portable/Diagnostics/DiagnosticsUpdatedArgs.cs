﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class DiagnosticsUpdatedArgs : UpdatedEventArgs
    {
        public DiagnosticsUpdatedKind Kind { get; }
        public Solution? Solution { get; }

        private readonly ImmutableArray<DiagnosticData> _diagnostics;

        private DiagnosticsUpdatedArgs(
            object id,
            Workspace workspace,
            Solution? solution,
            ProjectId? projectId,
            DocumentId? documentId,
            ImmutableArray<DiagnosticData> diagnostics,
            DiagnosticsUpdatedKind kind)
            : base(id, workspace, projectId, documentId)
        {
            // TODO: This assert fails for EditAndContinueDiagnosticUpdateSource. See https://github.com/dotnet/roslyn/issues/36246.
            // Debug.Assert(diagnostics.All(d => d.ProjectId == projectId && d.DocumentId == documentId));

            Debug.Assert(kind != DiagnosticsUpdatedKind.DiagnosticsRemoved || diagnostics.IsEmpty);

            Solution = solution;
            Kind = kind;
            _diagnostics = diagnostics;
        }

        /// <summary>
        /// Gets all the diagnostics for this event, regardless if this is for pull or push diagnostics.  Most clients
        /// should not use this.  The only clients that should are ones that are aggregating the values transparently
        /// and then forwarding on later on to other clients that will make this decision.
        /// </summary>
        /// <returns></returns>
        public ImmutableArray<DiagnosticData> GetAllDiagnosticsRegardlessOfPushPullSetting()
            => _diagnostics;

        /// <summary>
        /// Gets all the diagnostics for this event, respecting the callers setting on if they're getting it for pull
        /// diagnostics or push diagnostics.  Most clients should use this to ensure they see the proper set of
        /// diagnostics in their scenario (or an empty array if not in their scenario).
        /// </summary>
        public ImmutableArray<DiagnosticData> GetPullDiagnostics(
            Workspace workspace, Option2<DiagnosticMode> diagnosticMode)
        {
            // If push diagnostics are on, they get nothing since they're asking for pull diagnostics.
            if (workspace.IsPushDiagnostics(diagnosticMode))
                return ImmutableArray<DiagnosticData>.Empty;

            return _diagnostics;
        }

        /// <summary>
        /// Gets all the diagnostics for this event, respecting the callers setting on if they're getting it for pull
        /// diagnostics or push diagnostics.  Most clients should use this to ensure they see the proper set of
        /// diagnostics in their scenario (or an empty array if not in their scenario).
        /// </summary>
        public ImmutableArray<DiagnosticData> GetPushDiagnostics(
            Workspace workspace, Option2<DiagnosticMode> diagnosticMode)
        {
            // If pull diagnostics are on, they get nothing since they're asking for push diagnostics.
            if (workspace.IsPullDiagnostics(diagnosticMode))
                return ImmutableArray<DiagnosticData>.Empty;

            return _diagnostics;
        }

        public static DiagnosticsUpdatedArgs DiagnosticsCreated(
            object id,
            Workspace workspace,
            Solution? solution,
            ProjectId? projectId,
            DocumentId? documentId,
            ImmutableArray<DiagnosticData> diagnostics)
        {
            return new DiagnosticsUpdatedArgs(id, workspace, solution, projectId, documentId, diagnostics, DiagnosticsUpdatedKind.DiagnosticsCreated);
        }

        public static DiagnosticsUpdatedArgs DiagnosticsRemoved(
            object id,
            Workspace workspace,
            Solution? solution,
            ProjectId? projectId,
            DocumentId? documentId)
        {
            return new DiagnosticsUpdatedArgs(id, workspace, solution, projectId, documentId, ImmutableArray<DiagnosticData>.Empty, DiagnosticsUpdatedKind.DiagnosticsRemoved);
        }
    }
}
