// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.Xaml.Definitions;

/// <summary>
/// XamlDefinition with symbol.
/// </summary>
internal class XamlSymbolDefinition(ISymbol symbol) : XamlDefinition
{
    public ISymbol Symbol { get; } = symbol;
}
