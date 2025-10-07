// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

internal sealed class SynthesizedCollectionBuilderProjectedMethodSymbol(
    MethodSymbol originalCollectionBuilderMethod) : WrappedMethodSymbol
{
    private readonly MethodSymbol _originalCollectionBuilderMethod = originalCollectionBuilderMethod;

    private ImmutableArray<ParameterSymbol> _lazyParameters;

    public override MethodSymbol UnderlyingMethod => _originalCollectionBuilderMethod;

    public override Symbol ContainingSymbol => this.UnderlyingMethod.ContainingSymbol;
    public override ImmutableArray<CustomModifier> RefCustomModifiers => this.UnderlyingMethod.RefCustomModifiers;
    public override TypeWithAnnotations ReturnTypeWithAnnotations => this.UnderlyingMethod.ReturnTypeWithAnnotations;

    // Note: it is very intentional that we return empty arrays for Type arguments/parameters.  Consider a
    // hypothetical signature like:
    //
    //  Dict<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey> comparer, ReadOnlySpan<KeyValuePair<TKey, TValue>> elements)
    //
    // Where the target type is `Dict<int, string>`.  The conversion process will already have instantiated this method
    // with the appropriate `int, string` type arguments.  What we want to then expose is a signature like:
    //
    //  Dict<int, string> Create(IEqualityComparer<int> comparer)
    //
    // i.e.  we want to remove the type parameters and the final parameter that takes the elements.  That way there is
    // no more inference done, or any confusion about needing type arguments when resolving a `with(...)` element
    // against this signature.
    public override int Arity => 0;
    public override bool IsGenericMethod => false;
    public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => [];
    public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];

    internal override int ParameterCount => base.ParameterCount - 1;
    public override ImmutableArray<ParameterSymbol> Parameters
    {
        get
        {
            if (_lazyParameters.IsDefault)
            {
                var parameters = this.UnderlyingMethod.Parameters
                    .Take(this.ParameterCount)
                    .SelectAsArray(
                        static (p, @this) => (ParameterSymbol)new SynthesizedCollectionBuilderProjectedParameterSymbol(@this, p), this);
                ImmutableInterlocked.InterlockedInitialize(ref _lazyParameters, parameters);
            }
            return _lazyParameters;
        }
    }

    internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete)
        => this.UnderlyingMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);

    public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => throw new System.NotImplementedException();

    public override Symbol AssociatedSymbol => throw new System.NotImplementedException();

    internal override bool HasSpecialNameAttribute => throw new System.NotImplementedException();

    internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
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

