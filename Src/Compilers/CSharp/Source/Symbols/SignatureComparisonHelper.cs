using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    /// <summary>
    /// Helper methods shared by MethodSignatureComparer and PropertySignatureComparer.
    /// </summary>
    internal static class SignatureComparisonHelper
    {
        internal static bool HaveSameParameterTypes(ReadOnlyArray<ParameterSymbol> params1, TypeMap typeMap1, ReadOnlyArray<ParameterSymbol> params2, TypeMap typeMap2, bool considerRefOutDifference, bool considerCustomModifiers)
        {
            Contract.ThrowIfNotEquals(params1.Count, params2.Count);

            var numParams = params1.Count;

            for (int i = 0; i < numParams; i++)
            {
                var param1 = params1[i];
                var param2 = params2[i];

                var type1 = SubstituteType(typeMap1, param1.Type);
                var type2 = SubstituteType(typeMap2, param2.Type);

                //the runtime compares custom modifiers using (effectively) SequenceEqual
                if (considerCustomModifiers)
                {
                    if (!type1.IsSameType(type2, ignoreDynamic: true) || !param1.CustomModifiers.SequenceEqual(param2.CustomModifiers))
                    {
                        return false;
                    }
                }
                else if (!type1.IsSameType(type2, ignoreCustomModifiers: true, ignoreDynamic: true))
                {
                    return false;
                }

                var refKind1 = param1.RefKind;
                var refKind2 = param2.RefKind;

                // Metadata signatures don't distinguish ref/out, but C# does - even when comparing metadata method signatures.
                if (considerRefOutDifference)
                {
                    if (refKind1 != refKind2)
                    {
                        return false;
                    }
                }
                else
                {
                    if ((refKind1 == RefKind.None) != (refKind2 == RefKind.None))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        internal static TypeSymbol SubstituteType(TypeMap typeMap, TypeSymbol typeSymbol)
        {
            return typeMap == null ? typeSymbol : typeMap.SubstituteType(typeSymbol);
        }
    }
}
