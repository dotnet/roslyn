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
        throw new System.NotImplementedException();
    }

    public bool TryResolveInsertString(VSInternalCompletionItem completionItem, [NotNullWhen(true)] out string? insertString)
    {
        throw new System.NotImplementedException();
    }
}
