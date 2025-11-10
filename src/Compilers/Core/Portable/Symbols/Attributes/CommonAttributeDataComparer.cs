// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used to determine if two <see cref="AttributeData"/> instances are identical,
    /// i.e. they have the same attribute type, attribute constructor and have identical arguments.
    /// </summary>
    internal sealed class CommonAttributeDataComparer : IEqualityComparer<AttributeData>
    {
        public static CommonAttributeDataComparer Instance = new CommonAttributeDataComparer(considerNamedArgumentsOrder: true);
        public static CommonAttributeDataComparer InstanceIgnoringNamedArgumentOrder = new CommonAttributeDataComparer(considerNamedArgumentsOrder: false);

        private readonly bool _considerNamedArgumentsOrder;
        private CommonAttributeDataComparer(bool considerNamedArgumentsOrder)
        {
            this._considerNamedArgumentsOrder = considerNamedArgumentsOrder;
        }

        public bool Equals(AttributeData attr1, AttributeData attr2)
        {
            Debug.Assert(attr1 != null);
            Debug.Assert(attr2 != null);

            var typedConstantComparer = TypedConstantComparer.IgnoreAll;
            var namedArgumentComparer = NamedArgumentComparer.IgnoreAll;

            bool equals = attr1.AttributeClass == attr2.AttributeClass &&
                attr1.AttributeConstructor == attr2.AttributeConstructor &&
                attr1.HasErrors == attr2.HasErrors &&
                attr1.IsConditionallyOmitted == attr2.IsConditionallyOmitted &&
                attr1.CommonConstructorArguments.SequenceEqual(attr2.CommonConstructorArguments, typedConstantComparer) &&
                (_considerNamedArgumentsOrder ? attr1.NamedArguments.SequenceEqual(attr2.NamedArguments, namedArgumentComparer) : attr1.NamedArguments.SetEquals(attr2.NamedArguments, namedArgumentComparer));

            Debug.Assert(!equals || GetHashCode(attr1) == GetHashCode(attr2), "If attributes are equal for some options, their hashes must be equal for those same options.");
            return equals;
        }

        public int GetHashCode(AttributeData attr)
        {
            Debug.Assert(attr != null);

            int hash = attr.AttributeClass?.GetHashCode() ?? 0;
            hash = attr.AttributeConstructor != null ? Hash.Combine(attr.AttributeConstructor.GetHashCode(), hash) : hash;
            hash = Hash.Combine(attr.HasErrors, hash);
            hash = Hash.Combine(attr.IsConditionallyOmitted, hash);
            hash = Hash.Combine(GetHashCodeForConstructorArguments(attr.CommonConstructorArguments), hash);
            hash = Hash.Combine(GetHashCodeForNamedArguments(attr.NamedArguments), hash);

            return hash;
        }

        private static int GetHashCodeForConstructorArguments(ImmutableArray<TypedConstant> constructorArguments)
        {
            int hash = 0;

            foreach (var arg in constructorArguments)
            {
                hash = Hash.Combine(arg.GetHashCode(), hash);
            }

            return hash;
        }

        private int GetHashCodeForNamedArguments(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
        {
            int hash = 0;

            foreach (var arg in namedArguments)
            {
                if (arg.Key != null)
                {
                    hash = hashCombine(arg.Key.GetHashCode(), hash, _considerNamedArgumentsOrder);
                }

                hash = hashCombine(arg.Value.GetHashCode(), hash, _considerNamedArgumentsOrder);
            }

            return hash;

            static int hashCombine(int value, int currentHash, bool considerNamedArgumentsOrder)
            {
                // Prefer Hash.Combine for better distribution, unless we are ignoring the order of named arguments (then we use XOR which is commutative).
                if (!considerNamedArgumentsOrder)
                {
                    return value ^ currentHash;
                }

                return Hash.Combine(value, currentHash);
            }
        }

        private class TypedConstantComparer : IEqualityComparer<TypedConstant>
        {
            public static readonly TypedConstantComparer IgnoreAll = new TypedConstantComparer();

            private TypedConstantComparer()
            {
            }

            public bool Equals(TypedConstant x, TypedConstant y)
            {
                bool result = equals(x, y);
                Debug.Assert(!result || GetHashCode(x) == GetHashCode(y), "If TypedConstants are equal, their hashes must be equal.");
                return result;

                static bool equals(TypedConstant x, TypedConstant y)
                {
                    if (x.Kind == TypedConstantKind.Type && y.Kind == TypedConstantKind.Type)
                    {
                        return x.ValueInternal is ISymbolInternal xType && y.ValueInternal is ISymbolInternal yType && xType.Equals(yType, TypeCompareKind.AllIgnoreOptions);
                    }

                    return x.Equals(y);
                }
            }

            public int GetHashCode(TypedConstant obj)
            {
                return obj.GetHashCode();
            }
        }

        private class NamedArgumentComparer : IEqualityComparer<KeyValuePair<string, TypedConstant>>
        {
            public static readonly NamedArgumentComparer IgnoreAll = new NamedArgumentComparer();

            private NamedArgumentComparer()
            {
            }

            public bool Equals(KeyValuePair<string, TypedConstant> pair1, KeyValuePair<string, TypedConstant> pair2)
            {
                bool equals = pair1.Key == pair2.Key && TypedConstantComparer.IgnoreAll.Equals(pair1.Value, pair2.Value);
                Debug.Assert(!equals || GetHashCode(pair1) == GetHashCode(pair2), "If named arguments are equal, their hashes must be equal.");
                return equals;
            }

            public int GetHashCode(KeyValuePair<string, TypedConstant> pair)
            {
                return pair.GetHashCode();
            }
        }
    }
}
