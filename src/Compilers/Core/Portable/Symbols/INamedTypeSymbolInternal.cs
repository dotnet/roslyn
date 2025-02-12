// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface INamedTypeSymbolInternal : ITypeSymbolInternal
    {
        /// <summary>
        /// For enum types, gets the underlying type. Returns null on all other
        /// kinds of types.
        /// </summary>
        INamedTypeSymbolInternal? EnumUnderlyingType { get; }

        ImmutableArray<ISymbolInternal> GetMembers();
        ImmutableArray<ISymbolInternal> GetMembers(string name);

        /// <summary>
        /// True if this type or some containing type has type parameters.
        /// </summary>
        bool IsGenericType { get; }

        internal static class Helpers
        {
            /// <summary>
            /// Returns True or False if we can determine whether the type is managed
            /// without looking at its fields and Unknown otherwise.
            /// Also returns whether or not the given type is generic.
            /// </summary>
            public static (ThreeState isManaged, bool hasGenerics) IsManagedTypeHelper(INamedTypeSymbolInternal type)
            {
                // To match dev10, we treat enums as their underlying types.
                if (type.TypeKind == TypeKind.Enum)
                {
                    Debug.Assert(type.EnumUnderlyingType is not null);
                    type = type.EnumUnderlyingType;
                }

                // Short-circuit common cases.
                switch (type.SpecialType)
                {
                    case SpecialType.System_Void:
                    case SpecialType.System_Boolean:
                    case SpecialType.System_Char:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_IntPtr:
                    case SpecialType.System_UIntPtr:
                    case SpecialType.System_ArgIterator:
                    case SpecialType.System_RuntimeArgumentHandle:
                        return (ThreeState.False, false);
                    case SpecialType.System_TypedReference:
                        return (ThreeState.True, false);
                    case SpecialType.None:
                    default:
                        // CONSIDER: could provide cases for other common special types.
                        break; // Proceed with additional checks.
                }

                bool hasGenerics = type.IsGenericType;
                switch (type.TypeKind)
                {
                    case TypeKind.Enum:
                        return (ThreeState.False, hasGenerics);
                    case TypeKind.Struct:
                        return (ThreeState.Unknown, hasGenerics);
                    default:
                        return (ThreeState.True, hasGenerics);
                }
            }
        }
    }
}
