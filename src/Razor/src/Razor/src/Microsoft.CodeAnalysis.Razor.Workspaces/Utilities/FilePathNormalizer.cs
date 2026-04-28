// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.Extensions.Internal;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

internal static class FilePathNormalizer
{
    private static readonly Func<char, char> s_charConverter = PlatformInformation.IsLinux
        ? c => c
        : char.ToLowerInvariant;

    public static string NormalizeDirectory(string? directoryFilePath)
    {
        if (directoryFilePath.IsNullOrEmpty() || directoryFilePath == "/")
        {
            return "/";
        }

        var directoryFilePathSpan = directoryFilePath.AsSpan();

        // Ensure that the array is at least 1 character larger, so that we can add
        // a trailing space after normalization if necessary.
        var arrayLength = directoryFilePathSpan.Length + 1;
        using var _ = ArrayPool<char>.Shared.GetPooledArraySpan(arrayLength, out var destination);
        var (start, length) = NormalizeCore(directoryFilePathSpan, destination);
        ReadOnlySpan<char> normalizedSpan = destination.Slice(start, length);

        // Add a trailing slash if the normalized span doesn't end in one.
        if (normalizedSpan is not [.., '/'])
        {
            destination[start + length] = '/';
            normalizedSpan = destination.Slice(start, length + 1);
        }

        if (directoryFilePathSpan.Equals(normalizedSpan, StringComparison.Ordinal))
        {
            return directoryFilePath;
        }

        return CreateString(normalizedSpan);
    }

    public static string Normalize(string? filePath)
    {
        if (filePath.IsNullOrEmpty())
        {
            return "/";
        }

        var filePathSpan = filePath.AsSpan();

        // Rent a buffer for Normalize to write to.
        using var _ = ArrayPool<char>.Shared.GetPooledArraySpan(filePathSpan.Length, out var destination);
        var normalizedSpan = NormalizeCoreAndGetSpan(filePathSpan, destination);

        // If we didn't change anything, just return the original string.
        if (filePathSpan.Equals(normalizedSpan, StringComparison.Ordinal))
        {
            return filePath;
        }

        // Otherwise, create a new string from our normalized char buffer.
        return CreateString(normalizedSpan);
    }

    /// <summary>
    ///  Returns the directory portion of the given file path in normalized form.
    /// </summary>
    public static string GetNormalizedDirectoryName(string? filePath)
    {
        if (filePath.IsNullOrEmpty() || filePath == "/")
        {
            return "/";
        }

        var filePathSpan = filePath.AsSpan();

        using var _1 = ArrayPool<char>.Shared.GetPooledArraySpan(filePathSpan.Length, out var destination);
        var directoryNameSpan = NormalizeDirectoryNameCore(filePathSpan, destination);

        if (filePathSpan.Equals(directoryNameSpan, StringComparison.Ordinal))
        {
            return filePath;
        }

        return CreateString(directoryNameSpan);
    }

    public static bool AreDirectoryPathsEquivalent(string? filePath1, string? filePath2)
    {
        var filePathSpan1 = filePath1.AsSpanOrDefault();
        var filePathSpan2 = filePath2.AsSpanOrDefault();

        if (filePathSpan1.IsEmpty)
        {
            return filePathSpan2.IsEmpty;
        }
        else if (filePathSpan2.IsEmpty)
        {
            return false;
        }

        using var _1 = ArrayPool<char>.Shared.GetPooledArraySpan(filePathSpan1.Length, out var destination1);
        var normalizedSpan1 = NormalizeDirectoryNameCore(filePathSpan1, destination1);

        using var _2 = ArrayPool<char>.Shared.GetPooledArraySpan(filePathSpan2.Length, out var destination2);
        var normalizedSpan2 = NormalizeDirectoryNameCore(filePathSpan2, destination2);

        return normalizedSpan1.Equals(normalizedSpan2, PathUtilities.OSSpecificPathComparison);
    }

    public static bool AreFilePathsEquivalent(string? filePath1, string? filePath2)
    {
        var filePathSpan1 = filePath1.AsSpanOrDefault();
        var filePathSpan2 = filePath2.AsSpanOrDefault();

        if (filePathSpan1.IsEmpty)
        {
            return filePathSpan2.IsEmpty;
        }
        else if (filePathSpan2.IsEmpty)
        {
            return false;
        }

        using var _1 = ArrayPool<char>.Shared.GetPooledArraySpan(filePathSpan1.Length, out var destination1);
        var normalizedSpan1 = NormalizeCoreAndGetSpan(filePathSpan1, destination1);

        using var _2 = ArrayPool<char>.Shared.GetPooledArraySpan(filePathSpan2.Length, out var destination2);
        var normalizedSpan2 = NormalizeCoreAndGetSpan(filePathSpan2, destination2);

        return normalizedSpan1.Equals(normalizedSpan2, PathUtilities.OSSpecificPathComparison);
    }

