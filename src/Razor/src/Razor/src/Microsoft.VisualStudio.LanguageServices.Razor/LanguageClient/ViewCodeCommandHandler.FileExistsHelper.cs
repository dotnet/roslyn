// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.VisualStudio.Razor.LanguageClient;

internal sealed partial class ViewCodeCommandHandler
{
    /// <summary>
    /// Simple helper that caches <see cref="File.Exists"/> results and evicts a results after
    /// <see cref="MaxMilliseconds"/> have passed.
    /// </summary>
    private readonly struct FileExistsHelper
    {
        private const int MaxMilliseconds = 10000;

        private readonly Stopwatch _watch;
        private readonly Dictionary<string, (bool Exists, long Milliseconds)> _cache;

        public FileExistsHelper()
        {
            _watch = Stopwatch.StartNew();
            _cache = new(PathUtilities.OSSpecificPathComparer);
        }

        public bool FileExists(string filePath)
        {
            var currentMilliseconds = _watch.ElapsedMilliseconds;

            // Calculate if we've never seen this file path or its cached value has expired.
            if (!_cache.TryGetValue(filePath, out var value) || CachedValueExpired(currentMilliseconds, value.Milliseconds))
            {
                var exists = File.Exists(filePath);
                value = (exists, currentMilliseconds);
                _cache[filePath] = value;
            }

            return value.Exists;

            static bool CachedValueExpired(long currentMilliseconds, long cachedMilliseconds)
            {
                return currentMilliseconds - cachedMilliseconds > MaxMilliseconds;
            }
        }
    }
}
