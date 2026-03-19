// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ObsoleteSymbol;

/// <summary>
/// Service which can analyze a span of a document and identify all locations of declarations or references to
/// symbols which are marked <see cref="ObsoleteAttribute"/>.
/// </summary>
internal interface IObsoleteSymbolService : ILanguageService
{
    Task<ImmutableArray<TextSpan>> GetLocationsAsync(Document document, ImmutableArray<TextSpan> textSpans, CancellationToken cancellationToken);
}
