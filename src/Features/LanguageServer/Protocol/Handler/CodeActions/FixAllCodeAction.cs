// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CodeActions;

internal sealed class FixAllCodeAction : AbstractFixAllCodeAction
{
    private readonly string _title;

    public FixAllCodeAction(string title, IFixAllState fixAllState, bool showPreviewChangesDialog) : base(fixAllState, showPreviewChangesDialog)
    {
        _title = title;
    }

    public override string Title
        => _title;

    protected override IFixAllContext CreateFixAllContext(IFixAllState fixAllState, IProgressTracker progressTracker, CancellationToken cancellationToken)
        => new FixAllContext((FixAllState)fixAllState, progressTracker, cancellationToken);

    protected override bool IsInternalProvider(IFixAllState fixAllState)
    {
        return true;
    }
}
