// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Represents a method of a tuple type (such as (int, byte).ToString())
    /// that is backed by a method within the tuple underlying type.
    /// </summary>
    internal sealed class TupleMethodSymbol : WrappedMethodSymbol
    {
        private readonly TupleTypeSymbol _containingType;
        private readonly MethodSymbol _underlyingMethod;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        public TupleMethodSymbol(TupleTypeSymbol container, MethodSymbol underlyingMethod)
        {
            Debug.Assert(underlyingMethod.ConstructedFrom == (object)underlyingMethod);
            _containingType = container;

            TypeMap.Empty.WithAlphaRename(underlyingMethod, this, out _typeParameters);
            _underlyingMethod = underlyingMethod.ConstructIfGeneric(TypeArguments);
        }

        public override bool IsTupleMethod
        {
            get
            {
                return true;
            }
        }

        public override MethodSymbol TupleUnderlyingMethod
        {
            get
            {
                return _underlyingMethod.ConstructedFrom;
            }
        }

        public override MethodSymbol UnderlyingMethod
        {
            get
            {
                return _underlyingMethod;
            }
        }

        public override Symbol AssociatedSymbol
        {
            get
            {
                return _containingType.GetTupleMemberSymbolForUnderlyingMember(_underlyingMethod.ConstructedFrom.AssociatedSymbol);
            }
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return _underlyingMethod.ConstructedFrom.ExplicitInterfaceImplementations;
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
            ImmutableArray<ParameterSymbol> underlying = _underlyingMethod.Parameters;
            var builder = ArrayBuilder<ParameterSymbol>.GetInstance(underlying.Length);

            foreach (var parameter in underlying)
            {
                builder.Add(new TupleParameterSymbol(this, parameter));
            }

            return builder.ToImmutableAndFree();
        }

        public override bool ReturnsVoid
        {
            get
            {
                return _underlyingMethod.ReturnsVoid;
            }
        }

        public override TypeSymbolWithAnnotations ReturnType
        {
            get
            {
                return _underlyingMethod.ReturnType;
            }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return _underlyingMethod.RefCustomModifiers;
            }
        }

        public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments
        {
            get
            {
                return GetTypeParametersAsTypeArguments();
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return _typeParameters;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return _underlyingMethod.OriginalDefinition.IsExplicitInterfaceImplementation;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingMethod.GetAttributes();
        }

        public override ImmutableArray<CSharpAttributeData> GetReturnTypeAttributes()
        {
            return _underlyingMethod.GetReturnTypeAttributes();
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override DiagnosticInfo GetUseSiteDiagnostic()
        {
            DiagnosticInfo result = base.GetUseSiteDiagnostic();
            MergeUseSiteDiagnostics(ref result, _underlyingMethod.GetUseSiteDiagnostic());
            return result;
        }

        public override int GetHashCode()
        {
            return _underlyingMethod.ConstructedFrom.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TupleMethodSymbol);
        }

        public bool Equals(TupleMethodSymbol other)
        {
            if ((object)other == this)
            {
                return true;
            }

            return (object)other != null && TypeSymbol.Equals(_containingType, other._containingType, TypeCompareKind.ConsiderEverything2) && _underlyingMethod.ConstructedFrom == other._underlyingMethod.ConstructedFrom;
        }
    }
}
