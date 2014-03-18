// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    partial struct MetadataTypeName
    {
        /// <summary>
        /// A digest of MetadataTypeName's fully qualified name which can be used as the key in a dictionary
        /// </summary>
        public struct Key : IEquatable<Key>
        {
            // PERF: We can work with either a fully qualified name (a single string) or
            // a 'split' name (namespace and type). If typeName is null, then a FQN is
            // stored in namespaceOrFullyQualifiedName
            private readonly string namespaceOrFullyQualifiedName;
            private readonly string typeName;
            private readonly byte useCLSCompliantNameArityEncoding; // Using byte instead of bool for denser packing and smaller structure size
            private readonly short forcedArity;

            internal Key(MetadataTypeName mdTypeName)
            {
                if (mdTypeName.IsNull)
                {
                    this.namespaceOrFullyQualifiedName = null;
                    this.typeName = null;
                    this.useCLSCompliantNameArityEncoding = 0;
                    this.forcedArity = 0;
                }
                else
                {
                    if (mdTypeName.fullName != null)
                    {
                        this.namespaceOrFullyQualifiedName = mdTypeName.fullName;
                        this.typeName = null;
                    }
                    else
                    {
                        Debug.Assert(mdTypeName.namespaceName != null);
                        Debug.Assert(mdTypeName.typeName != null);
                        this.namespaceOrFullyQualifiedName = mdTypeName.namespaceName;
                        this.typeName = mdTypeName.typeName;
                    }

                    this.useCLSCompliantNameArityEncoding = mdTypeName.UseCLSCompliantNameArityEncoding ? (byte)1 : (byte)0;
                    this.forcedArity = mdTypeName.forcedArity;
                }
            }

            private bool HasFullyQualifiedName
            {
                get
                {
                    return this.typeName == null;
                }
            }

            public bool Equals(Key other)
            {
                return useCLSCompliantNameArityEncoding == other.useCLSCompliantNameArityEncoding &&
                    forcedArity == other.forcedArity &&
                    EqualNames(ref other);
            }

            private bool EqualNames(ref Key other)
            {
                if (this.typeName == other.typeName)
                {
                    return this.namespaceOrFullyQualifiedName == other.namespaceOrFullyQualifiedName;
                }

                if (this.HasFullyQualifiedName)
                {
                    return MetadataHelpers.SplitNameEqualsFullyQualifiedName(other.namespaceOrFullyQualifiedName, other.typeName, this.namespaceOrFullyQualifiedName);
                }

                if (other.HasFullyQualifiedName)
                {
                    return MetadataHelpers.SplitNameEqualsFullyQualifiedName(this.namespaceOrFullyQualifiedName, this.typeName, other.namespaceOrFullyQualifiedName);
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
                       Hash.Combine(useCLSCompliantNameArityEncoding != 0,
                       forcedArity));
            }

            private int GetHashCodeName()
            {
                int hashCode = Hash.GetFNVHashCode(this.namespaceOrFullyQualifiedName);

                if (!this.HasFullyQualifiedName)
                {
                    hashCode = Hash.CombineFNVHash(hashCode, MetadataHelpers.DotDelimiter);
                    hashCode = Hash.CombineFNVHash(hashCode, this.typeName);
                }

                return hashCode;
            }
        }

        public Key ToKey()
        {
            return new Key(this);
        }
    }
}
