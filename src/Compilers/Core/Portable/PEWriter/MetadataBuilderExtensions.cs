// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis;

internal static class MetadataBuilderExtensions
{
    internal static BlobHandle GetOrAddBlobAndFree(this MetadataBuilder metadataBuilder, PooledBlobBuilder builder)
    {
        var handle = metadataBuilder.GetOrAddBlob(builder);
        builder.Free();
        return handle;
    }
}
