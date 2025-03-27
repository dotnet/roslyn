// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Reference to another C# or VB compilation.
    /// </summary>
    public abstract class CompilationReference : MetadataReference, IEquatable<CompilationReference>
    {
        public Compilation Compilation { get { return CompilationCore; } }
        internal abstract Compilation CompilationCore { get; }

        internal CompilationReference(MetadataReferenceProperties properties)
            : base(properties)
        {
            Debug.Assert(properties.Kind != MetadataImageKind.Module);
        }

        internal static MetadataReferenceProperties GetProperties(Compilation compilation, ImmutableArray<string> aliases, bool embedInteropTypes)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (compilation.IsSubmission)
            {
                throw new NotSupportedException(CodeAnalysisResources.CannotCreateReferenceToSubmission);
            }

            if (compilation.Options.OutputKind == OutputKind.NetModule)
            {
                throw new NotSupportedException(CodeAnalysisResources.CannotCreateReferenceToModule);
            }

            return new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases, embedInteropTypes);
        }

        /// <summary>
        /// Returns an instance of the reference with specified aliases.
        /// </summary>
        /// <param name="aliases">The new aliases for the reference.</param>
        /// <exception cref="ArgumentException">Alias is invalid for the metadata kind.</exception> 
        public new CompilationReference WithAliases(IEnumerable<string> aliases)
        {
            return this.WithAliases(ImmutableArray.CreateRange(aliases));
        }

        /// <summary>
        /// Returns an instance of the reference with specified aliases.
        /// </summary>
        /// <param name="aliases">The new aliases for the reference.</param>
        /// <exception cref="ArgumentException">Alias is invalid for the metadata kind.</exception> 
        public new CompilationReference WithAliases(ImmutableArray<string> aliases)
        {
            return WithProperties(Properties.WithAliases(aliases));
        }

        /// <summary>
        /// Returns an instance of the reference with specified interop types embedding.
        /// </summary>
        /// <param name="value">The new value for <see cref="MetadataReferenceProperties.EmbedInteropTypes"/>.</param>
        /// <exception cref="ArgumentException">Interop types can't be embedded from modules.</exception> 
        public new CompilationReference WithEmbedInteropTypes(bool value)
        {
            return WithProperties(Properties.WithEmbedInteropTypes(value));
        }

        /// <summary>
        /// Returns an instance of the reference with specified properties, or this instance if properties haven't changed.
        /// </summary>
        /// <param name="properties">The new properties for the reference.</param>
        /// <exception cref="ArgumentException">Specified values not valid for this reference.</exception> 
        public new CompilationReference WithProperties(MetadataReferenceProperties properties)
        {
            if (properties == this.Properties)
            {
                return this;
            }

            if (properties.Kind == MetadataImageKind.Module)
            {
                throw new ArgumentException(CodeAnalysisResources.CannotCreateReferenceToModule);
            }

            return WithPropertiesImpl(properties);
        }

        internal sealed override MetadataReference WithPropertiesImplReturningMetadataReference(MetadataReferenceProperties properties)
        {
            if (properties.Kind == MetadataImageKind.Module)
            {
                throw new NotSupportedException(CodeAnalysisResources.CannotCreateReferenceToModule);
            }

            return WithPropertiesImpl(properties);
        }

        internal abstract CompilationReference WithPropertiesImpl(MetadataReferenceProperties properties);

        public override string? Display
        {
            get
            {
                return Compilation.AssemblyName;
            }
        }

        public bool Equals(CompilationReference? other)
        {
            if (other == null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            // MetadataProperty implements value equality
            return object.Equals(this.Compilation, other.Compilation) && object.Equals(this.Properties, other.Properties);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as CompilationReference);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Compilation.GetHashCode(), this.Properties.GetHashCode());
        }
    }
}
