// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        internal sealed class LoggingSourceFileResolver : SourceFileResolver
        {
            private readonly TouchedFileLogger _loggerOpt;

            public LoggingSourceFileResolver(
                ImmutableArray<string> searchPaths,
                string baseDirectory,
                ImmutableArray<KeyValuePair<string, string>> pathMap,
                TouchedFileLogger logger)
                : base(searchPaths, baseDirectory, pathMap)
            {
                _loggerOpt = logger;
            }

            protected override bool FileExists(string fullPath)
            {
                if (fullPath != null)
                {
                    _loggerOpt?.AddRead(fullPath);
                }

                return base.FileExists(fullPath);
            }

            public LoggingSourceFileResolver WithBaseDirectory(string value) => 
                (BaseDirectory == value) ? this : new LoggingSourceFileResolver(SearchPaths, value, PathMap, _loggerOpt);

            public LoggingSourceFileResolver WithSearchPaths(ImmutableArray<string> value) => 
                (SearchPaths == value) ? this : new LoggingSourceFileResolver(value, BaseDirectory, PathMap, _loggerOpt);
        }
    }
}
