// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    internal sealed class AdditionalSourcesCollection
    {
        private readonly PooledDictionary<string, SourceText> _sourcesAdded;

        internal AdditionalSourcesCollection()
        {
            _sourcesAdded = PooledDictionary<string, SourceText>.GetInstance();
        }

        internal AdditionalSourcesCollection(ImmutableArray<GeneratedSourceText> sources)
            : this()
        {
            foreach (var source in sources)
            {
                _sourcesAdded.Add(source.HintName, source.Text);
            }
        }

        public void Add(string hintName, SourceText source)
        {
            _sourcesAdded.Add(hintName, source);
        }

        public void RemoveSource(string hintName)
        {
            _sourcesAdded.Remove(hintName);
        }

        public bool Contains(string hintName) => _sourcesAdded.ContainsKey(hintName);

        internal ImmutableArray<GeneratedSourceText> ToImmutableAndFree()
        {
            // https://github.com/dotnet/roslyn/issues/42627: This needs to be consistently ordered
            ArrayBuilder<GeneratedSourceText> builder = ArrayBuilder<GeneratedSourceText>.GetInstance();
            foreach (var (hintName, sourceText) in _sourcesAdded)
            {
                builder.Add(new GeneratedSourceText(hintName, sourceText));
            }
            _sourcesAdded.Free();
            return builder.ToImmutableAndFree();
        }
    }
}
