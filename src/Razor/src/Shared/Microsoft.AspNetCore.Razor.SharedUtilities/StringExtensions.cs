// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor;

namespace System;

internal static class StringExtensions
{
    /// <summary>
    ///  Indicates whether the specified string is <see langword="null"/> or an empty string ("").
    /// </summary>
    /// <param name="value">
    ///  The string to test.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if the <paramref name="value"/> parameter is <see langword="null"/>
    ///  or an empty string (""); otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  This extension method is useful on .NET Framework and .NET Standard 2.0 where
    ///  <see cref="string.IsNullOrEmpty(string?)"/> is not annotated for nullability.
    /// </remarks>
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
        => string.IsNullOrEmpty(value);

    /// <summary>
    ///  Indicates whether a specified string is <see langword="null"/>, empty, or consists only
    ///  of white-space characters.
    /// </summary>
    /// <param name="value">
    ///  The string to test.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if the <paramref name="value"/> parameter is <see langword="null"/>
    ///  or <see cref="string.Empty"/>, or if <paramref name="value"/> consists exclusively of
    ///  white-space characters.
    /// </returns>
    /// <remarks>
    ///  This extension method is useful on .NET Framework and .NET Standard 2.0 where
    ///  <see cref="string.IsNullOrWhiteSpace(string?)"/> is not annotated for nullability.
    /// </remarks>
    public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
        => string.IsNullOrWhiteSpace(value);

    /// <summary>
    ///  Creates a new <see cref="ReadOnlySpan{T}"/> over a portion of the target string from
    ///  a specified position to the end of the string.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="startIndex">
    ///  The index at which to begin this slice.
    /// </param>
    /// <remarks>
    ///  This uses Razor's <see cref="Index"/> type, which is type-forwarded on .NET.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="startIndex"/> is less than 0 or greater than <paramref name="text"/>.Length.
    /// </exception>
    public static ReadOnlySpan<char> AsSpan(this string? text, Index startIndex)
    {
#if NET
        return MemoryExtensions.AsSpan(text, startIndex);
#else
        if (text is null)
        {
            if (!startIndex.Equals(Index.Start))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(startIndex));
            }

            return default;
        }

        return text.AsSpan(startIndex.GetOffset(text.Length));
