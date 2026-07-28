// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Text;
using System.Text.Json;
using NuGet.Versioning;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Reads the <c>libraries</c> section of a <c>project.assets.json</c> file to discover which packages a
/// restore has already resolved. Only the library keys are inspected, so the file is streamed through a
/// small fixed buffer instead of being materialized into a <c>LockFile</c> model.
/// </summary>
internal static class ProjectAssetsReader
{
    /// <summary>
    /// Initial size of the read buffer. It grows if a single JSON token does not fit, which real assets files
    /// never require; the longest token in one is a few hundred bytes.
    /// </summary>
    private const int BufferSize = 16 * 1024;

    /// <summary>
    /// Upper bound on the length of a library key decoded on the stack. Keys are a package id (which NuGet
    /// limits to 100 characters) plus a version, so realistic keys stay well below this.
    /// </summary>
    private const int MaxStackAllocatedKeyLength = 512;

    private static ReadOnlySpan<byte> Utf8Bom => Encoding.UTF8.Preamble;

    /// <summary>
    /// Sets the entry in <paramref name="resolvedReferences"/> for each item in
    /// <paramref name="packageReferences"/> that the assets file lists at a version satisfying its range.
    /// Entries are only ever set to <see langword="true"/>, so <paramref name="resolvedReferences"/> must
    /// start cleared.
    /// </summary>
    /// <param name="assetsFileVersion">
    /// Set to the value of the top level <c>version</c> property once it is read, and left unchanged if the
    /// file does not have an integer one. Reported when the file cannot be read, so it is passed by reference
    /// to stay available to the caller if parsing later fails.
    /// </param>
    public static void FindResolvedPackageReferences(
        string projectAssetsPath,
        ReadOnlySpan<PackageReferenceItem> packageReferences,
        Span<bool> resolvedReferences,
        ref int? assetsFileVersion)
    {
        Contract.ThrowIfFalse(packageReferences.Length == resolvedReferences.Length);

        // A buffer size of 1 disables FileStream's internal buffer; this reader does its own buffering.
        using var stream = new FileStream(projectAssetsPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            var state = new JsonReaderState();
            var isFinalBlock = false;

            // Utf8JsonReader does not consume a byte order mark, so strip one before parsing begins.
            var bufferedCount = stream.ReadAtLeast(buffer, Utf8Bom.Length, throwOnEndOfStream: false);
            if (buffer.AsSpan(0, bufferedCount).StartsWith(Utf8Bom))
            {
                bufferedCount -= Utf8Bom.Length;
                buffer.AsSpan(Utf8Bom.Length, bufferedCount).CopyTo(buffer);
            }

            // Depth of the object value of the top level "libraries" property, or -1 while outside of it.
            var librariesDepth = -1;
            var atLibrariesValue = false;
            var atVersionValue = false;

            while (!isFinalBlock)
            {
                if (bufferedCount == buffer.Length)
                {
                    // The buffer holds a single incomplete token, so it has to grow for that token to be read.
                    var grownBuffer = ArrayPool<byte>.Shared.Rent(checked(buffer.Length * 2));
                    buffer.AsSpan(0, bufferedCount).CopyTo(grownBuffer);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = grownBuffer;
                }

                var readCount = stream.Read(buffer, bufferedCount, buffer.Length - bufferedCount);
                bufferedCount += readCount;
                isFinalBlock = readCount == 0;

                var reader = new Utf8JsonReader(buffer.AsSpan(0, bufferedCount), isFinalBlock, state);
                // Expected json looks something like the following:
                // {
                //   "version": 3,
                //   "targets": {},
                //   "libraries": {
                //     "Newtonsoft.Json/13.0.3": {
                //       "sha512": "hash",
                //       "type": "package",
                //       "path": "newtonsoft.json/13.0.3",
                //       "files": [
                //         ".nupkg.metadata",
                //         "lib/net6.0/Newtonsoft.Json.dll"
                //   	  ]
                //   },
                while (reader.Read())
                {
                    if (atLibrariesValue)
                    {
                        atLibrariesValue = false;
                        if (reader.TokenType == JsonTokenType.StartObject)
                            librariesDepth = reader.CurrentDepth;
                    }
                    else if (atVersionValue)
                    {
                        atVersionValue = false;
                        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var version))
                            assetsFileVersion = version;
                    }
                    else if (librariesDepth >= 0)
                    {
                        if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == librariesDepth + 1)
                            MarkResolvedPackageReferences(ref reader, packageReferences, resolvedReferences);
                        else if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == librariesDepth)
                            librariesDepth = -1;
                    }
                    else if (reader.TokenType == JsonTokenType.PropertyName && reader.CurrentDepth == 1)
                    {
                        if (reader.ValueTextEquals("libraries"))
                            atLibrariesValue = true;
                        else if (reader.ValueTextEquals("version"))
                            atVersionValue = true;
                    }
                }

                state = reader.CurrentState;

                // Keep the trailing partial token so the next read can complete it.
                var consumedCount = checked((int)reader.BytesConsumed);
                bufferedCount -= consumedCount;
                buffer.AsSpan(consumedCount, bufferedCount).CopyTo(buffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void MarkResolvedPackageReferences(
        ref Utf8JsonReader reader,
        ReadOnlySpan<PackageReferenceItem> packageReferences,
        Span<bool> resolvedReferences)
    {
        // Decoding to UTF-16 gives escaped and unescaped keys a single code path. A decoded key is never
        // longer than its raw UTF-8 form, so only implausibly long keys fall back to allocating.
        Span<char> keyBuffer = stackalloc char[MaxStackAllocatedKeyLength];
        var libraryKey = reader.ValueSpan.Length <= keyBuffer.Length
            ? keyBuffer[..reader.CopyString(keyBuffer)]
            : reader.GetString().AsSpan();

        // Package and project libraries both use "Name/Version" keys, and the lock file model this replaced
        // matched against both. A key without that shape belongs to neither.
        var separatorIndex = libraryKey.LastIndexOf('/');
        if (separatorIndex <= 0 || separatorIndex == libraryKey.Length - 1)
            return;

        var packageId = libraryKey[..separatorIndex];
        var resolvedVersion = libraryKey[(separatorIndex + 1)..];
        for (var i = 0; i < packageReferences.Length; i++)
        {
            if (resolvedReferences[i] || !packageId.Equals(packageReferences[i].Name, StringComparison.OrdinalIgnoreCase))
                continue;

            // Only packages the project references get this far, so versions are allocated a handful of times.
            if (NuGetVersion.TryParse(resolvedVersion.ToString(), out var version) &&
                GetVersionRange(packageReferences[i]).Satisfies(version))
            {
                resolvedReferences[i] = true;
            }
        }
    }

    private static VersionRange GetVersionRange(PackageReferenceItem reference)
        => VersionRange.TryParse(reference.VersionRange, out var versionRange) ? versionRange : VersionRange.All;
}
