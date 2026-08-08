// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Debugging;

/// <summary>
/// Result of splicing a debugger expression into a source document.
/// </summary>
internal readonly struct DebuggerSpliceResult(
    SourceText text,
    int completionPosition,
    int spliceStart,
    int insertedLength)
{
    /// <summary>
    /// The spliced source text with the expression inserted.
    /// </summary>
    public SourceText Text { get; } = text;

    /// <summary>
    /// The absolute position in the spliced text where completion should be invoked.
    /// </summary>
    public int CompletionPosition { get; } = completionPosition;

    /// <summary>
    /// The position in the original document where the splice was inserted.
    /// Used to compute position adjustments for resolve operations.
    /// </summary>
    public int SpliceStart { get; } = spliceStart;

    /// <summary>
    /// The length of text inserted during splicing (separator + expression + terminator).
    /// Used to compute position adjustments for resolve operations.
    /// </summary>
    public int InsertedLength { get; } = insertedLength;
}

