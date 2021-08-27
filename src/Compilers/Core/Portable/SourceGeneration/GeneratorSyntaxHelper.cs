// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal abstract class GeneratorSyntaxHelper
    {
        public abstract bool IsAttribute(SyntaxNode node);

        public bool TryGetAttributeData(string attributeFQN, SyntaxNode node, SemanticModel model, CancellationToken cancellationToken, [NotNullWhen(true)] out SyntaxNode? attributedSyntax, [NotNullWhen(true)] out AttributeData? attributeData)
        {
            attributeData = null;
            attributedSyntax = node.Parent?.Parent;
            if (attributedSyntax is null)
            {
                return false;
            }

            // get the attribute we're looking for (//TODO: we need to decide on source vs library etc)
            var attributeTypeSymbol = model.Compilation.GetTypeByMetadataName(attributeFQN);
            if (attributeTypeSymbol is null)
            {
                return false;
            }

            var attributeCtorSymbol = model.GetSymbolInfo(node, cancellationToken).Symbol;
            if (attributeCtorSymbol is null || !attributeCtorSymbol.ContainingSymbol.Equals(attributeTypeSymbol, SymbolEqualityComparer.Default))
            {
                return false;
            }

            // we found a syntax that has the matching attribute applied. Now we need to get the data for the symbol itself
            var attributedSymbol = model.GetDeclaredSymbol(attributedSyntax);
            if (attributedSymbol is null)
            {
                return false;
            }

            //TODO: how to we handle module:, assembly:, return:, etc? Maybe we don't?
            var attributes = attributedSymbol.GetAttributes();
            foreach (var attribute in attributes)
            {
                // TODO: what happens for multi-attribute scenarios?
                if (attribute.AttributeClass?.Equals(attributeTypeSymbol, SymbolEqualityComparer.Default) == true)
                {
                    attributeData = attribute;
                    break;
                }
            }

            return attributeData is object;
        }
    }
}
