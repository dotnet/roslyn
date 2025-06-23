// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

namespace Microsoft.CodeAnalysis.CodeFixes;

internal sealed partial class FixMultipleCodeAction(
    IFixAllState fixAllState,
    string title,
    string computingFixWaitDialogMessage) : AbstractFixAllCodeFixCodeAction(fixAllState, showPreviewChangesDialog: false)
{
    public override string Title => title;

    internal override string Message => computingFixWaitDialogMessage;
}
