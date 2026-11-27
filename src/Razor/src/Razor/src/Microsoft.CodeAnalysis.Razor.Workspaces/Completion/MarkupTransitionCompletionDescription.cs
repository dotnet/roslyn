// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal sealed class MarkupTransitionCompletionDescription(string description) : CompletionDescription
{
    public override string Description { get; } = description;
}
