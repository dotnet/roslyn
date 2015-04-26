// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.RQName;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Helpers related to <see cref="RQName"/>s.
    /// </summary>
    public static class RQName
    {
        /// <summary>
        /// Returns an RQName for the given symbol, or <code>null</code>if the symbol cannot be represented by an RQName.
        /// </summary>
        /// <param name="symbol">The symbol to build an RQName for.</param>
        public static string From(ISymbol symbol)
        {
            var node = RQNodeBuilder.Build(symbol);
            return (node != null) ? ParenthesesTreeWriter.ToParenthesesFormat(node.ToSimpleTree()) : null;
        }
    }
}
