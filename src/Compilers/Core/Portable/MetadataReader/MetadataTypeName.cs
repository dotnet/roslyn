// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    [NonCopyable]
    internal partial struct MetadataTypeName
    {
        /// <summary>
        /// Full metadata name of a type, includes namespace name for top level types.
        /// </summary>
        private string _fullName;

        /// <summary>
        /// Namespace name for top level types.
        /// </summary>
        private string _namespaceName;

        /// <summary>
        /// Name of the type without namespace prefix, but possibly with generic arity mangling present.
        /// </summary>
        private string _typeName;

        /// <summary>
        /// Name of the type without namespace prefix and without generic arity mangling.
        /// </summary>
        private string _unmangledTypeName;

        /// <summary>
        /// Arity of the type inferred based on the name mangling. It doesn't have to match the actual
        /// arity of the type.
        /// </summary>
        private short _inferredArity;

        /// <summary>
        /// While resolving the name, consider only types with this arity.
        /// (-1) means allow any arity.
        /// If forcedArity >= 0 and useCLSCompliantNameArityEncoding, lookup may
        /// fail because forcedArity doesn't match the one encoded in the name.
        /// </summary>
        private short _forcedArity;

        /// <summary>
        /// While resolving the name, consider only types following 
        /// CLS-compliant generic type names and arity encoding (ECMA-335, section 10.7.2).
        /// I.e. arity is inferred from the name and matching type must have the same
        /// emitted name and arity.
        /// TODO: PERF: Encode this field elsewhere to save 4 bytes
        /// </summary>
        private bool _useCLSCompliantNameArityEncoding;

        /// <summary>
        /// Individual parts of qualified namespace name.
        /// </summary>
        private ImmutableArray<string> _namespaceSegments;

        public static MetadataTypeName FromFullName(string fullName, bool useCLSCompliantNameArityEncoding = false, int forcedArity = -1)
        {
            Debug.Assert(fullName != null);
            Debug.Assert(forcedArity >= -1 && forcedArity < short.MaxValue);
            Debug.Assert(forcedArity == -1 ||
                         !useCLSCompliantNameArityEncoding ||
                         forcedArity == MetadataHelpers.InferTypeArityFromMetadataName(fullName),
                         "Conflicting metadata type name resolution options!");

            MetadataTypeName name;

            name._fullName = fullName;
            name._namespaceName = null;
            name._typeName = null;
            name._unmangledTypeName = null;
            name._inferredArity = -1;
            name._useCLSCompliantNameArityEncoding = useCLSCompliantNameArityEncoding;
            name._forcedArity = (short)forcedArity;
            name._namespaceSegments = default(ImmutableArray<string>);

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

            name._fullName = null;
            name._namespaceName = namespaceName;
            name._typeName = typeName;
            name._unmangledTypeName = null;
            name._inferredArity = -1;
            name._useCLSCompliantNameArityEncoding = useCLSCompliantNameArityEncoding;
            name._forcedArity = (short)forcedArity;
            name._namespaceSegments = default(ImmutableArray<string>);

            return name;
        }

        public static MetadataTypeName FromTypeName(string typeName, bool useCLSCompliantNameArityEncoding = false, int forcedArity = -1)
        {
            Debug.Assert(typeName != null);
            Debug.Assert(!typeName.Contains(MetadataHelpers.DotDelimiterString) || typeName.IndexOf(MetadataHelpers.MangledNameRegionStartChar) >= 0);
            Debug.Assert(forcedArity >= -1 && forcedArity < short.MaxValue);
            Debug.Assert(forcedArity == -1 ||
                         !useCLSCompliantNameArityEncoding ||
                         forcedArity == MetadataHelpers.InferTypeArityFromMetadataName(typeName),
                         "Conflicting metadata type name resolution options!");

            MetadataTypeName name;

            name._fullName = typeName;
            name._namespaceName = string.Empty;
            name._typeName = typeName;
            name._unmangledTypeName = null;
            name._inferredArity = -1;
            name._useCLSCompliantNameArityEncoding = useCLSCompliantNameArityEncoding;
            name._forcedArity = (short)forcedArity;
            name._namespaceSegments = ImmutableArray<string>.Empty;

            return name;
        }

        /// <summary>
        /// Full metadata name of a type, includes namespace name for top level types.
        /// </summary>
        public string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    Debug.Assert(_namespaceName != null);
                    Debug.Assert(_typeName != null);
                    _fullName = MetadataHelpers.BuildQualifiedName(_namespaceName, _typeName);
                }

                return _fullName;
            }
        }

        /// <summary>
        /// Namespace name for top level types, empty string for nested types.
        /// </summary>
        public string NamespaceName
        {
            get
            {
                if (_namespaceName == null)
                {
                    Debug.Assert(_fullName != null);
                    _typeName = MetadataHelpers.SplitQualifiedName(_fullName, out _namespaceName);
                }

                return _namespaceName;
            }
        }

        /// <summary>
        /// Name of the type without namespace prefix, but possibly with generic arity mangling present.
        /// </summary>
        public string TypeName
        {
            get
            {
                if (_typeName == null)
                {
                    Debug.Assert(_fullName != null);
                    _typeName = MetadataHelpers.SplitQualifiedName(_fullName, out _namespaceName);
                }

                return _typeName;
            }
        }

        /// <summary>
        /// Name of the type without namespace prefix and without generic arity mangling.
        /// </summary>
        public string UnmangledTypeName
        {
            get
            {
                if (_unmangledTypeName == null)
                {
                    Debug.Assert(_inferredArity == -1);
                    _unmangledTypeName = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(TypeName, out _inferredArity);
                }

                return _unmangledTypeName;
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
                if (_inferredArity == -1)
                {
                    Debug.Assert(_unmangledTypeName == null);
                    _unmangledTypeName = MetadataHelpers.InferTypeArityAndUnmangleMetadataName(TypeName, out _inferredArity);
                }

                return _inferredArity;
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
        public readonly bool UseCLSCompliantNameArityEncoding
        {
            get
            {
                return _useCLSCompliantNameArityEncoding;
            }
        }

        /// <summary>
        /// While resolving the name, consider only types with this arity.
        /// (-1) means allow any arity.
        /// If ForcedArity >= 0 and UseCLSCompliantNameArityEncoding, lookup may
        /// fail because ForcedArity doesn't match the one encoded in the name.
        /// </summary>
        public readonly int ForcedArity
        {
            get
            {
                return _forcedArity;
            }
        }

        /// <summary>
        /// Individual parts of qualified namespace name.
        /// </summary>
        public ImmutableArray<string> NamespaceSegments
        {
            get
            {
                if (_namespaceSegments.IsDefault)
                {
                    _namespaceSegments = MetadataHelpers.SplitQualifiedName(NamespaceName);
                }

                return _namespaceSegments;
            }
        }

        public readonly bool IsNull
        {
            get
            {
                return _typeName == null && _fullName == null;
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
                return String.Format("{{{0},{1},{2},{3}}}", NamespaceName, TypeName, UseCLSCompliantNameArityEncoding.ToString(), _forcedArity.ToString());
            }
        }
    }
}
