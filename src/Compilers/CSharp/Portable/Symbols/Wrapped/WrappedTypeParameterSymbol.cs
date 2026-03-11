// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a type parameter that is based on another type parameter.
    /// When inheriting from this class, one shouldn't assume that 
    /// the default behavior it has is appropriate for every case.
    /// That behavior should be carefully reviewed and derived type
    /// should override behavior as appropriate.
    /// </summary>
    internal abstract class WrappedTypeParameterSymbol
        : TypeParameterSymbol
    {
        /// <summary>
        /// The underlying TypeParameterSymbol, cannot be another RetargetingTypeParameterSymbol.
        /// </summary>
        protected readonly TypeParameterSymbol _underlyingTypeParameter;

        public WrappedTypeParameterSymbol(TypeParameterSymbol underlyingTypeParameter)
        {
            Debug.Assert((object)underlyingTypeParameter != null);
            _underlyingTypeParameter = underlyingTypeParameter;
        }

        public TypeParameterSymbol UnderlyingTypeParameter
        {
            get
            {
                return _underlyingTypeParameter;
            }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return _underlyingTypeParameter.IsImplicitlyDeclared; }
        }

        public override TypeParameterKind TypeParameterKind
        {
            get
            {
                return _underlyingTypeParameter.TypeParameterKind;
            }
        }

        public override int Ordinal
        {
            get
            {
                return _underlyingTypeParameter.Ordinal;
            }
        }

        public override bool HasConstructorConstraint
        {
            get
            {
                return _underlyingTypeParameter.HasConstructorConstraint;
            }
        }

        public override bool HasReferenceTypeConstraint
        {
            get
            {
                return _underlyingTypeParameter.HasReferenceTypeConstraint;
            }
        }

        public override bool IsReferenceTypeFromConstraintTypes
        {
            get
            {
                return _underlyingTypeParameter.IsReferenceTypeFromConstraintTypes || CalculateIsReferenceTypeFromConstraintTypes(ConstraintTypesNoUseSiteDiagnostics);
            }
        }

        internal override bool? ReferenceTypeConstraintIsNullable
        {
            get
            {
                return _underlyingTypeParameter.ReferenceTypeConstraintIsNullable;
            }
        }

        public override bool HasNotNullConstraint
        {
            get
            {
                return _underlyingTypeParameter.HasNotNullConstraint;
            }
        }

        public override bool HasUnmanagedTypeConstraint
        {
            get
            {
                return _underlyingTypeParameter.HasUnmanagedTypeConstraint;
            }
        }

        public override bool HasValueTypeConstraint
        {
            get
            {
                return _underlyingTypeParameter.HasValueTypeConstraint;
            }
        }

        public override bool AllowsRefLikeType
        {
            get
            {
                return _underlyingTypeParameter.AllowsRefLikeType;
            }
        }

        public override bool IsValueTypeFromConstraintTypes
        {
            get
            {
                return _underlyingTypeParameter.IsValueTypeFromConstraintTypes || CalculateIsValueTypeFromConstraintTypes(ConstraintTypesNoUseSiteDiagnostics);
            }
        }

        public override VarianceKind Variance
        {
            get
            {
                return _underlyingTypeParameter.Variance;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return _underlyingTypeParameter.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return _underlyingTypeParameter.DeclaringSyntaxReferences;
            }
        }

        public override string Name
        {
            get
            {
                return _underlyingTypeParameter.Name;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _underlyingTypeParameter.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            _underlyingTypeParameter.EnsureAllConstraintsAreResolved();
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingTypeParameter.GetAttributes();
        }

        internal abstract override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes);
    }
}
