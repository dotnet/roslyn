// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class SyntaxNodeExtensions
    {
        public static ISymbol GetDeclaredOrReferencedSymbol(this SyntaxNode node, SemanticModel model)
        {
            if (node == null)
            {
                return null;
            }

            return model.GetDeclaredSymbol(node) ?? model.GetSymbolInfo(node).Symbol;
        }
    }
}
