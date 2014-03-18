// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information about a metadata reference.
    /// </summary>
    [Serializable]
    public struct MetadataReferenceProperties : IEquatable<MetadataReferenceProperties>
    {
        private readonly MetadataImageKind kind;
        private readonly string alias;
        private readonly bool embedInteropTypes;

        /// <summary>
        /// Default properties for a module reference.
        /// </summary>
        public static readonly MetadataReferenceProperties Module = new MetadataReferenceProperties(MetadataImageKind.Module, null, false);

        /// <summary>
        /// Default properties for an assembly reference.
        /// </summary>
        public static readonly MetadataReferenceProperties Assembly = new MetadataReferenceProperties(MetadataImageKind.Assembly, null, false);

        /// <summary>
        /// Initializes reference properties.
        /// </summary>
        /// <param name="kind">The image kind - assembly or module.</param>
        /// <param name="alias">Assembly alias. Can't be set for a module.</param>
        /// <param name="embedInteropTypes">True to embed interop types from the referenced assembly to the referencing compilation. Must be false for a module.</param>
        public MetadataReferenceProperties(MetadataImageKind kind = MetadataImageKind.Assembly, string alias = null, bool embedInteropTypes = false)
        {
            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException("kind");
            }

            if (kind == MetadataImageKind.Module)
            {
                if (embedInteropTypes)
                {
                    throw new ArgumentException(CodeAnalysisResources.CannotEmbedInteropTypesFromModule, "embedInteropTypes");
                }

                if (alias != null)
                {
                    throw new ArgumentException(CodeAnalysisResources.CannotAliasModule, "alias");
                }
            }

            if (alias != null && !alias.IsValidClrTypeName())
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidAlias, "alias");
            }

            this.kind = kind;
            this.alias = alias;
            this.embedInteropTypes = embedInteropTypes;
        }

        /// <summary>
        /// Returns <see cref="MetadataReferenceProperties"/> with <see cref="P:Alias"/> set to specified value.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <see cref="P:Kind"/> is <see cref="MetadataImageKind.Module"/>, as modules can't be aliased.
        /// </exception>
        public MetadataReferenceProperties WithAlias(string alias)
        {
            return new MetadataReferenceProperties(this.kind, alias, this.EmbedInteropTypes);
        }

        /// <summary>
        /// Returns <see cref="MetadataReferenceProperties"/> with <see cref="P:EmbedInteropTypes"/> set to specified value.
        /// </summary>
        /// <exception cref="ArgumentException"><see cref="P:Kind"/> is <see cref="MetadataImageKind.Module"/>, as interop types can't be embedded from modules.</exception>
        public MetadataReferenceProperties WithEmbedInteropTypes(bool embedInteropTypes)
        {
            return new MetadataReferenceProperties(this.kind, this.alias, embedInteropTypes);
        }

        /// <summary>
        /// The image kind (assembly or module) the reference refers to.
        /// </summary>
        public MetadataImageKind Kind
        {
            get { return kind; }
        }

        /// <summary>
        /// Non-empty alias for the metadata reference, or null.
        /// </summary>
        /// <remarks>
        /// In C# this alias can be used in "extern alias" syntax to disambiguate type names. 
        /// </remarks>
        public string Alias
        {
            get { return alias; }
        }

        /// <summary>
        /// True if interop types defined in the referenced metadata should be embedded into the compilation referencing the metadata.
        /// </summary>
        public bool EmbedInteropTypes
        {
            get { return embedInteropTypes; }
        }

        public override bool Equals(object obj)
        {
            return obj is MetadataReferenceProperties && Equals((MetadataReferenceProperties)obj);
        }

        public bool Equals(MetadataReferenceProperties other)
        {
            return this.alias == other.alias
                && this.embedInteropTypes == other.embedInteropTypes
                && this.kind == other.kind;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.alias, Hash.Combine(this.embedInteropTypes, this.kind.GetHashCode()));
        }

        public static bool operator ==(MetadataReferenceProperties left, MetadataReferenceProperties right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MetadataReferenceProperties left, MetadataReferenceProperties right)
        {
            return !left.Equals(right);
        }
    }
}
