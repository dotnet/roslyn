// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.TextDifferencing;

internal enum DiffKind : byte
{
    /// <summary>
    /// Diff by character.
    /// </summary>
    Char,
    /// <summary>
    /// Diff by line
    /// </summary>
    Line,
    /// <summary>
    /// Diff by word
    /// </summary>
    /// <remarks>
    /// Word break characters are: whitespace, '/' and '"'. Contiguous word breaks are treated as a single word.
    /// </remarks>
    Word,
}
