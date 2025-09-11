// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Fix all code action for a code action registered by a <see cref="CodeRefactoringProvider"/>.
/// </summary>
internal sealed class RefactorAllCodeAction(RefactorAllState fixAllState, bool showPreviewChangesDialog = true)
    : AbstractFixAllCodeAction(fixAllState, showPreviewChangesDialog)
{
    protected override IRefactorOrFixAllContext CreateFixAllContext(IRefactorOrFixAllState fixAllState, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
        => new RefactorAllContext((RefactorAllState)fixAllState, progressTracker, cancellationToken);

    protected override bool IsInternalProvider(IRefactorOrFixAllState fixAllState)
        => true; // FixAll for refactorings is currently only supported for internal code refactoring providers.
}
