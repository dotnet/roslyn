// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Helper structure to encapsulate/cache various information about metadata name of a type and 
    /// name resolution options.
    /// Also, allows us to stop using strings in the APIs that accept only metadata names, 
    /// making usage of them less bug prone.
    /// </summary>
    internal partial struct MetadataTypeName
    {
        /// <summary>
        /// Full metadata name of a type, includes namespace name for top level types.
        /// </summary>
        private string fullName;

        /// <summary>
        /// Namespace name for top level types.
        /// </summary>
        private string namespaceName;

        /// <summary>
        /// Name of the type without namespace prefix, but possibly with generic arity mangling present.
        /// </summary>
        private string typeName;

        /// <summary>
        /// Name of the type without namespace prefix and without generic arity mangling.
        /// </summary>
        private string unmangledTypeName;

        /// <summary>
        /// Arity of the type inferred based on the name mangling. It doesn't have to match the actual
        /// arity of the type.
        /// </summary>
        private short inferredArity;

        /// <summary>
        /// While resolving the name, consider only types with this arity.
        /// (-1) means allow any arity.
        /// If forcedArity >= 0 and useCLSCompliantNameArityEncoding, lookup may
        /// fail because forcedArity doesn't match the one encoded in the name.
        /// </summary>
        private short forcedArity;

        /// <summary>
        /// While resolving the name, consider only types following 
        /// CLS-compliant generic type names and arity encoding (ECMA-335, section 10.7.2).
        /// I.e. arity is inferred from the name and matching type must have the same
        /// emitted name and arity.
        /// TODO: PERF: Encode this field elsewhere to save 4 bytes
        /// </summary>
        private bool useCLSCompliantNameArityEncoding;

        /// <summary>
        /// Individual parts of qualified namespace name.
        /// </summary>
        private ImmutableArray<string> namespaceSegments;

        public static MetadataTypeName FromFullName(string fullName, bool useCLSCompliantNameArityEncoding = false, int forcedArity = -1)
        {
            Debug.Assert(fullName != null);
            Debug.Assert(forcedArity >= -1 && forcedArity < short.MaxValue);
            Debug.Assert(forcedArity == -1 ||
                         !useCLSCompliantNameArityEncoding ||
                         forcedArity == MetadataHelpers.InferTypeArityFromMetadataName(fullName),
                         "Conflicting metadata type name resolution options!");

            MetadataTypeName name;

            name.fullName = fullName;
            name.namespaceName = null;
            name.typeName = null;
            name.unmangledTypeName = null;
            name.inferredArity = -1;
            name.useCLSCompliantNameArityEncoding = useCLSCompliantNameArityEncoding;
            name.forcedArity = (short)forcedArity;
            name.namespaceSegments = default(ImmutableArray<string>);

            return name;
        }

        public static MetadataTypeName FromNamespaceAndTypeName(
            string namespaceName, string typeName,
            bool useCLSCompliantNameArityEncoding = false, int forcedArity = -1
        )
        {
            Debug.Assert(namespaceName != null);
            Debug.Assert(typeName != null);
            Debug.Assert(forcedArity >= -1 && forcedArity < short.MaxValue);
            Debug.Assert(!typeName.Contains(MetadataHelpers.DotDelimiterString));
            Debug.Assert(forcedArity == -1 ||
                         !useCLSCompliantNameArityEncoding ||
                         forcedArity == MetadataHelpers.InferTypeArityFromMetadataName(typeName),
                         "Conflicting metadata type name resolution options!");

            MetadataTypeName name;

            name.fullName = null;
            name.namespaceName = namespaceName;
            name.typeName = typeName;
            name.unmangledTypeName = null;
            name.inferredArity = -1;
            name.useCLSCompliantNameArityEncoding = useCLSCompliantNameArityEncoding;
            name.forcedArity = (short)forcedArity;
            name.namespaceSegments = default(ImmutableArray<string>);

            return name;
        }

        public static MetadataTypeName FromTypeName(string typeName, bool useCLSCompliantNameArityEncoding = false, int forcedArity = -1)
        {
            Debug.Assert(typeName != null);
            Debug.Assert(!typeName.Contains(MetadataHelpers.DotDelimiterString));
            Debug.Assert(forcedArity >= -1 && forcedArity < short.MaxValue);
            Debug.Assert(forcedArity == -1 ||
                         !useCLSCompliantNameArityEncoding ||
                         forcedArity == MetadataHelpers.InferTypeArityFromMetadataName(typeName),
                         "Conflicting metadata type name resolution options!");

            MetadataTypeName name;

            name.fullName = typeName;
            name.namespaceName = string.Empty;
            name.typeName = typeName;
            name.unmangledTypeName = null;
            name.inferredArity = -1;
            name.useCLSCompliantNameArityEncoding = useCLSCompliantNameArityEncoding;
            name.forcedArity = (short)forcedArity;
            name.namespaceSegments = ImmutableArray<string>.Empty;

            return name;
        }

        /// <summary>
        /// Full metadata name of a type, includes namespace name for top level types.
        /// </summary>
        public string FullName
        {
            get
            {
                if (fullName == null)
                {
                    Debug.Assert(namespaceName != null);
                    Debug.Assert(typeName != null);
                    fullName = MetadataHelpers.BuildQualifiedName(namespaceName, typeName);
                }

                return fullName;
            }
        }

        /// <summary>
        /// Namespace name for top level types, empty string for nested types.
        /// </summary>
        public string NamespaceName
        {
            get
            {
                if (namespaceName == null)
                {
                    Debug.Assert(fullName != null);
                    typeName = MetadataHelpers.SplitQualifiedName(fullName, out namespaceName);
                }

                return namespaceName;
            }
        }

        /// <summary>
        /// Name of the type without namespace prefix, but possibly with generic arity mangling present.
        /// </summary>
        public string TypeName
        {
            get
            {
                if (typeName == null)
                {
                    Debug.Assert(fullName != null);
                    typeName = MetadataHelpers.SplitQualifiedName(fullName, out namespaceName);
                }

                return typeName;
            }
        }

        /// <summary>
        /// Name of the type without namespace prefix and without generic arity mangling.
        /// </summary>
        public string UnmangledTypeName
        {
            get
            {
                if (unmangledTypeName == null)
                {
                    Debug.Assert(inferredArity == -1);
                    unmangledTypeName = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(TypeName, out inferredArity);
                }

                return unmangledTypeName;
            }
        }

        /// <summary>
        /// Arity of the type inferred based on the name mangling. It doesn't have to match the actual
        /// arity of the type.
        /// </summary>
        public int InferredArity
        {
            get
            {
                if (inferredArity == -1)
                {
                    Debug.Assert(unmangledTypeName == null);
                    unmangledTypeName = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(TypeName, out inferredArity);
                }

                return inferredArity;
            }
        }

        /// <summary>
        /// Does name include arity mangling suffix.
        /// </summary>
        public bool IsMangled
        {
            get
            {
                return InferredArity > 0;
            }
        }

        /// <summary>
        /// While resolving the name, consider only types following 
        /// CLS-compliant generic type names and arity encoding (ECMA-335, section 10.7.2).
        /// I.e. arity is inferred from the name and matching type must have the same
        /// emitted name and arity.
        /// </summary>
        public bool UseCLSCompliantNameArityEncoding
        {
            get
            {
                return useCLSCompliantNameArityEncoding;
            }
        }

        /// <summary>
        /// While resolving the name, consider only types with this arity.
        /// (-1) means allow any arity.
        /// If ForcedArity >= 0 and UseCLSCompliantNameArityEncoding, lookup may
        /// fail because ForcedArity doesn't match the one encoded in the name.
        /// </summary>
        public int ForcedArity
        {
            get
            {
                return forcedArity;
            }
        }

        /// <summary>
        /// Individual parts of qualified namespace name.
        /// </summary>
        public ImmutableArray<string> NamespaceSegments
        {
            get
            {
                if (namespaceSegments.IsDefault)
                {
                    namespaceSegments = MetadataHelpers.SplitQualifiedName(NamespaceName);
                }

                return namespaceSegments;
            }
        }

        public bool IsNull
        {
            get
            {
                return typeName == null && fullName == null;
            }
        }

        public override string ToString()
        {
            if (IsNull)
            {
                return "{Null}";
            }
            else
            {
                return String.Format("{{{0},{1},{2},{3}}}", NamespaceName, TypeName, UseCLSCompliantNameArityEncoding.ToString(), forcedArity.ToString());
            }
        }
    }
}