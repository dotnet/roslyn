// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Features.Definitions
{
    /// <summary>
    /// XamlDefinition with symbol.
    /// </summary>
    internal sealed class XamlSymbolDefinition : XamlDefinition
    {
        public ISymbol Symbol { get; }

        public XamlSymbolDefinition(ISymbol symbol)
        {
            Symbol = symbol;
        }
    }
}
