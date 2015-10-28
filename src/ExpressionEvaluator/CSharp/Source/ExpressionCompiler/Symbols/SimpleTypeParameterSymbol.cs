// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    /// <summary>
    /// A simple type parameter with no constraints.
    /// </summary>
    internal sealed class SimpleTypeParameterSymbol : TypeParameterSymbol
    {
        private readonly Symbol _container;
        private readonly int _ordinal;
        private readonly string _name;

        public SimpleTypeParameterSymbol(Symbol container, int ordinal, string name)
        {
            _container = container;
            _ordinal = ordinal;
            _name = name;
        }

        public override string Name
        {
            get { return _name; }
        }

        public override int Ordinal
        {
            get { return _ordinal; }
        }

        public override TypeParameterKind TypeParameterKind
        {
            get { return TypeParameterKind.Type; }
        }

        public override bool HasConstructorConstraint
        {
            get { return false; }
        }

        public override bool HasReferenceTypeConstraint
        {
            get { return false; }
        }

        public override bool HasValueTypeConstraint
        {
            get { return false; }
        }

        public override VarianceKind Variance
        {
            get { return VarianceKind.None; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _container; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { throw ExceptionUtilities.Unreachable; }
        }

        internal override void EnsureAllConstraintsAreResolved()
        {
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            return ImmutableArray<TypeSymbolWithAnnotations>.Empty;
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
