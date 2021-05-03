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
        internal ImmutableArray<string> NonSourceFileDocumentNames { get; }
        internal BlobReader OptionsBlobReader { get; }

        internal RebuildData(
            BlobReader optionsBlobReader,
            MetadataReader pdbReader,
            int sourceFileCount)
        {
            OptionsBlobReader = optionsBlobReader;

            var count = pdbReader.Documents.Count - sourceFileCount;
            var builder = ArrayBuilder<string>.GetInstance(count);
            foreach (var documentHandle in pdbReader.Documents.Skip(sourceFileCount))
            {
                var document = pdbReader.GetDocument(documentHandle);
                var name = pdbReader.GetString(document.Name);
                builder.Add(name);
            }
            NonSourceFileDocumentNames = builder.ToImmutableAndFree();
        }
    }
}
