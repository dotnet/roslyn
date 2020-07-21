// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        internal override bool? ReferenceTypeConstraintIsNullable
        {
            get { return false; }
        }

        public override bool HasNotNullConstraint => false;

        internal override bool? IsNotNullable => null;

        public override bool HasValueTypeConstraint
        {
            get { return false; }
        }

        public override bool HasUnmanagedTypeConstraint
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

        internal override void EnsureAllConstraintsAreResolved(bool canIgnoreNullableContext)
        {
        }

        internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress, bool canIgnoreNullableContext)
        {
            return ImmutableArray<TypeWithAnnotations>.Empty;
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
