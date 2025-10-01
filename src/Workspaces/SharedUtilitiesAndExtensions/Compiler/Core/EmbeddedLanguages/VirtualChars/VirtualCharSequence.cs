// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

    /// <summary>
    /// Finds the virtual char in this sequence that contains the position.  Will return null if this position is not
    /// in the span of this sequence.
    /// </summary>
    public VirtualChar? Find(int tokenStart, int position)
        => _leafCharacters?.Find(tokenStart, position);

    public VirtualCharGreenSequence Skip(int count)
        => this.GetSubSequence(TextSpan.FromBounds(count, this.Length));

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

/// <inheritdoc cref="VirtualCharGreenSequence"/>
internal readonly struct VirtualCharSequence
{
    private static readonly ObjectPool<ImmutableSegmentedList<VirtualCharGreen>.Builder> s_builderPool
        = new(() => ImmutableSegmentedList.CreateBuilder<VirtualCharGreen>());

    private readonly int _tokenStart;
    private readonly VirtualCharGreenSequence _sequence;

    public static readonly VirtualCharSequence Empty = new(0, VirtualCharGreenSequence.Empty);

    public static VirtualCharSequence Create(int tokenStart, string text)
        => new(tokenStart, VirtualCharGreenSequence.Create(text));

    public static VirtualCharSequence Create(ImmutableSegmentedList<VirtualChar> virtualChars)
    {
        if (virtualChars.IsEmpty)
            return Empty;

        // Determine the earliest token start of all of the virtual chars.  This will be where this entire sequence
        // starts.  Any virtual chars with the same token start will keep their same offset.  Any virtual chars with a
        // later token start will have their offset adjusted to be relative to this new start.
        var minimumTokenStart = virtualChars.Min(static c => c.TokenStart);

        using var pooledObject = s_builderPool.GetPooledObject();
        var builder = pooledObject.Object;

        foreach (var ch in virtualChars)
        {
            Debug.Assert(ch.TokenStart >= minimumTokenStart);
            var offsetDifference = ch.TokenStart - minimumTokenStart;
            var newGreen = ch.Green.WithOffset(ch.Green.Offset + offsetDifference);
            builder.Add(newGreen);
        }

        var result = new VirtualCharSequence(minimumTokenStart, VirtualCharGreenSequence.Create(builder.ToImmutable()));
        builder.Clear();

        return result;
    }

    public VirtualCharSequence(int tokenStart, VirtualCharGreenSequence sequence)
    {
        if (tokenStart < 0)
            throw new ArgumentException("tokenStart cannot be negative", nameof(tokenStart));

        _tokenStart = tokenStart;
        _sequence = sequence;
    }

    /// <inheritdoc cref="VirtualCharGreenSequence.Length"/>
    public int Length => _sequence.Length;

    public VirtualChar this[int index]
        => new(_sequence[index], _tokenStart);

    /// <inheritdoc cref="VirtualCharGreenSequence.Find"/>
    public VirtualChar? Find(int position)
        => _sequence.Find(_tokenStart, position);

    public bool Contains(VirtualChar @char)
        => IndexOf(@char) >= 0;

    public int IndexOf(VirtualChar @char)
    {
        var index = 0;
        foreach (var ch in this)
        {
            if (ch == @char)
                return index;

            index++;
        }

        return -1;
    }

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

    /// <inheritdoc cref="VirtualCharGreenSequence.IsDefault"/>
    public bool IsDefault => _sequence.IsDefault;

    /// <inheritdoc cref="VirtualCharGreenSequence.IsEmpty"/>
    public bool IsEmpty => _sequence.IsEmpty;

    /// <inheritdoc cref="VirtualCharGreenSequence.IsDefaultOrEmpty"/>
    public bool IsDefaultOrEmpty => _sequence.IsDefaultOrEmpty;

    /// <inheritdoc cref="VirtualCharGreenSequence.GetSubSequence"/>
    public VirtualCharSequence GetSubSequence(TextSpan span)
       => new(_tokenStart, _sequence.GetSubSequence(span));

    /// <inheritdoc cref="VirtualCharGreenSequence.Skip"/>
    public VirtualCharSequence Skip(int count)
        => new(_tokenStart, _sequence.Skip(count));

    public Enumerator GetEnumerator()
        => new(this);

    public VirtualChar First() => this[0];
    public VirtualChar Last() => this[^1];

    public VirtualChar? FirstOrNull(Func<VirtualChar, bool> predicate)
    {
        foreach (var ch in this)
        {
            if (predicate(ch))
                return ch;
        }

        return null;
    }

    public VirtualChar? LastOrNull(Func<VirtualChar, bool> predicate)
    {
        for (var i = this.Length - 1; i >= 0; i--)
        {
            var ch = this[i];
            if (predicate(ch))
                return ch;
        }

        return null;
    }

    public bool Any(Func<VirtualChar, bool> predicate)
    {
        foreach (var ch in this)
        {
            if (predicate(ch))
                return true;
        }

        return false;
    }

    public bool All(Func<VirtualChar, bool> predicate)
    {
        foreach (var ch in this)
        {
            if (!predicate(ch))
                return false;
        }

        return true;
    }

    public VirtualCharSequence SkipWhile(Func<VirtualChar, bool> predicate)
    {
        var start = 0;
        foreach (var ch in this)
        {
            if (!predicate(ch))
                break;

            start++;
        }

        return this.GetSubSequence(TextSpan.FromBounds(start, this.Length));
    }

    [Conditional("DEBUG")]
    public void AssertAdjacentTo(VirtualCharSequence virtualChars)
    {
        _sequence.AssertAdjacentTo(virtualChars._sequence);
    }

    /// <summary>
    /// Combines two <see cref="VirtualCharSequence"/>s, producing a final sequence that points at the same underlying
    /// data, but spans from the start of <paramref name="chars1"/> to the end of <paramref name="chars2"/>.
    /// </summary>  
    public static VirtualCharSequence FromBounds(
        VirtualCharSequence chars1, VirtualCharSequence chars2)
    {
        Debug.Assert(chars1._tokenStart == chars2._tokenStart);
        return new VirtualCharSequence(
            chars1._tokenStart,
            VirtualCharGreenSequence.FromBounds(chars1._sequence, chars2._sequence));
    }

    public struct Enumerator(VirtualCharSequence virtualCharSequence) : IEnumerator<VirtualChar>
    {
        private int _position = -1;

        public bool MoveNext() => ++_position < virtualCharSequence.Length;
        public readonly VirtualChar Current => virtualCharSequence[_position];

        public void Reset()
            => _position = -1;

        readonly object? IEnumerator.Current => this.Current;
        public readonly void Dispose() { }
    }
}
