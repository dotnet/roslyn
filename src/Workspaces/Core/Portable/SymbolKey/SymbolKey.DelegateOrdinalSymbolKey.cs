// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        /// <summary>
        /// Symbol key to reference a delegate that is in the middle of being written out (or read in).  This can happen
        /// as delegates can be recursive in nature.  For example <c>delegate void D(D d);</c>.  We need a way to not
        /// infinitely recurse when we hit the reference to D inside its own signature.
        /// </summary>
        private static class DelegateOrdinalSymbolKey
        {
            public static void Create(INamedTypeSymbol namedType, int delegateIndex, SymbolKeyWriter visitor)
            {
                Contract.ThrowIfFalse(namedType.TypeKind == TypeKind.Delegate);
                visitor.WriteInteger(delegateIndex);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var delegateIndex = reader.ReadInteger();
                var delegateType = reader.ResolveDelegate(delegateIndex);

                if (delegateType == null)
                {
                    failureReason = $"({nameof(DelegateOrdinalSymbolKey)} failed)";
                    return default;
                }

                failureReason = null;
                return new SymbolKeyResolution(delegateType);
            }
        }
    }
}
