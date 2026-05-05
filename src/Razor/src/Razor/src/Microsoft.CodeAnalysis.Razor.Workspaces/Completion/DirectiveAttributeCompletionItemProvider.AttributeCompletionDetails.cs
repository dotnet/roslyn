// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Razor.Tooltip;

namespace Microsoft.CodeAnalysis.Razor.Completion;

internal partial class DirectiveAttributeCompletionItemProvider
{
    private readonly struct AttributeCompletionDetails(
        RazorCompletionItemKind kind,
        ImmutableArray<BoundAttributeDescriptionInfo> descriptions = default,
        ImmutableArray<RazorCommitCharacter> commitCharacters = default)
    {
        public RazorCompletionItemKind Kind => kind;

        public ImmutableArray<BoundAttributeDescriptionInfo> Descriptions => descriptions.NullToEmpty();
        public ImmutableArray<RazorCommitCharacter> CommitCharacters => commitCharacters.NullToEmpty();

        public void Deconstruct(
            out RazorCompletionItemKind kind,
            out ImmutableArray<BoundAttributeDescriptionInfo> descriptions,
            out ImmutableArray<RazorCommitCharacter> commitCharacters)
            => (kind, descriptions, commitCharacters) = (Kind, Descriptions, CommitCharacters);

        public void Deconstruct(
            out ImmutableArray<BoundAttributeDescriptionInfo> descriptions,
            out ImmutableArray<RazorCommitCharacter> commitCharacters)
            => (descriptions, commitCharacters) = (Descriptions, CommitCharacters);
    }
}