    public static int GetHashCode(string filePath)
    {
        if (filePath.Length == 0)
        {
            return filePath.GetHashCode();
        }

        var filePathSpan = filePath.AsSpanOrDefault();

        using var _ = ArrayPool<char>.Shared.GetPooledArraySpan(filePathSpan.Length, out var destination);
        var normalizedSpan = NormalizeCoreAndGetSpan(filePathSpan, destination);

        var hashCombiner = HashCodeCombiner.Start();

        foreach (var ch in normalizedSpan)
        {
            hashCombiner.Add(s_charConverter(ch));
        }

        return hashCombiner.CombinedHash;
    }

    private static ReadOnlySpan<char> NormalizeCoreAndGetSpan(ReadOnlySpan<char> source, Span<char> destination)
    {
        var (start, length) = NormalizeCore(source, destination);
        return destination.Slice(start, length);
    }

    private static ReadOnlySpan<char> NormalizeDirectoryNameCore(ReadOnlySpan<char> source, Span<char> destination)
    {
        var normalizedSpan = NormalizeCoreAndGetSpan(source, destination);

        var lastSlashIndex = normalizedSpan.LastIndexOf('/');

        return lastSlashIndex >= 0
            ? normalizedSpan[..(lastSlashIndex + 1)] // Include trailing slash
            : normalizedSpan;
    }

    /// <summary>
    ///  Normalizes the given <paramref name="source"/> file path and writes the result in <paramref name="destination"/>.
    /// </summary>
    /// <param name="source">The span to normalize.</param>
    /// <param name="destination">The span to write to.</param>
    /// <returns>
    ///  Returns a tuple containing the start index and length of the normalized path within <paramref name="destination"/>.
    /// </returns>
    private static (int start, int length) NormalizeCore(ReadOnlySpan<char> source, Span<char> destination)
    {
        if (source.IsEmpty)
        {
            if (destination.Length < 1)
            {
                throw new ArgumentException("Destination length must be at least 1 if the source is empty.", nameof(destination));
            }

            destination[0] = '/';

            return (start: 0, length: 1);
        }

        if (destination.Length < source.Length)
        {
            throw new ArgumentException("Destination length must be greater or equal to the source length.", nameof(destination));
        }

        int charsWritten;

        // Note: We check for '%' characters before calling UrlDecoder.Decode to ensure that we *only*
        // decode when there are '%XX' entities. So, calling Normalize on a path and then calling Normalize
        // on the result will not call Decode twice.
        if (source.Contains("%".AsSpan(), StringComparison.Ordinal))
        {
            UrlDecoder.Decode(source, destination, out charsWritten);
        }
        else
        {
            source.CopyTo(destination);
            charsWritten = source.Length;
        }

        // Replace slashes in our normalized span.
        NormalizeAndDedupeSlashes(destination, ref charsWritten);

        if (PlatformInformation.IsWindows &&
            charsWritten > 1 &&
            destination is ['/', ..] and not ['/', '/', ..])
        {
            // We've been provided a path that probably looks something like /C:/path/to.
            // So, we adjust the result to skip the leading '/'.
            return (start: 1, length: charsWritten - 1);
        }
        else
        {
            // Already a valid path like C:/path or //path
            return (start: 0, length: charsWritten);
        }
    }

    private static void NormalizeAndDedupeSlashes(Span<char> span, ref int charsWritten)
    {
        ref var src = ref MemoryMarshal.GetReference(span);

        var write = 0;
        for (var read = 0; read < charsWritten; read++, write++)
        {
            ref var readSlot = ref Unsafe.Add(ref src, read);
            ref var writeSlot = ref Unsafe.Add(ref src, write);

            if (readSlot is '\\' or '/')
            {
                writeSlot = '/';

                // Be careful not to read past the end of the span.
                if (read < charsWritten - 1 && Unsafe.Add(ref readSlot, 1) is '/' or '\\')
                {
                    // We found two slashes in a row. If we are at the start of the path,
                    // we we are at '\\network' paths, so want to leave them alone. Otherwise
                    // we skip over one of them to de-dupe
                    if (read == 0)
                    {
                        writeSlot = '\\';
                        writeSlot = ref Unsafe.Add(ref writeSlot, 1);
                        writeSlot = '\\';
                        read++;
                        write++;
                    }
                    else if (read > 0)
                    {
                        read++;
                    }
                }
            }
            else
            {
                writeSlot = readSlot;
            }
        }

        charsWritten = write;
    }

    private static unsafe string CreateString(ReadOnlySpan<char> source)
    {
        if (source.IsEmpty)
        {
            return string.Empty;
        }

        fixed (char* ptr = source)
        {
            return new string(ptr, 0, source.Length);
        }
    }
}
