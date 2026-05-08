// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal sealed class ThrowingSnippetCompletionItemResolveProvider : ISnippetCompletionItemProvider
{
    public void AddSnippetCompletions(ref PooledArrayBuilder<VSInternalCompletionItem> builder, RazorLanguageKind projectedKind, VSInternalCompletionInvokeKind invokeKind, string? triggerCharacter)
    {
        // No-op: this test provider intentionally adds no snippets.
        // Resolve is the only operation that should throw.
    }

    public bool TryResolveInsertString(VSInternalCompletionItem completionItem, [NotNullWhen(true)] out string? insertString)
    {
        throw new System.NotImplementedException();
    }
}
