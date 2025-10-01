// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

/// <summary>
/// Represents the individual characters that raw string token represents (i.e. with escapes collapsed).  
/// The difference between this and the result from token.ValueText is that for each collapsed character
/// returned the original span of text in the original token can be found.  i.e. if you had the
/// following in C#:
/// <para/>
/// <c>"G\u006fo"</c>
/// <para/>
/// Then you'd get back:
/// <para/>
/// <c>'G' -> [0, 1) 'o' -> [1, 7) 'o' -> [7, 1)</c>
/// <para/>
/// This allows for embedded language processing that can refer back to the user's original code
/// instead of the escaped value we're processing.
/// </summary>
internal partial struct VirtualCharGreenSequence
{
    public static readonly VirtualCharGreenSequence Empty = Create(ImmutableSegmentedList<VirtualCharGreen>.Empty);

    public static VirtualCharGreenSequence Create(ImmutableSegmentedList<VirtualCharGreen> virtualChars)
        => new(new ImmutableSegmentedListChunk(virtualChars));

    public static VirtualCharGreenSequence Create(string underlyingData)
        => new(new StringChunk(underlyingData));

    /// <summary>
    /// The actual characters that this <see cref="VirtualCharSequence"/> is a portion of.
    /// </summary>
    private readonly Chunk _leafCharacters;

    /// <summary>
    /// The portion of <see cref="_leafCharacters"/> that is being exposed.  This span 
    /// is `[inclusive, exclusive)`.
    /// </summary>
    private readonly TextSpan _span;

    private VirtualCharGreenSequence(Chunk sequence)
        : this(sequence, new TextSpan(0, sequence.Length))
    {
    }

    private VirtualCharGreenSequence(Chunk sequence, TextSpan span)
    {
        if (span.Start > sequence.Length)
            throw new ArgumentException();

        if (span.End > sequence.Length)
            throw new ArgumentException();

        _leafCharacters = sequence;
        _span = span;
    }

    /// <summary>
    /// Gets the number of elements contained in the <see cref="VirtualCharSequence"/>.
    /// </summary>
    public int Length => _span.Length;

    /// <summary>
    /// Gets the <see cref="VirtualChar"/> at the specified index.
    /// </summary>
    public VirtualCharGreen this[int index] => _leafCharacters[_span.Start + index];

    /// <summary>
    /// Gets a value indicating whether the <see cref="VirtualCharSequence"/> was declared but not initialized.
    /// </summary>
    public bool IsDefault => _leafCharacters == null;
    public bool IsEmpty => Length == 0;
    public bool IsDefaultOrEmpty => IsDefault || IsEmpty;

    /// <summary>
    /// Retreives a sub-sequence from this <see cref="VirtualCharSequence"/>.
    /// </summary>
    public VirtualCharGreenSequence GetSubSequence(TextSpan span)
       => new(_leafCharacters, new TextSpan(_span.Start + span.Start, span.Length));

    public Enumerator GetEnumerator()
        => new(this);

    public VirtualCharGreen First() => this[0];
    public VirtualCharGreen Last() => this[^1];

    /// <summary>
    /// Finds the virtual char in this sequence that contains the position.  Will return null if this position is not
    /// in the span of this sequence.
    /// </summary>
    public VirtualChar? Find(int tokenStart, int position)
        => _leafCharacters?.Find(tokenStart, position);

    public VirtualCharGreenSequence Skip(int count)
        => this.GetSubSequence(TextSpan.FromBounds(count, this.Length));

    /// <summary>
    /// Create a <see cref="string"/> from the <see cref="VirtualCharSequence"/>.
    /// </summary>
    public string CreateString()
    {
        using var _ = PooledStringBuilder.GetInstance(out var builder);
        foreach (var ch in this)
            ch.AppendTo(builder);

        return builder.ToString();
    }

    [Conditional("DEBUG")]
    public void AssertAdjacentTo(VirtualCharGreenSequence virtualChars)
    {
        Debug.Assert(_leafCharacters == virtualChars._leafCharacters);
        Debug.Assert(_span.End == virtualChars._span.Start);
    }

    /// <summary>
    /// Combines two <see cref="VirtualCharGreenSequence"/>s, producing a final sequence that points at the same
    /// underlying data, but spans from the start of <paramref name="chars1"/> to the end of <paramref name="chars2"/>.
    /// </summary>  
    public static VirtualCharGreenSequence FromBounds(
        VirtualCharGreenSequence chars1, VirtualCharGreenSequence chars2)
    {
        Debug.Assert(chars1._leafCharacters == chars2._leafCharacters);
        return new VirtualCharGreenSequence(
            chars1._leafCharacters,
            TextSpan.FromBounds(chars1._span.Start, chars2._span.End));
    }
}
