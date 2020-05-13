// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.IncrementalCaches
{
    internal partial class SymbolTreeInfoIncrementalAnalyzerProvider
    {
        private readonly struct MetadataInfo
        {
            /// <summary>
            /// Can't be null.  Even if we weren't able to read in metadata, we'll still create an empty
            /// index.
            /// </summary>
            public readonly SymbolTreeInfo SymbolTreeInfo;

            /// <summary>
            /// The set of projects that are referencing this metadata-index.  When this becomes empty we can dump the
            /// index from memory.
            /// <para/>
            /// Note: the Incremental-Analyzer infrastructure guarantees that it will call all the
            /// methods on <see cref="SymbolTreeInfoIncrementalAnalyzer"/> in a serial fashion.  As that is the only
            /// type that reads/writes these <see cref="MetadataInfo"/> objects, we don't need to lock this.
            /// </summary>
            public readonly HashSet<ProjectId> ReferencingProjects;

            public MetadataInfo(SymbolTreeInfo info, HashSet<ProjectId> referencingProjects)
            {
                Contract.ThrowIfNull(info);
                SymbolTreeInfo = info;
                ReferencingProjects = referencingProjects;
            }
        }
    }
}
