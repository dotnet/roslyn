// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Structure;

internal abstract class BlockStructureService : ILanguageService
{
    /// <summary>
    /// Gets the service corresponding to the specified document.
    /// </summary>
    public static BlockStructureService GetService(Document document)
        => document.GetLanguageService<BlockStructureService>();

    /// <summary>
    /// The language from <see cref="LanguageNames"/> this service corresponds to.
    /// </summary>
    public abstract string Language { get; }

    public abstract Task<BlockStructure> GetBlockStructureAsync(Document document, BlockStructureOptions options, CancellationToken cancellationToken);
}
