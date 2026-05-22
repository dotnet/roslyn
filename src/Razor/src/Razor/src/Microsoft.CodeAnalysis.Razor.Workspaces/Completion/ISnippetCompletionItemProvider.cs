// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal interface ISnippetCompletionItemProvider
{
    void AddSnippetCompletions(ref PooledArrayBuilder<VSInternalCompletionItem> builder, RazorLanguageKind projectedKind, string? triggerCharacter, ICollection<string> validElementNames, RazorCompletionOptions options, bool isStartTagContext);
    bool TryResolveInsertString(VSInternalCompletionItem completionItem, [NotNullWhen(true)] out string? insertString);
}