#endif
    }

    /// <summary>
    ///  Creates a new <see cref="ReadOnlySpan{T}"/> over a portion of a target string using
    ///  the range start and end indexes.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="range">
    ///  The range that has start and end indexes to use for slicing the string.
    /// </param>
    /// <remarks>
    ///  This uses Razor's <see cref="Range"/> type, which is type-forwarded on .NET.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///  <paramref name="range"/>'s start or end index is not within the bounds of the string.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start index is greater than its end index.
    /// </exception>
    public static ReadOnlySpan<char> AsSpan(this string? text, Range range)
    {
#if NET
        return MemoryExtensions.AsSpan(text, range);
#else
        if (text is null)
        {
            if (!range.Start.Equals(Index.Start) || !range.End.Equals(Index.Start))
            {
                ThrowHelper.ThrowArgumentNullException(nameof(text));
            }

            return default;
        }

        var (start, length) = range.GetOffsetAndLength(text.Length);
        return text.AsSpan(start, length);
#endif
    }

    /// <summary>
    ///  Creates a new <see cref="ReadOnlySpan{T}"/> over a string. If the target string
    ///  is <see langword="null"/> a <see langword="default"/>(<see cref="ReadOnlySpan{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    public static ReadOnlySpan<char> AsSpanOrDefault(this string? text)
        => text is not null ? text.AsSpan() : default;

    /// <summary>
    ///  Creates a new <see cref="ReadOnlySpan{T}"/> over a portion of the target string from
    ///  a specified position to the end of the string. If the target string is <see langword="null"/>
    ///  a <see langword="default"/>(<see cref="ReadOnlySpan{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="start">
    ///  The index at which to begin this slice.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="start"/> is less than 0 or greater than <paramref name="text"/>.Length.
    /// </exception>
    public static ReadOnlySpan<char> AsSpanOrDefault(this string? text, int start)
        => text is not null ? text.AsSpan(start) : default;

    /// <summary>
    ///  Creates a new <see cref="ReadOnlySpan{T}"/> over a portion of the target string from
    ///  a specified position for a specified number of characters. If the target string is
    ///  <see langword="null"/> a <see langword="default"/>(<see cref="ReadOnlySpan{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="start">
    ///  The index at which to begin this slice.
    /// </param>
    /// <param name="length">
    ///  The desired length for the slice.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="start"/>, <paramref name="length"/>, or <paramref name="start"/> + <paramref name="length"/>
    ///  is not in the range of <paramref name="text"/>.
    /// </exception>
    public static ReadOnlySpan<char> AsSpanOrDefault(this string? text, int start, int length)
        => text is not null ? text.AsSpan(start, length) : default;

    /// <summary>
    ///  Creates a new <see cref="ReadOnlySpan{T}"/> over a portion of the target string from
    ///  a specified position to the end of the string. If the target string is <see langword="null"/>
    ///  a <see langword="default"/>(<see cref="ReadOnlySpan{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="startIndex">
    ///  The index at which to begin this slice.
    /// </param>
    public static ReadOnlySpan<char> AsSpanOrDefault(this string? text, Index startIndex)
    {
        if (text is null)
        {
            return default;
        }

#if NET
        return MemoryExtensions.AsSpan(text, startIndex);
#else
        return text.AsSpan(startIndex.GetOffset(text.Length));
#endif
    }

    /// <summary>
    ///  Creates a new <see cref="ReadOnlySpan{T}"/> over a portion of the target string using the range
    ///  start and end indexes. If the target string is <see langword="null"/> a
    ///  <see langword="default"/>(<see cref="ReadOnlySpan{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="range">
    ///  The range that has start and end indexes to use for slicing the string.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start or end index is not within the bounds of the string.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start index is greater than its end index.
    /// </exception>
    public static ReadOnlySpan<char> AsSpanOrDefault(this string? text, Range range)
    {
        if (text is null)
        {
            return default;
        }

#if NET
        return MemoryExtensions.AsSpan(text, range);
#else
        var (start, length) = range.GetOffsetAndLength(text.Length);
        return text.AsSpan(start, length);
#endif
    }

    /// <summary>
    ///  Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of a target string starting at a specified index.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="startIndex">
    ///  The index at which to begin this slice.
    /// </param>
    /// <remarks>
    ///  This uses Razor's <see cref="Index"/> type, which is type-forwarded on .NET.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="startIndex"/> is less than 0 or greater than <paramref name="text"/>.Length.
    /// </exception>
    public static ReadOnlyMemory<char> AsMemory(this string? text, Index startIndex)
    {
#if NET
        return MemoryExtensions.AsMemory(text, startIndex);
#else
        if (text is null)
        {
            if (!startIndex.Equals(Index.Start))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(startIndex));
            }

            return default;
        }

        return text.AsMemory(startIndex.GetOffset(text.Length));
