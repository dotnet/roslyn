// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

internal sealed class SynthesizedCollectionBuilderProjectedMethodSymbol(
    MethodSymbol originalCollectionBuilderMethod) : WrappedMethodSymbol
{
    private readonly MethodSymbol _originalCollectionBuilderMethod = originalCollectionBuilderMethod;

    private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;
    private ImmutableArray<ParameterSymbol> _lazyParameters;

    public override MethodSymbol UnderlyingMethod => _originalCollectionBuilderMethod;

    public override TypeWithAnnotations ReturnTypeWithAnnotations => throw new System.NotImplementedException();

    public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => throw new System.NotImplementedException();

    public override ImmutableArray<TypeParameterSymbol> TypeParameters
    {
        get
        {
            if (_lazyTypeParameters.IsDefault)
            {
                var parameters = _originalCollectionBuilderMethod.TypeParameters.SelectAsArray(
                    static (p, @this) => (TypeParameterSymbol)new SynthesizedCollectionBuilderProjectedTypeParameterSymbol(@this, p), this);
                ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameters, parameters);
            }
            return _lazyTypeParameters;
        }
    }

    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (_lazyParameters.IsDefault)
            {
                var parameters = _originalCollectionBuilderMethod.Parameters
                    .Take(_originalCollectionBuilderMethod.Parameters.Length - 1)
                    .SelectAsArray(
                        static (p, @this) => (ParameterSymbol)new SynthesizedCollectionBuilderProjectedParameterSymbol(@this, p), this);
                ImmutableInterlocked.InterlockedInitialize(ref _lazyParameters, parameters);
            }
            return _lazyParameters;
        }
    }

    public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => throw new System.NotImplementedException();

    public override ImmutableArray<CustomModifier> RefCustomModifiers => throw new System.NotImplementedException();

    public override Symbol AssociatedSymbol => throw new System.NotImplementedException();

    public override Symbol ContainingSymbol => throw new System.NotImplementedException();

    internal override bool HasSpecialNameAttribute => throw new System.NotImplementedException();

    internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
    {
        throw new System.NotImplementedException();
    }

    internal override UnmanagedCallersOnlyAttributeData GetUnmanagedCallersOnlyAttributeData(bool forceComplete)
    {
        throw new System.NotImplementedException();
    }

    internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument)
    {
        throw new System.NotImplementedException();
    }

    internal override bool IsNullableAnalysisEnabled()
    {
        throw new System.NotImplementedException();
    }

    internal override int TryGetOverloadResolutionPriority()
    {
        throw new System.NotImplementedException();
    }

    private sealed class SynthesizedCollectionBuilderProjectedParameterSymbol(
        SynthesizedCollectionBuilderProjectedMethodSymbol methodSymbol,
        ParameterSymbol originalParameter) : WrappedParameterSymbol(originalParameter)
    {
        private readonly SynthesizedCollectionBuilderProjectedMethodSymbol _methodSymbol = methodSymbol;

        public override Symbol ContainingSymbol => _methodSymbol;

        internal override bool HasEnumeratorCancellationAttribute => throw new NotImplementedException();

        internal override bool IsCallerFilePath => throw new NotImplementedException();

        internal override bool IsCallerLineNumber => throw new NotImplementedException();

        internal override bool IsCallerMemberName => throw new NotImplementedException();

        internal override int CallerArgumentExpressionParameterIndex => throw new NotImplementedException();

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => throw new NotImplementedException();

        internal override bool HasInterpolatedStringHandlerArgumentError => throw new NotImplementedException();
    }

    internal sealed class SynthesizedCollectionBuilderProjectedTypeParameterSymbol(
        SynthesizedCollectionBuilderProjectedMethodSymbol methodSymbol,
        TypeParameterSymbol originalTypeParameter) : WrappedTypeParameterSymbol(originalTypeParameter)
    {
        private readonly SynthesizedCollectionBuilderProjectedMethodSymbol _methodSymbol = methodSymbol;

        public override Symbol ContainingSymbol => _methodSymbol;

        internal override bool? IsNotNullable => throw new NotImplementedException();

        internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            throw new NotImplementedException();
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            throw new NotImplementedException();
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            throw new NotImplementedException();
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            throw new NotImplementedException();
        }
    }
}

