// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor.Completion.Delegation;

internal interface IDelegatedCSharpCompletionResponseRewriter
{
    RazorVSInternalCompletionList Rewrite(
        RazorVSInternalCompletionList completionList,
        RazorCodeDocument codeDocument,
        int hostDocumentIndex,
        Position projectedPosition,
        RazorCompletionOptions completionOptions);
}
