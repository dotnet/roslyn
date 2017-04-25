// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.DocumentHighlighting
{
    internal interface IDocumentHighlightingAdditionalReferenceProvider : ILanguageService
    {
        Task<ImmutableArray<Location>> GetAdditionalReferencesAsync(Document document, ISymbol symbol, CancellationToken cancellationToken);
    }
}