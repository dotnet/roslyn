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
    // PROTOTYPE: should this implement ICollection?
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public sealed class AdditionalSourcesCollection
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
            if (Contains(hintName))
            {
                throw new ArgumentException($"Parameter {nameof(hintName)} must be a unique value.", nameof(hintName));
            }
            _sourcesAdded.Add(hintName, source);
        }

        public void RemoveSource(string hintName)
        {
            _sourcesAdded.Remove(hintName);
        }

        public bool Contains(string hintName) => _sourcesAdded.ContainsKey(hintName);

        internal ImmutableArray<GeneratedSourceText> ToImmutableAndFree()
        {
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
