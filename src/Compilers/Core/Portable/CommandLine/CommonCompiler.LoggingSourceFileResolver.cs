// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingSourceFileResolver : SourceFileResolver
        {
            private readonly TouchedFileLogger? _logger;

            public LoggingSourceFileResolver(
                ImmutableArray<string> searchPaths,
                string? baseDirectory,
                ImmutableArray<KeyValuePair<string, string>> pathMap,
                TouchedFileLogger? logger)
                : base(searchPaths, baseDirectory, pathMap)
            {
                _logger = logger;
            }

            protected override bool FileExists(string? fullPath)
            {
                if (fullPath != null)
                {
                    _logger?.AddRead(fullPath);
                }

                return base.FileExists(fullPath);
            }

            public LoggingSourceFileResolver WithBaseDirectory(string value) =>
                (BaseDirectory == value) ? this : new LoggingSourceFileResolver(SearchPaths, value, PathMap, _logger);

            public LoggingSourceFileResolver WithSearchPaths(ImmutableArray<string> value) =>
                (SearchPaths == value) ? this : new LoggingSourceFileResolver(value, BaseDirectory, PathMap, _logger);
        }
    }
}
