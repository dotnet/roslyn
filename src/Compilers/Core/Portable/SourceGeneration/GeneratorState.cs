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
            : this(info, ImmutableArray<GeneratedSourceText>.Empty)
        {
        }

        private GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSourceText> sources)
        {
            this.Sources = sources;
            this.Info = info;
        }

        internal ImmutableArray<GeneratedSourceText> Sources { get; }

        internal GeneratorInfo Info { get; }

        internal GeneratorState WithSources(ImmutableArray<GeneratedSourceText> sources)
        {
            return new GeneratorState(this.Info, sources);
        }
    }
}
