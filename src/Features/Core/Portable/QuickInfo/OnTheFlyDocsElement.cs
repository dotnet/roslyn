// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.QuickInfo;

/// <summary>
/// Represents the data needed to provide on-the-fly documentation from the symbol.
/// </summary>
/// <param name="symbolSignature">formatted string representation of a symbol/></param>
/// <param name="declarationCode">the symbol's declaration code</param>
/// <param name="language">the language of the symbol</param>
/// <param name="hasComments">whether the symbol has comments</param>
internal sealed class OnTheFlyDocsElement(string symbolSignature, ImmutableArray<string> declarationCode, string language, bool isContentExcluded, bool hasComments = false)
{
    public string SymbolSignature { get; } = symbolSignature;
    public ImmutableArray<string> DeclarationCode { get; } = declarationCode;
    public string Language { get; } = language;
    public bool IsContentExcluded { get; set; } = isContentExcluded;

    // Added for telemetry collection purposes.
    public bool HasComments { get; set; } = hasComments;
}
