// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    internal sealed class VirtualizedRelativePathResolver : RelativePathResolver
    {
        private readonly HashSet<string> _existingFullPaths;

        public VirtualizedRelativePathResolver(IEnumerable<string> existingFullPaths, string baseDirectory = null, ImmutableArray<string> searchPaths = default(ImmutableArray<string>))
            : base(searchPaths.NullToEmpty(), baseDirectory)
        {
            _existingFullPaths = new HashSet<string>(existingFullPaths, StringComparer.Ordinal);
        }

        protected override bool FileExists(string fullPath)
        {
            // normalize path to remove '..' and '.'
            return _existingFullPaths.Contains(Path.GetFullPath(fullPath));
        }
    }
}
