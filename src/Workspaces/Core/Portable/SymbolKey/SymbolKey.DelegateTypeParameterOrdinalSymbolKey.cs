// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        /// <summary>
        /// Symbol key to reference the type parameter of a delegate that is in the middle of being written out (or read
        /// in).  This can happen as delegates can be recursive in nature.  For example <c>delegate void D&lt;T&gt;(T d);</c>.
        /// We need a way to not infinitely recurse when we hit the reference to T inside the signature for D.
        /// </summary>
        private static class DelegateTypeParameterOrdinalSymbolKey
        {
            public static void Create(ITypeParameterSymbol symbol, int delegateIndex, SymbolKeyWriter visitor)
            {
                Contract.ThrowIfFalse(symbol.TypeParameterKind == TypeParameterKind.Type);
                visitor.WriteInteger(delegateIndex);
                visitor.WriteInteger(symbol.Ordinal);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var delegateIndex = reader.ReadInteger();
                var ordinal = reader.ReadInteger();
                var delegateType = reader.ResolveDelegate(delegateIndex);

                var typeParameter = delegateType?.TypeParameters[ordinal];
                if (typeParameter == null)
                {
                    failureReason = $"({nameof(DelegateTypeParameterOrdinalSymbolKey)} failed)";
                    return default;
                }

                failureReason = null;
                return new SymbolKeyResolution(typeParameter);
            }
        }
    }
}
