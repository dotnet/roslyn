// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Fix all code action for a code action registered by a <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    internal sealed class FixAllCodeRefactoringCodeAction(IFixAllState fixAllState) : AbstractFixAllCodeAction(fixAllState, showPreviewChangesDialog: true)
    {
        protected override IFixAllContext CreateFixAllContext(IFixAllState fixAllState, IProgressTracker progressTracker, CancellationToken cancellationToken)
            => new FixAllContext((FixAllState)fixAllState, progressTracker, cancellationToken);

        protected override bool IsInternalProvider(IFixAllState fixAllState)
            => true; // FixAll for refactorings is currently only supported for internal code refactoring providers.
    }
}
