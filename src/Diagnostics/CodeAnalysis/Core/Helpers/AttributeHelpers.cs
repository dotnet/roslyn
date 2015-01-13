using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Analyzers
{
    internal static class AttributeHelpers
    {
        internal static IEnumerable<AttributeData> GetApplicableAttributes(INamedTypeSymbol type)
        {
            var attributes = new List<AttributeData>();

            while (type != null)
            {
                attributes.AddRange(type.GetAttributes());

                type = type.BaseType;
            }

            return attributes;
        }

        internal static bool DerivesFrom(INamedTypeSymbol symbol, INamedTypeSymbol candidateBaseType)
        {
            while (symbol != null)
            {
                if (symbol.Equals(candidateBaseType))
                {
                    return true;
                }

                symbol = symbol.BaseType;
            }

            return false;
        }
    }
}
