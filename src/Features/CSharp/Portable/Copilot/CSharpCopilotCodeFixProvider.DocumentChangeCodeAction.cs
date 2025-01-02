// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

internal sealed partial class CSharpCopilotCodeFixProvider
{
    /// <summary>
    /// The CodeAction that represents the change that will be made to the document per the Copilot suggestion.
    /// It also contains a special <see cref="CopilotDismissChangesCodeAction"/> that is reported as part
    /// of <see cref="AdditionalPreviewFlavors"/> so that the lightbulb preview for this Copilot suggestion
    /// shows a 'Dismiss' hyperlink for dismissing bad Copilot suggestions.
    /// </summary>
    private sealed class CopilotDocumentChangeCodeAction(
        string title,
        Func<IProgress<CodeAnalysisProgress>, CancellationToken, Task<Document>> createChangedDocument,
        string? equivalenceKey,
        CopilotDismissChangesCodeAction dismissChangesCodeAction,
        CodeActionPriority priority) : CodeAction.DocumentChangeAction(title, createChangedDocument, equivalenceKey, priority)
    {
        internal sealed override ImmutableArray<CodeAction> AdditionalPreviewFlavors { get; } = [dismissChangesCodeAction];
    }
}
