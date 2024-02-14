// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CSharp.Copilot
{
    internal sealed partial class CSharpCopilotCodeFixProvider
    {
        private sealed class CopilotDocumentChangeCodeAction : CodeAction.DocumentChangeAction
        {
            internal sealed override ImmutableArray<CodeAction> AdditionalPreviewFlavors { get; }

            public CopilotDocumentChangeCodeAction(
                string title,
                Func<IProgress<CodeAnalysisProgress>, CancellationToken, Task<Document>> createChangedDocument,
                string? equivalenceKey,
                CopilotDismissChangesCodeAction dismissChangesCodeAction,
                CodeActionPriority priority = CodeActionPriority.Default)
                : base(title, createChangedDocument, equivalenceKey, priority)
            {
                AdditionalPreviewFlavors = [dismissChangesCodeAction];
            }
        }
    }
}
