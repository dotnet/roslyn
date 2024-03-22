// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal partial class FixMultipleCodeAction(
    IFixAllState fixAllState,
    string title,
    string computingFixWaitDialogMessage) : AbstractFixAllCodeFixCodeAction(fixAllState, showPreviewChangesDialog: false)
{
    private readonly string _title = title;
    private readonly string _computingFixWaitDialogMessage = computingFixWaitDialogMessage;

    public override string Title => _title;

    internal override string Message => _computingFixWaitDialogMessage;
}
