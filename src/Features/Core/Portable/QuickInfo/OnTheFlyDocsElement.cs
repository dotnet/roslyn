// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.QuickInfo;

/// <summary>
/// Represents the data needed to provide on-the-fly documentation from an <see cref="ISymbol"/>.
/// </summary>
/// <param name="symbolSignature">formatted string representation of an <see cref="ISymbol"/></param>
/// <param name="declarationCode">the symbol's declaration code</param>
internal sealed class OnTheFlyDocsElement(string symbolSignature, ImmutableArray<string> declarationCode)
{
    public string SymbolSignature { get; } = symbolSignature;
    public ImmutableArray<string> DeclarationCode { get; } = declarationCode;
}
