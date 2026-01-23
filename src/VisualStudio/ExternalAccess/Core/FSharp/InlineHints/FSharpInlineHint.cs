// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.InlineHints;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.InlineHints;
#endif

/// <inheritdoc cref="InlineHint"/>
internal readonly struct FSharpInlineHint
{
    public readonly TextSpan Span;
    public readonly ImmutableArray<TaggedText> DisplayParts;
    private readonly Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>>? _getDescriptionAsync;

    public FSharpInlineHint(
        TextSpan span,
        ImmutableArray<TaggedText> displayParts,
        Func<Document, CancellationToken, Task<ImmutableArray<TaggedText>>>? getDescriptionAsync = null)
    {
        if (displayParts.Length == 0)
            throw new ArgumentException($"{nameof(displayParts)} must be non-empty");

        Span = span;
        DisplayParts = displayParts;
        _getDescriptionAsync = getDescriptionAsync;
    }

    /// <summary>
    /// Gets a description for the inline hint, suitable to show when a user hovers over the editor adornment.  The
    /// <paramref name="document"/> will represent the file at the time this hint was created.
    /// </summary>
    public Task<ImmutableArray<TaggedText>> GetDescriptionAsync(Document document, CancellationToken cancellationToken)
        => _getDescriptionAsync?.Invoke(document, cancellationToken) ?? SpecializedTasks.EmptyImmutableArray<TaggedText>();
}