#endif
    }

    /// <summary>
    ///  Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of a target string using
    ///  the range start and end indexes.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="range">
    ///  The range that has start and end indexes to use for slicing the string.
    /// </param>
    /// <remarks>
    ///  This uses Razor's <see cref="Range"/> type, which is type-forwarded on .NET.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///  <paramref name="range"/>'s start or end index is not within the bounds of the string.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start index is greater than its end index.
    /// </exception>
    public static ReadOnlyMemory<char> AsMemory(this string? text, Range range)
    {
#if NET
        return MemoryExtensions.AsMemory(text, range);
#else
        if (text is null)
        {
            if (!range.Start.Equals(Index.Start) || !range.End.Equals(Index.Start))
            {
                ThrowHelper.ThrowArgumentNullException(nameof(text));
            }

            return default;
        }

        var (start, length) = range.GetOffsetAndLength(text.Length);
        return text.AsMemory(start, length);
#endif
    }

    /// <summary>
    ///  Creates a new <see cref="ReadOnlyMemory{T}"/> over a string. If the target string
    ///  is <see langword="null"/> a <see langword="default"/>(<see cref="ReadOnlyMemory{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    public static ReadOnlyMemory<char> AsMemoryOrDefault(this string? text)
        => text is not null ? text.AsMemory() : default;

    /// <summary>
    ///  Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the target string from
    ///  a specified position to the end of the string. If the target string is <see langword="null"/>
    ///  a <see langword="default"/>(<see cref="ReadOnlyMemory{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="start">
    ///  The index at which to begin this slice.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="start"/> is less than 0 or greater than <paramref name="text"/>.Length.
    /// </exception>
    public static ReadOnlyMemory<char> AsMemoryOrDefault(this string? text, int start)
        => text is not null ? text.AsMemory(start) : default;

    /// <summary>
    ///  Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the target string from
    ///  a specified position for a specified number of characters. If the target string is
    ///  <see langword="null"/> a <see langword="default"/>(<see cref="ReadOnlyMemory{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="start">
    ///  The index at which to begin this slice.
    /// </param>
    /// <param name="length">
    ///  The desired length for the slice.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="start"/>, <paramref name="length"/>, or <paramref name="start"/> + <paramref name="length"/>
    ///  is not in the range of <paramref name="text"/>.
    /// </exception>
    public static ReadOnlyMemory<char> AsMemoryOrDefault(this string? text, int start, int length)
        => text is not null ? text.AsMemory(start, length) : default;

    /// <summary>
    ///  Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the target string from
    ///  a specified position to the end of the string. If the target string is <see langword="null"/>
    ///  a <see langword="default"/>(<see cref="ReadOnlyMemory{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="startIndex">
    ///  The index at which to begin this slice.
    /// </param>
    public static ReadOnlyMemory<char> AsMemoryOrDefault(this string? text, Index startIndex)
    {
        if (text is null)
        {
            return default;
        }

#if NET
        return MemoryExtensions.AsMemory(text, startIndex);
#else
        return text.AsMemory(startIndex.GetOffset(text.Length));
#endif
    }

    /// <summary>
    ///  Creates a new <see cref="ReadOnlyMemory{T}"/> over a portion of the target string using the range
    ///  start and end indexes. If the target string is <see langword="null"/> a
    ///  <see langword="default"/>(<see cref="ReadOnlyMemory{T}"/>) is returned.
    /// </summary>
    /// <param name="text">
    ///  The target string.
    /// </param>
    /// <param name="range">
    ///  The range that has start and end indexes to use for slicing the string.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start or end index is not within the bounds of the string.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start index is greater than its end index.
    /// </exception>
    public static ReadOnlyMemory<char> AsMemoryOrDefault(this string? text, Range range)
    {
        if (text is null)
        {
            return default;
        }

#if NET
        return MemoryExtensions.AsMemory(text, range);
#else
        var (start, length) = range.GetOffsetAndLength(text.Length);
        return text.AsMemory(start, length);
#endif
    }

    /// <summary>
    ///  Returns a value indicating whether a specified character occurs within a string instance.
    /// </summary>
    /// <param name="text">
    ///  The string instance.
    /// </param>
    /// <param name="value">
    ///  The character to seek.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if the value parameter occurs within the string; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  This method exists on .NET Core, but doesn't on .NET Framework or .NET Standard 2.0.
    /// </remarks>
    public static bool Contains(this string text, char value)
    {
#if NET
        return text.Contains(value);
#else
        return text.IndexOf(value) >= 0;
#endif
    }

    /// <summary>
    ///  Returns a value indicating whether a specified character occurs within a string instance,
    ///  using the specified comparison rules.
    /// </summary>
    /// <param name="text">
    ///  The string instance.
    /// </param>
    /// <param name="value">
    ///  The character to seek.
    /// </param>
    /// <param name="comparisonType">
    ///  One of the enumeration values that specifies the rules to use in the comparison.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if the value parameter occurs within the string; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  This method exists on .NET Core, but doesn't on .NET Framework or .NET Standard 2.0.
    /// </remarks>
    public static bool Contains(this string text, char value, StringComparison comparisonType)
    {
#if NET
        return text.Contains(value, comparisonType);
#else
        return text.IndexOf(value, comparisonType) != 0;
#endif
    }

    /// <summary>
    ///  Reports the zero-based index of the first occurrence of the specified Unicode character in a string instance.
    ///  A parameter specifies the type of search to use for the specified character.
    /// </summary>
    /// <param name="text">
    ///  The string instance.
    /// </param>
    /// <param name="value">
    ///  The character to compare to the character at the start of this string.
    /// </param>
    /// <param name="comparisonType">
    ///  An enumeration value that specifies the rules for the search.
    /// </param>
    /// <returns>
    ///  The zero-based index of <paramref name="value"/> if that character is found, or -1 if it is not.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   Index numbering starts from zero.
    ///  </para>
    ///  <para>
    ///   The <paramref name="comparisonType"/> parameter is a <see cref="StringComparison"/> enumeration member
    ///   that specifies whether the search for the <paramref name="value"/> argument uses the current or invariant culture,
    ///   is case-sensitive or case-insensitive, or uses word or ordinal comparison rules.
    ///  </para>
    ///  <para>
    ///   This method exists on .NET Core, but doesn't on .NET Framework or .NET Standard 2.0.
    ///  </para>
    /// </remarks>
    public static int IndexOf(this string text, char value, StringComparison comparisonType)
    {
#if NET
        return text.IndexOf(value, comparisonType);
#else
        // [ch] produces a ReadOnlySpan<char> using a ref to ch.
        return text.AsSpan().IndexOf([value], comparisonType);
#endif
    }

    /// <summary>
    ///  Determines whether a string instance starts with the specified character.
    /// </summary>
    /// <param name="text">
    ///  The string instance.
    /// </param>
    /// <param name="value">
    ///  The character to compare to the character at the start of this string.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if <paramref name="value"/> matches the start of the string;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method performs an ordinal (case-sensitive and culture-insensitive) comparison.
    ///  </para>
    ///  <para>
    ///   This method exists on .NET Core, but doesn't on .NET Framework or .NET Standard 2.0.
    ///  </para>
    /// </remarks>
    public static bool StartsWith(this string text, char value)
    {
#if NET
        return text.StartsWith(value);
#else
        return text.Length > 0 && text[0] == value;
#endif
    }

    /// <summary>
    ///  Determines whether the end of a string instance matches the specified character.
    /// </summary>
    /// <param name="text">
    ///  The string instance.
    /// </param>
    /// <param name="value">
    ///  The character to compare to the character at the end of this string.
    /// </param>
    /// <returns>
    ///  <see langword="true"/> if <paramref name="value"/> matches the end of this string;
    ///  otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///  <para>
    ///   This method performs an ordinal (case-sensitive and culture-insensitive) comparison.
    ///  </para>
    ///  <para>
    ///   This method exists on .NET Core, but doesn't on .NET Framework or .NET Standard 2.0.
    ///  </para>
    /// </remarks>
    public static bool EndsWith(this string text, char value)
    {
#if NET
        return text.EndsWith(value);
#else
        return text.Length > 0 && text[^1] == value;
#endif
    }

    extension(string)
    {
        /// <summary>
        ///  Builds a string using a <see cref="MemoryBuilder{T}"/> of <see cref="ReadOnlyMemory{T}"/> of <see cref="char"/>
        ///  through the specified action delegate.
        /// </summary>
        /// <typeparam name="TState">
        ///  The type of the state object passed to the action.
        /// </typeparam>
        /// <param name="state">
        ///  The state object to pass to the action delegate.
        /// </param>
        /// <param name="action">
        ///  The delegate that operates on the memory builder to construct the string content.
        /// </param>
        /// <returns>
        ///  A string built from the chunks added to the memory builder by the action delegate.
        /// </returns>
        public static string Build<TState>(TState state, MemoryBuilderAction<ReadOnlyMemory<char>, TState> action)
        {
            var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
            try
            {
                action(ref builder, state);
                return builder.CreateString();
            }
            finally
            {
                builder.Dispose();
            }
        }

        /// <summary>
        ///  Attempts to build a string using a <see cref="MemoryBuilder{T}"/> of <see cref="ReadOnlyMemory{T}"/> of <see cref="char"/>
        ///  through the specified function delegate.
        /// </summary>
        /// <typeparam name="TState">
        ///  The type of the state object passed to the function.
        /// </typeparam>
        /// <param name="state">
        ///  The state object to pass to the function delegate.
        /// </param>
        /// <param name="func">
        ///  The delegate that operates on the memory builder and returns a boolean indicating success.
        /// </param>
        /// <returns>
        ///  A string built from the chunks added to the memory builder if the function returns <see langword="true"/>;
        ///  otherwise, <see langword="null"/>.
        /// </returns>
        public static string? TryBuild<TState>(TState state, MemoryBuilderFunc<ReadOnlyMemory<char>, TState, bool> func)
        {
            var builder = new MemoryBuilder<ReadOnlyMemory<char>>();
            try
            {
                if (func(ref builder, state))
                {
                    return builder.CreateString();
                }

                return null;
            }
            finally
            {
                builder.Dispose();
            }
        }
    }

