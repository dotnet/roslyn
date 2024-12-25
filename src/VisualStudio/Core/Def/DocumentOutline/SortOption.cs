// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.LanguageServices.DocumentOutline;

/// <summary>
/// The Sort Option to be applied to the document symbol data model.
/// </summary>
internal enum SortOption
{
    /// <summary>
    /// Sort by document symbol name.
    /// </summary>
    Name,
    /// <summary>
    /// Sort by document symbol location in a document (by comparing each symbol's range start position).
    /// </summary>
    Location,
    /// <summary>
    /// Sort by document symbol <see cref="Roslyn.LanguageServer.Protocol.SymbolKind"/>.
    /// </summary>
    /// <remarks>
    /// At the moment, we order the symbols by the SymbolKind enum values. If this ordering turns out to be
    /// undesirable, we can always add a preferred ordering later on.
    /// </remarks>
    Type
}
