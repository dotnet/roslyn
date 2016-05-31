﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a property of a tuple type (such as (int, byte).SomeProperty)
    /// that is backed by a property within the tuple underlying type.
    /// </summary>
    internal sealed class TuplePropertySymbol : WrappedPropertySymbol
    {
        private readonly TupleTypeSymbol _containingType;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        public TuplePropertySymbol(TupleTypeSymbol container, PropertySymbol underlyingProperty)
            : base(underlyingProperty)
        {
            _containingType = container;
        }

        public override bool IsTupleProperty
        {
            get
            {
                return true;
            }
        }

        public override PropertySymbol TupleUnderlyingProperty
        {
            get
            {
                return _underlyingProperty;
            }
        }

        public override TypeSymbol Type
        {
            get
            {
                return _underlyingProperty.Type;
            }
        }

        public override ImmutableArray<CustomModifier> TypeCustomModifiers
        {
            get
            {
                return _underlyingProperty.TypeCustomModifiers;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    InterlockedOperations.Initialize(ref _lazyParameters, CreateParameters());
                }

                return _lazyParameters;
            }
        }

        private ImmutableArray<ParameterSymbol> CreateParameters()
        {
            ImmutableArray<ParameterSymbol> underlying = _underlyingProperty.Parameters;
            var builder = ArrayBuilder<ParameterSymbol>.GetInstance(underlying.Length);

            foreach (var parameter in underlying)
            {
                builder.Add(new TupleParameterSymbol(this, parameter));
            }

            return builder.ToImmutableAndFree();
        }

        public override MethodSymbol GetMethod
        {
            get
            {
                return _containingType.GetTupleMemberSymbolForUnderlyingMember(_underlyingProperty.GetMethod);
            }
        }

        public override MethodSymbol SetMethod
        {
            get
            {
                return _containingType.GetTupleMemberSymbolForUnderlyingMember(_underlyingProperty.SetMethod);
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return _underlyingProperty.IsExplicitInterfaceImplementation;
            }
        }

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return _underlyingProperty.ExplicitInterfaceImplementations;
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        internal override bool MustCallMethodsDirectly
        {
            get
            {
                return _underlyingProperty.MustCallMethodsDirectly;
            }
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            DiagnosticInfo result = base.GetUseSiteDiagnostic();
            MergeUseSiteDiagnostics(ref result, _underlyingProperty.GetUseSiteDiagnostic());
            return result;
        }

        public override int GetHashCode()
        {
            return _underlyingProperty.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TuplePropertySymbol);
        }

        public bool Equals(TuplePropertySymbol other)
        {
            if ((object)other == this)
            {
                return true;
            }

            return (object)other != null && _containingType == other._containingType && _underlyingProperty == other._underlyingProperty;
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingProperty.GetAttributes();
        }
    }
}