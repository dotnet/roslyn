// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    internal readonly struct GeneratorState
    {
        public GeneratorState(GeneratorInfo info)
            : this(info, ImmutableDictionary<GeneratedSourceText, SyntaxTree>.Empty)
        {
        }

        private GeneratorState(GeneratorInfo info, ImmutableDictionary<GeneratedSourceText, SyntaxTree> sources)
        {
            this.Sources = sources;
            this.Info = info;
        }

        internal ImmutableDictionary<GeneratedSourceText, SyntaxTree> Sources { get; }

        internal GeneratorInfo Info { get; }

        internal GeneratorState WithSources(ImmutableDictionary<GeneratedSourceText, SyntaxTree> sources)
        {
            return new GeneratorState(this.Info, sources);
        }
    }
}
