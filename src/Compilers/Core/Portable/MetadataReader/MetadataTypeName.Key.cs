// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial struct MetadataTypeName
    {
        /// <summary>
        /// A digest of MetadataTypeName's fully qualified name which can be used as the key in a dictionary
        /// </summary>
        public readonly struct Key : IEquatable<Key>
        {
            // PERF: We can work with either a fully qualified name (a single string) or
            // a 'split' name (namespace and type). If typeName is null, then a FQN is
            // stored in namespaceOrFullyQualifiedName
            private readonly string _namespaceOrFullyQualifiedName;
            private readonly string _typeName;
            private readonly byte _useCLSCompliantNameArityEncoding; // Using byte instead of bool for denser packing and smaller structure size
            private readonly short _forcedArity;

            internal Key(in MetadataTypeName mdTypeName)
            {
                if (mdTypeName.IsNull)
                {
                    _namespaceOrFullyQualifiedName = null;
                    _typeName = null;
                    _useCLSCompliantNameArityEncoding = 0;
                    _forcedArity = 0;
                }
                else
                {
                    if (mdTypeName._fullName != null)
                    {
                        _namespaceOrFullyQualifiedName = mdTypeName._fullName;
                        _typeName = null;
                    }
                    else
                    {
                        Debug.Assert(mdTypeName._namespaceName != null);
                        Debug.Assert(mdTypeName._typeName != null);
                        _namespaceOrFullyQualifiedName = mdTypeName._namespaceName;
                        _typeName = mdTypeName._typeName;
                    }

                    _useCLSCompliantNameArityEncoding = mdTypeName.UseCLSCompliantNameArityEncoding ? (byte)1 : (byte)0;
                    _forcedArity = mdTypeName._forcedArity;
                }
            }

            private bool HasFullyQualifiedName
            {
                get
                {
                    return _typeName == null;
                }
            }

            public bool Equals(Key other)
            {
                return _useCLSCompliantNameArityEncoding == other._useCLSCompliantNameArityEncoding &&
                    _forcedArity == other._forcedArity &&
                    EqualNames(ref other);
            }

            private bool EqualNames(ref Key other)
            {
                if (_typeName == other._typeName)
                {
                    return _namespaceOrFullyQualifiedName == other._namespaceOrFullyQualifiedName;
                }

                if (this.HasFullyQualifiedName)
                {
                    return MetadataHelpers.SplitNameEqualsFullyQualifiedName(other._namespaceOrFullyQualifiedName, other._typeName, _namespaceOrFullyQualifiedName);
                }

                if (other.HasFullyQualifiedName)
                {
                    return MetadataHelpers.SplitNameEqualsFullyQualifiedName(_namespaceOrFullyQualifiedName, _typeName, other._namespaceOrFullyQualifiedName);
                }

                return false;
            }

            public override bool Equals(object obj)
            {
                return obj is Key && this.Equals((Key)obj);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(GetHashCodeName(),
                       Hash.Combine(_useCLSCompliantNameArityEncoding != 0,
                       _forcedArity));
            }

            private int GetHashCodeName()
            {
                int hashCode = Hash.GetFNVHashCode(_namespaceOrFullyQualifiedName);

                if (!this.HasFullyQualifiedName)
                {
                    hashCode = Hash.CombineFNVHash(hashCode, MetadataHelpers.DotDelimiter);
                    hashCode = Hash.CombineFNVHash(hashCode, _typeName);
                }

                return hashCode;
            }
        }

        public readonly Key ToKey()
        {
            return new Key(in this);
        }
    }
}
