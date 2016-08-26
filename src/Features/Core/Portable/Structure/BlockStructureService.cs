// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class BlockStructureService : ILanguageService
    {
        /// <summary>
        /// Gets the service corresponding to the specified document.
        /// </summary>
        public static BlockStructureService GetService(Document document)
        {
            return document.GetLanguageService<BlockStructureService>();
        }

        /// <summary>
        /// The language from <see cref="LanguageNames"/> this service corresponds to.
        /// </summary>
        public abstract string Language { get; }

        public abstract Task<BlockStructure> GetBlockStructureAsync(Document document, CancellationToken cancellationToken);

        public abstract BlockStructure GetBlockStructure(Document document, CancellationToken cancellationToken);
    }
}