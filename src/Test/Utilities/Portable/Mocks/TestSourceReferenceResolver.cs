// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public sealed class TestSourceReferenceResolver : SourceReferenceResolver
    {
        public static readonly SourceReferenceResolver Default = new TestSourceReferenceResolver(sources: null);

        public static SourceReferenceResolver Create(params KeyValuePair<string, string>[] sources)
        {
            return new TestSourceReferenceResolver(sources.ToDictionary(p => p.Key, p => (object)p.Value));
        }

        public static SourceReferenceResolver Create(params KeyValuePair<string, object>[] sources)
        {
            return new TestSourceReferenceResolver(sources.ToDictionary(p => p.Key, p => p.Value));
        }

        public static SourceReferenceResolver Create(Dictionary<string, string> sources = null)
        {
            return new TestSourceReferenceResolver(sources.ToDictionary(p => p.Key, p => (object)p.Value));
        }

        private readonly Dictionary<string, object> _sources;

        private TestSourceReferenceResolver(Dictionary<string, object> sources)
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
                var data = _sources[resolvedPath];
                return new MemoryStream((data is string) ? Encoding.UTF8.GetBytes((string)data) : (byte[])data);
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
