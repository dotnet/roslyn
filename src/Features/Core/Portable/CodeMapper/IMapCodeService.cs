// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.CodeMapping;

internal interface IMapCodeService : ILanguageService
{
    Task<Document> MapCodeAsync(Document document, ImmutableArray<string> contents, ImmutableArray<DocumentSpan> focusLocations, bool formatMappedCode, CancellationToken cancellationToken);
}

