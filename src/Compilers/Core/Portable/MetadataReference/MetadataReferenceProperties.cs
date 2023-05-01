// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information about a metadata reference.
    /// </summary>
    public struct MetadataReferenceProperties : IEquatable<MetadataReferenceProperties>
    {
        private readonly MetadataImageKind _kind;
        private readonly ImmutableArray<string> _aliases;
        private readonly bool _embedInteropTypes;

        /// <summary>
        /// Default properties for a module reference.
        /// </summary>
        public static MetadataReferenceProperties Module => new MetadataReferenceProperties(MetadataImageKind.Module);

        /// <summary>
        /// Default properties for an assembly reference.
        /// </summary>
        public static MetadataReferenceProperties Assembly => new MetadataReferenceProperties(MetadataImageKind.Assembly);

        /// <summary>
        /// Initializes reference properties.
        /// </summary>
        /// <param name="kind">The image kind - assembly or module.</param>
        /// <param name="aliases">Assembly aliases. Can't be set for a module.</param>
        /// <param name="embedInteropTypes">True to embed interop types from the referenced assembly to the referencing compilation. Must be false for a module.</param>
        public MetadataReferenceProperties(MetadataImageKind kind = MetadataImageKind.Assembly, ImmutableArray<string> aliases = default, bool embedInteropTypes = false)
        {
            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (kind == MetadataImageKind.Module)
            {
                if (embedInteropTypes)
                {
                    throw new ArgumentException(CodeAnalysisResources.CannotEmbedInteropTypesFromModule, nameof(embedInteropTypes));
                }

                if (!aliases.IsDefaultOrEmpty)
                {
                    throw new ArgumentException(CodeAnalysisResources.CannotAliasModule, nameof(aliases));
                }
            }

            if (!aliases.IsDefaultOrEmpty)
            {
                foreach (var alias in aliases)
                {
                    if (!alias.IsValidClrTypeName())
                    {
                        throw new ArgumentException(CodeAnalysisResources.InvalidAlias, nameof(aliases));
                    }
                }
            }

            _kind = kind;
            _aliases = aliases;
            _embedInteropTypes = embedInteropTypes;
            HasRecursiveAliases = false;
        }

        internal MetadataReferenceProperties(MetadataImageKind kind, ImmutableArray<string> aliases, bool embedInteropTypes, bool hasRecursiveAliases)
            : this(kind, aliases, embedInteropTypes)
        {
            HasRecursiveAliases = hasRecursiveAliases;
        }

        /// <summary>
        /// Returns <see cref="MetadataReferenceProperties"/> with specified aliases.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <see cref="Kind"/> is <see cref="MetadataImageKind.Module"/>, as modules can't be aliased.
        /// </exception>
        public MetadataReferenceProperties WithAliases(IEnumerable<string> aliases)
        {
            return WithAliases(aliases.AsImmutableOrEmpty());
        }

        /// <summary>
        /// Returns <see cref="MetadataReferenceProperties"/> with specified aliases.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <see cref="Kind"/> is <see cref="MetadataImageKind.Module"/>, as modules can't be aliased.
        /// </exception>
        public MetadataReferenceProperties WithAliases(ImmutableArray<string> aliases)
        {
            return new MetadataReferenceProperties(_kind, aliases, _embedInteropTypes, HasRecursiveAliases);
        }

        /// <summary>
        /// Returns <see cref="MetadataReferenceProperties"/> with <see cref="EmbedInteropTypes"/> set to specified value.
        /// </summary>
        /// <exception cref="ArgumentException"><see cref="Kind"/> is <see cref="MetadataImageKind.Module"/>, as interop types can't be embedded from modules.</exception>
        public MetadataReferenceProperties WithEmbedInteropTypes(bool embedInteropTypes)
        {
            return new MetadataReferenceProperties(_kind, _aliases, embedInteropTypes, HasRecursiveAliases);
        }

        /// <summary>
        /// Returns <see cref="MetadataReferenceProperties"/> with <see cref="HasRecursiveAliases"/> set to specified value.
        /// </summary>
        internal MetadataReferenceProperties WithRecursiveAliases(bool value)
        {
            return new MetadataReferenceProperties(_kind, _aliases, _embedInteropTypes, value);
        }

        /// <summary>
        /// The image kind (assembly or module) the reference refers to.
        /// </summary>
        public MetadataImageKind Kind => _kind;

        /// <summary>
        /// Alias that represents a global declaration space.
        /// </summary>
        /// <remarks>
        /// Namespaces in references whose <see cref="Aliases"/> contain <see cref="GlobalAlias"/> are available in global declaration space.
        /// </remarks>
        public static string GlobalAlias => "global";

        /// <summary>
        /// Aliases for the metadata reference. Empty if the reference has no aliases.
        /// </summary>
        /// <remarks>
        /// In C# these aliases can be used in "extern alias" syntax to disambiguate type names. 
        /// </remarks>
        public ImmutableArray<string> Aliases
        {
            get
            {
                // Simplify usage - we can't avoid the _aliases field being null but we can always return empty array here:
                return _aliases.NullToEmpty();
            }
        }

        /// <summary>
        /// True if interop types defined in the referenced metadata should be embedded into the compilation referencing the metadata.
        /// </summary>
        public bool EmbedInteropTypes => _embedInteropTypes;

        /// <summary>
        /// True to apply <see cref="Aliases"/> recursively on the target assembly and on all its transitive dependencies.
        /// False to apply <see cref="Aliases"/> only on the target assembly.
        /// </summary>
        internal bool HasRecursiveAliases { get; private set; }

        public override bool Equals(object? obj)
        {
            return obj is MetadataReferenceProperties && Equals((MetadataReferenceProperties)obj);
        }

        public bool Equals(MetadataReferenceProperties other)
        {
            return Aliases.SequenceEqual(other.Aliases)
                && _embedInteropTypes == other._embedInteropTypes
                && _kind == other._kind
                && HasRecursiveAliases == other.HasRecursiveAliases;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Hash.CombineValues(Aliases), Hash.Combine(_embedInteropTypes, Hash.Combine(HasRecursiveAliases, ((int)_kind).GetHashCode())));
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
