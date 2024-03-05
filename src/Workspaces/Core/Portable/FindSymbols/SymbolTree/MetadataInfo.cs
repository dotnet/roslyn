// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.SymbolTree;

internal sealed partial class SymbolTreeInfoCacheServiceFactory
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
        /// </summary>
        /// <remarks>
        /// <para>Accesses to this collection must lock the set.</para>
        /// </remarks>
        public readonly HashSet<ProjectId> ReferencingProjects;

        public MetadataInfo(SymbolTreeInfo info, HashSet<ProjectId> referencingProjects)
        {
            Contract.ThrowIfNull(info);
            SymbolTreeInfo = info;
            ReferencingProjects = referencingProjects;
        }
    }
}
