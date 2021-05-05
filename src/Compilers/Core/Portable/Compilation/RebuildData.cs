// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal sealed class RebuildData
    {
        /// <summary>
        /// This represents the set of document names for the #line / #ExternalSource directives
        /// that we need to emit into the PDB (in the order specified in the array).
        /// </summary>
        internal ImmutableArray<string> NonSourceFileDocumentNames { get; }

        internal BlobReader OptionsBlobReader { get; }

        internal RebuildData(
            BlobReader optionsBlobReader,
            ImmutableArray<string> nonSourceFileDocumentNames)
        {
            OptionsBlobReader = optionsBlobReader;
            NonSourceFileDocumentNames = nonSourceFileDocumentNames;
        }
    }
}
