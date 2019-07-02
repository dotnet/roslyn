// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class EETypeParameterSymbol : TypeParameterSymbol
    {
        private readonly Symbol _container;
        private readonly TypeParameterSymbol _sourceTypeParameter;
        private readonly int _ordinal;
        private readonly Func<TypeMap> _getTypeMap;
        private TypeMap _lazyTypeMap;

        public EETypeParameterSymbol(
            Symbol container,
            TypeParameterSymbol sourceTypeParameter,
            int ordinal,
            Func<TypeMap> getTypeMap)
        {
            Debug.Assert((container.Kind == SymbolKind.NamedType) || (container.Kind == SymbolKind.Method));
            _container = container;
            _sourceTypeParameter = sourceTypeParameter;
            _ordinal = ordinal;
            _getTypeMap = getTypeMap;
        }

        public override Symbol ContainingSymbol
        {
            get { return _container; }
        }

        public override string Name
        {
            get { return _sourceTypeParameter.Name; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public override bool HasConstructorConstraint
        {
            get { return _sourceTypeParameter.HasConstructorConstraint; }
        }

        public override bool HasReferenceTypeConstraint
        {
            get { return _sourceTypeParameter.HasReferenceTypeConstraint; }
        }

        internal override bool? ReferenceTypeConstraintIsNullable
        {
            get { return _sourceTypeParameter.ReferenceTypeConstraintIsNullable; }
        }

        public override bool HasNotNullConstraint
        {
            get { return _sourceTypeParameter.HasNotNullConstraint; }
        }

        internal override bool? IsNotNullable
        {
            get
            {
                if (_sourceTypeParameter.ConstraintTypesNoUseSiteDiagnostics.IsEmpty)
                {
                    return _sourceTypeParameter.IsNotNullable;
                }

                return CalculateIsNotNullable();
            }
        }

        public override bool HasValueTypeConstraint
        {
            get { return _sourceTypeParameter.HasValueTypeConstraint; }
        }

        public override bool HasUnmanagedTypeConstraint
        {
            get { return _sourceTypeParameter.HasUnmanagedTypeConstraint; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public override int Ordinal
        {
            get { return _ordinal; }
        }

        public override TypeParameterKind TypeParameterKind
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public override VarianceKind Variance
        {
            get { return _sourceTypeParameter.Variance; }
        }

        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
            _sourceTypeParameter.EnsureAllConstraintsAreResolved();
        }

        internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            var constraintTypes = _sourceTypeParameter.GetConstraintTypes(inProgress);

            if (constraintTypes.IsEmpty)
            {
                return constraintTypes;
            }

            // Remap constraints from sourceTypeParameter since constraints
            // may be defined in terms of other type parameters.
            return this.TypeMap.SubstituteTypes(constraintTypes);
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            var type = _sourceTypeParameter.GetDeducedBaseType(inProgress);
            return this.TypeMap.SubstituteType(type).AsTypeSymbolOnly();
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            var type = _sourceTypeParameter.GetEffectiveBaseClass(inProgress);
            return this.TypeMap.SubstituteNamedType(type);
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            var interfaces = _sourceTypeParameter.GetInterfaces(inProgress);
            return this.TypeMap.SubstituteNamedTypes(interfaces);
        }

        private TypeMap TypeMap
        {
            get
            {
                if (_lazyTypeMap == null)
                {
                    Interlocked.CompareExchange(ref _lazyTypeMap, _getTypeMap(), null);
                }
                return _lazyTypeMap;
            }
        }
    }
}
