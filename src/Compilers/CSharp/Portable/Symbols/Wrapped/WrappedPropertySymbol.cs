// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a property that is based on another property.
    /// When inheriting from this class, one shouldn't assume that 
    /// the default behavior it has is appropriate for every case.
    /// That behavior should be carefully reviewed and derived type
    /// should override behavior as appropriate.
    /// </summary>
    internal abstract class WrappedPropertySymbol : PropertySymbol
    {
        /// <summary>
        /// The underlying PropertySymbol.
        /// </summary>
        protected readonly PropertySymbol _underlyingProperty;

        public WrappedPropertySymbol(PropertySymbol underlyingProperty)
        {
            Debug.Assert((object)underlyingProperty != null);
            _underlyingProperty = underlyingProperty;
        }

        public PropertySymbol UnderlyingProperty
        {
            get
            {
                return _underlyingProperty;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingProperty.IsImplicitlyDeclared; }
        }

        public override RefKind RefKind
        {
            get
            {
                return _underlyingProperty.RefKind;
            }
        }

        public override bool IsIndexer
        {
            get
            {
                return _underlyingProperty.IsIndexer;
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                return _underlyingProperty.CallingConvention;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingProperty.Name;
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                return _underlyingProperty.HasSpecialName;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingProperty.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingProperty.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingProperty.DeclaringSyntaxReferences;
            }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                return _underlyingProperty.DeclaredAccessibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return _underlyingProperty.IsStatic;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return _underlyingProperty.IsVirtual;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return _underlyingProperty.IsOverride;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _underlyingProperty.IsAbstract;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return _underlyingProperty.IsSealed;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return _underlyingProperty.IsExtern;
            }
        }

        internal sealed override bool IsRequired => _underlyingProperty.IsRequired;

        internal sealed override bool HasUnscopedRefAttribute => _underlyingProperty.HasUnscopedRefAttribute;

        internal sealed override bool IsCallerUnsafe => _underlyingProperty.IsCallerUnsafe;

        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                return _underlyingProperty.ObsoleteAttributeData;
            }
        }

        public override string MetadataName
        {
            get
            {
                return _underlyingProperty.MetadataName;
            }
        }

        internal override bool HasRuntimeSpecialName
        {
            get
            {
                return _underlyingProperty.HasRuntimeSpecialName;
            }
        }

        internal override int TryGetOverloadResolutionPriority() => _underlyingProperty.OverloadResolutionPriority;
    }
}