#if !NET
    /// <summary>
    ///  Encapsulates a method that receives a span of objects of type <typeparamref name="T"/>
    ///  and a state object of type <typeparamref name="TArg"/>.
    /// </summary>
    /// <typeparam name="T">
    ///  The type of the objects in the span.
    /// </typeparam>
    /// <typeparam name="TArg">
    ///  The type of the object that represents the state.
    /// </typeparam>
    /// <param name="span">
    ///  A span of objects of type <typeparamref name="T"/>.
    /// </param>
    /// <param name="arg">
    ///  A state object of type <typeparamref name="TArg"/>.
    /// </param>
    public delegate void SpanAction<T, in TArg>(Span<T> span, TArg arg);

    extension(string)
    {
        /// <summary>
        ///  Creates a new string with a specific length and initializes it after creation by using the specified callback.
        /// </summary>
        /// <typeparam name="TState">
        ///  The type of the element to pass to <paramref name="action"/>.
        /// </typeparam>
        /// <param name="length">
        ///  The length of the string to create.
        /// </param>
        /// <param name="state">
        ///  The element to pass to <paramref name="action"/>.
        /// </param>
        /// <param name="action">
        ///  A callback to initialize the string
        /// </param>
        /// <returns>
        ///  The created string.
        /// </returns>
        /// <remarks>
        ///  The initial content of the destination span passed to <paramref name="action"/> is undefined.
        ///  Therefore, it is the delegate's responsibility to ensure that every element of the span is assigned.
        ///  Otherwise, the resulting string could contain random characters.
        /// </remarks>
        public static unsafe string Create<TState>(int length, TState state, SpanAction<char, TState> action)
        {
            ArgHelper.ThrowIfNegative(length);

            if (length == 0)
            {
                return string.Empty;
            }

            var result = new string('\0', length);

            fixed (char* ptr = result)
            {
                action(new Span<char>(ptr, length), state);
            }

            return result;
        }
    }
#endif
}
