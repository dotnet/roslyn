// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public sealed class TestSourceReferenceResolver : SourceReferenceResolver
    {
        public static readonly SourceReferenceResolver Default = new TestSourceReferenceResolver(sources: null);

        public static SourceReferenceResolver Create(Dictionary<string, string> sources = null)
        {
            return (sources == null || sources.Count == 0) ? Default : new TestSourceReferenceResolver(sources);
        }

        private readonly Dictionary<string, string> _sources;

        private TestSourceReferenceResolver(Dictionary<string, string> sources)
        {
            _sources = sources;
        }

        public override string NormalizePath(string path, string baseFilePath) => path;

        public override string ResolveReference(string path, string baseFilePath) => 
            _sources?.ContainsKey(path) == true ? path : null;

        public override Stream OpenRead(string resolvedPath)
        {
            if (_sources != null && resolvedPath != null)
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(_sources[resolvedPath]));
            }
            else
            {
                throw new IOException();
            }
        }

        public override bool Equals(object other) => ReferenceEquals(this, other);

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
