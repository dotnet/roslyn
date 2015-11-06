// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal interface IFixMultipleOccurrencesService : IWorkspaceService
    {
        /// <summary>
        /// Get the fix multiple occurrences code fix for the given diagnostics with source locations.
        /// NOTE: This method does not apply the fix to the workspace.
        /// </summary>
        Solution GetFix(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsToFix,
            Workspace workspace,
            CodeFixProvider fixProvider,
            FixAllProvider fixAllProvider,
            string equivalenceKey,
            string waitDialogTitle,
            string waitDialogMessage,
            CancellationToken cancellationToken);

        /// <summary>
        /// Computes the fix multiple occurrences code fix for the given diagnostics without any source location, brings up the preview changes dialog for the fix and
        /// apply the code action operations corresponding to the fix.
        /// </summary>
        void ComputeAndApplyFix(
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
            Workspace workspace,
            CodeFixProvider fixProvider,
            FixAllProvider fixAllProvider,
            string equivalenceKey,
            string waitDialogAndPreviewChangesTitle,
            string waitDialogMessage,
            bool showPreviewChangesDialog,
            CancellationToken cancellationToken);

        /// <summary>
        /// Get the fix multiple occurrences code fix for the given diagnostics with source locations.
        /// NOTE: This method does not apply the fix to the workspace.
        /// </summary>
        Solution GetFix(
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsToFix,
            Workspace workspace,
            CodeFixProvider fixProvider,
            FixAllProvider fixAllProvider,
            string equivalenceKey,
            string waitDialogTitle,
            string waitDialogMessage,
            CancellationToken cancellationToken);
    }
}
