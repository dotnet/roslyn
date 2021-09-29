// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.PdbSourceDocument
{
    internal interface IPdbFileLocatorService
    {
        Task<(MetadataReader dllReader, MetadataReader pdbReader)?> GetMetadataReadersAsync(string dllPath, CancellationToken cancellationToken);
    }
}
