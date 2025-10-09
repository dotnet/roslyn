// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

/// <summary>
/// See
/// https://github.com/dotnet/csharplang/blob/main/proposals/collection-expression-arguments.md#collectionbuilderattribute-methods.
/// For collection builders: For each create method for the target type, we define a projection method with an identical
/// signature to the create method but without the last parameter.
/// </summary>
/// <remarks>
/// The 'Create' methods found are guaranteed by the spec to match generic arity with the collection type being created.
/// So they will have signatures like:
/// <code><![CDATA[
/// CollectionType<T1, T2> Create<T1, T2>(Parameter1, .. ParameterN, ReadOnlySpan<ElementType<T1, T2>> elements)
/// ]]></code>
/// Then spec then requires: For a collection expression with a target type <c><![CDATA[C<S0, S1, …>]]></c> where the
/// type declaration <![CDATA[C<T0, T1, …>]]> has an associated builder method <![CDATA[B.M<U0, U1, …>()]]>, the generic
/// type arguments from the target type are applied in order — and from outermost containing type to innermost — to the
/// builder method.
/// <para/> Because of this, the collection builder method will actually be the constructed method, not the original
/// definition. And from this constructed method, we will then generate: <![CDATA[CollectionType<X, Y>
/// Create(Parameter1, .. ParameterN)]]>.  In other words, the projected method will have the same return type, no type
/// parameters/arguments, and all but the last constructed parameter from the original method.
/// </remarks>
internal sealed class SynthesizedCollectionBuilderProjectedMethodSymbol(
    MethodSymbol originalCollectionBuilderMethod) : WrappedMethodSymbol
{
    private readonly MethodSymbol _originalCollectionBuilderMethod = originalCollectionBuilderMethod;

    private ImmutableArray<ParameterSymbol> _lazyParameters;

    public override MethodSymbol UnderlyingMethod => _originalCollectionBuilderMethod;

    public override ImmutableArray<CSharpAttributeData> GetAttributes()
        => this.UnderlyingMethod.GetAttributes();

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

    public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => throw new NotImplementedException();
    public override Symbol AssociatedSymbol => throw new NotImplementedException();
    internal override bool HasSpecialNameAttribute => throw new NotImplementedException();
    internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => throw new NotImplementedException();
    internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument) => throw new NotImplementedException();
    internal override bool IsNullableAnalysisEnabled() => throw new NotImplementedException();
    internal override int TryGetOverloadResolutionPriority() => throw new NotImplementedException();

    private sealed class SynthesizedCollectionBuilderProjectedParameterSymbol(
        SynthesizedCollectionBuilderProjectedMethodSymbol methodSymbol,
        ParameterSymbol originalParameter) : WrappedParameterSymbol(originalParameter)
    {
        private readonly SynthesizedCollectionBuilderProjectedMethodSymbol _methodSymbol = methodSymbol;

        public override Symbol ContainingSymbol => _methodSymbol;

        internal override bool IsCallerLineNumber => this.UnderlyingParameter.IsCallerLineNumber;

        internal override bool HasEnumeratorCancellationAttribute => throw new NotImplementedException();

        internal override bool IsCallerFilePath => throw new NotImplementedException();

        internal override bool IsCallerMemberName => throw new NotImplementedException();

        internal override int CallerArgumentExpressionParameterIndex => throw new NotImplementedException();

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => throw new NotImplementedException();

        internal override bool HasInterpolatedStringHandlerArgumentError => throw new NotImplementedException();
    }
}

