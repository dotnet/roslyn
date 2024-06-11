// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

#if NETCOREAPP
using System.Diagnostics.CodeAnalysis;
#endif

namespace Microsoft.SourceLink.Tools
{
    /// <summary>
    /// Source Link URL map. Maps file paths matching Source Link patterns to URLs.
    /// </summary>
    internal readonly struct SourceLinkMap
    {
        private readonly ReadOnlyCollection<Entry> _entries;

        private SourceLinkMap(ReadOnlyCollection<Entry> mappings)
        {
            _entries = mappings;
        }

        public readonly struct Entry(FilePathPattern filePath, UriPattern uri)
        {
            public readonly FilePathPattern FilePath = filePath;
            public readonly UriPattern Uri = uri;

            public void Deconstruct(out FilePathPattern filePath, out UriPattern uri)
            {
                filePath = FilePath;
                uri = Uri;
            }
        }

        public readonly struct FilePathPattern(string path, bool isPrefix)
        {
            public readonly string Path = path;
            public readonly bool IsPrefix = isPrefix;
        }

        public readonly struct UriPattern(string prefix, string suffix)
        {
            public readonly string Prefix = prefix;
            public readonly string Suffix = suffix;
        }

        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>
        /// Parses Source Link JSON string.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="json"/> is null.</exception>
        /// <exception cref="InvalidDataException">The JSON does not follow Source Link specification.</exception>
        /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON string.</exception>
        public static SourceLinkMap Parse(string json)
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            var list = new List<Entry>();

            var root = JsonDocument.Parse(json, new JsonDocumentOptions() { AllowTrailingCommas = true }).RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException();
            }

            foreach (var rootEntry in root.EnumerateObject())
            {
                if (!rootEntry.NameEquals("documents"))
                {
                    // potential future extensibility
                    continue;
                }

                if (rootEntry.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException();
                }

                foreach (var documentsEntry in rootEntry.Value.EnumerateObject())
                {
                    if (documentsEntry.Value.ValueKind != JsonValueKind.String ||
                        !TryParseEntry(documentsEntry.Name, documentsEntry.Value.GetString()!, out var entry))
                    {
                        throw new InvalidDataException();
                    }

                    list.Add(entry);
                }
            }

            // Sort the map by decreasing file path length. This ensures that the most specific paths will checked before the least specific
            // and that absolute paths will be checked before a wildcard path with a matching base
            list.Sort((left, right) => -left.FilePath.Path.Length.CompareTo(right.FilePath.Path.Length));

            return new SourceLinkMap(new ReadOnlyCollection<Entry>(list));
        }

        private static bool TryParseEntry(string key, string value, out Entry entry)
        {
            entry = default;

            // VALIDATION RULES
            // 1. The only acceptable wildcard is one and only one '*', which if present will be replaced by a relative path
            // 2. If the filepath does not contain a *, the uri cannot contain a * and if the filepath contains a * the uri must contain a *
            // 3. If the filepath contains a *, it must be the final character
            // 4. If the uri contains a *, it may be anywhere in the uri
            if (key.Length == 0)
            {
                return false;
            }

            var filePathStar = key.IndexOf('*');
            if (filePathStar == key.Length - 1)
            {
                key = key[..filePathStar];
            }
            else if (filePathStar >= 0)
            {
                return false;
            }

            string uriPrefix, uriSuffix;
            var uriStar = value.IndexOf('*');
            if (uriStar >= 0)
            {
                if (filePathStar < 0)
                {
                    return false;
                }

                uriPrefix = value[..uriStar];
                uriSuffix = value[(uriStar + 1)..];

                if (uriSuffix.IndexOf('*') >= 0)
                {
                    return false;
                }
            }
            else
            {
                uriPrefix = value;
                uriSuffix = "";
            }

            entry = new Entry(
                new FilePathPattern(key, isPrefix: filePathStar >= 0),
                new UriPattern(uriPrefix, uriSuffix));

            return true;
        }

        /// <summary>
        /// Maps specified <paramref name="path"/> to the corresponding URL.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        public bool TryGetUri(
            string path,
#if NETCOREAPP
            [NotNullWhen(true)]
#endif
            out string? uri)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.IndexOf('*') >= 0)
            {
                uri = null;
                return false;
            }

            // Note: the mapping function is case-insensitive.

            foreach (var (file, mappedUri) in _entries)
            {
                if (file.IsPrefix)
                {
                    if (path.StartsWith(file.Path, StringComparison.OrdinalIgnoreCase))
                    {
                        var escapedPath = string.Join("/", path[file.Path.Length..].Split(new[] { '/', '\\' }).Select(Uri.EscapeDataString));
                        uri = mappedUri.Prefix + escapedPath + mappedUri.Suffix;
                        return true;
                    }
                }
                else if (string.Equals(path, file.Path, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Assert(mappedUri.Suffix.Length == 0);
                    uri = mappedUri.Prefix;
                    return true;
                }
            }

            uri = null;
            return false;
        }
    }
}
