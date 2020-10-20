// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

#nullable enable

namespace Microsoft.CodeAnalysis.MetadataAsSource
{
    internal interface IMetadataAsSourceFileLSPService : IWorkspaceService
    {
        /// <summary>
        /// Gets and opens the generated file for the given symbol.
        /// </summary>
        /// <remarks>
        /// Currently used solely by LSP go-to-def. Silently opens the file, meaning that the host user may not even realize
        /// the file has been opened as we intentionally do not make it visible.
        /// </remarks>
        Task<MetadataAsSourceFile?> GetAndOpenGeneratedFileAsync(ISymbol symbol, Project project, CancellationToken cancellationToken);
    }
}
