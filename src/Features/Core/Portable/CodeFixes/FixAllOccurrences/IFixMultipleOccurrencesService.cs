﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
