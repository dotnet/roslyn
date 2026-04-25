// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis.Razor.SemanticTokens;

internal interface ISemanticTokensLegendService
{
    SemanticTokenModifiers TokenModifiers { get; }
    SemanticTokenTypes TokenTypes { get; }
}
