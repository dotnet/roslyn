// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// A <see cref="CodeActionOperation"/> for applying solution changes to a workspace.
    /// <see cref="CodeAction.GetOperationsAsync(CancellationToken)"/> may return at most one
    /// <see cref="ApplyChangesOperation"/>. Hosts may provide custom handling for 
    /// <see cref="ApplyChangesOperation"/>s, but if a <see cref="CodeAction"/> requires custom
    /// host behavior not supported by a single <see cref="ApplyChangesOperation"/>, then instead:
    /// <list type="bullet">
    /// <description><text>Implement a custom <see cref="CodeAction"/> and <see cref="CodeActionOperation"/>s</text></description>
    /// <description><text>Do not return any <see cref="ApplyChangesOperation"/> from <see cref="CodeAction.GetOperationsAsync(CancellationToken)"/></text></description>
    /// <description><text>Directly apply any workspace edits</text></description>
    /// <description><text>Handle any custom host behavior</text></description>
    /// <description><text>Produce a preview for <see cref="CodeAction.GetPreviewOperationsAsync(CancellationToken)"/> 
    ///   by creating a custom <see cref="PreviewOperation"/> or returning a single <see cref="ApplyChangesOperation"/>
    ///   to use the built-in preview mechanism</text></description>
    /// </list>
    /// </summary>
    public sealed class ApplyChangesOperation : CodeActionOperation
    {
        public Solution ChangedSolution { get; }

        public ApplyChangesOperation(Solution changedSolution)
        {
            ChangedSolution = changedSolution ?? throw new ArgumentNullException(nameof(changedSolution));
        }

        internal override bool ApplyDuringTests => true;

        public override void Apply(Workspace workspace, CancellationToken cancellationToken)
        {
            this.TryApply(workspace, new ProgressTracker(), cancellationToken);
        }

        internal override bool TryApply(
            Workspace workspace, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            return workspace.TryApplyChanges(ChangedSolution, progressTracker);
        }
    }
}
