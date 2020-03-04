﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used to determine if two <see cref="AttributeData"/> instances are identical,
    /// i.e. they have the same attribute type, attribute constructor and have identical arguments.
    /// </summary>
    internal sealed class CommonAttributeDataComparer : IEqualityComparer<AttributeData>
    {
        public static CommonAttributeDataComparer Instance = new CommonAttributeDataComparer();
        private CommonAttributeDataComparer() { }

        public bool Equals(AttributeData attr1, AttributeData attr2)
        {
            Debug.Assert(attr1 != null);
            Debug.Assert(attr2 != null);

            return attr1.AttributeClass == attr2.AttributeClass &&
                attr1.AttributeConstructor == attr2.AttributeConstructor &&
                attr1.HasErrors == attr2.HasErrors &&
                attr1.IsConditionallyOmitted == attr2.IsConditionallyOmitted &&
                attr1.CommonConstructorArguments.SequenceEqual(attr2.CommonConstructorArguments) &&
                attr1.NamedArguments.SequenceEqual(attr2.NamedArguments);
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

        private static int GetHashCodeForNamedArguments(ImmutableArray<KeyValuePair<string, TypedConstant>> namedArguments)
        {
            int hash = 0;

            foreach (var arg in namedArguments)
            {
                if (arg.Key != null)
                {
                    hash = Hash.Combine(arg.Key.GetHashCode(), hash);
                }

                hash = Hash.Combine(arg.Value.GetHashCode(), hash);
            }

            return hash;
        }
    }
}
