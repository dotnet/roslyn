// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

/// <summary>
/// See https://github.com/dotnet/csharplang/blob/90f1d8b0e9ba8a140f73aef376833969cce8bf9e/proposals/collection-expression-arguments.md?plain=1#L225
/// For collection builders: For each create method for the target type, we define a projection method with an identical
/// signature to the create method but without the last parameter.  This is the signature of the method a `with(...)`
/// element will be matched against when using a collection builder type for a collection expression.
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

    /// <summary>
    /// The projection method itself is intentionally not obsolete.  We don't want to report obsoletion errors when
    /// using it in some speculative binding for overload resolution.  Instead, we will then report the error on the
    /// original <see cref="UnderlyingMethod"/> this points at directly in <see
    /// cref="Binder.CheckCollectionBuilderMethod"/>.
    /// </summary>
    internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

    /// <summary>
    /// Similarly to <see cref="ObsoleteAttributeData"/>, we do not want to report unmanaged callers only on this
    /// method.  Instead, we will then report the error on the original <see cref="UnderlyingMethod"/> this points at
    /// directly in <see cref="Binder.CheckCollectionBuilderMethod"/>.
    /// </summary>
    internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => null;

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
                // Grab all but the last parameter from the underlying method.
                var parameters = this.UnderlyingMethod.Parameters;
                var parameterCount = parameters.Length - 1;
                var builder = ArrayBuilder<ParameterSymbol>.GetInstance(parameterCount);
                for (int i = 0; i < parameterCount; i++)
                    builder.Add(new SynthesizedCollectionBuilderProjectedParameterSymbol(this, parameters[i]));

                ImmutableInterlocked.InterlockedInitialize(ref _lazyParameters, builder.ToImmutableAndFree());
            }
            return _lazyParameters;
        }
    }

    internal override int TryGetOverloadResolutionPriority()
        => this.UnderlyingMethod.TryGetOverloadResolutionPriority();

    public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => throw ExceptionUtilities.Unreachable();
    public override Symbol AssociatedSymbol => throw ExceptionUtilities.Unreachable();
    internal override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();
    internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => throw ExceptionUtilities.Unreachable();
    internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol builderArgument) => throw ExceptionUtilities.Unreachable();
    internal override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();
    internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes) => throw ExceptionUtilities.Unreachable();

    private sealed class SynthesizedCollectionBuilderProjectedParameterSymbol(
        SynthesizedCollectionBuilderProjectedMethodSymbol methodSymbol,
        ParameterSymbol originalParameter) : WrappedParameterSymbol(originalParameter)
    {
        private readonly SynthesizedCollectionBuilderProjectedMethodSymbol _methodSymbol = methodSymbol;

        public override Symbol ContainingSymbol => _methodSymbol;

        internal override bool IsCallerLineNumber => this.UnderlyingParameter.IsCallerLineNumber;
        internal override bool IsCallerFilePath => this.UnderlyingParameter.IsCallerFilePath;
        internal override bool IsCallerMemberName => this.UnderlyingParameter.IsCallerMemberName;

        internal override int CallerArgumentExpressionParameterIndex => this.UnderlyingParameter.CallerArgumentExpressionParameterIndex;

        internal override bool HasEnumeratorCancellationAttribute => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => throw ExceptionUtilities.Unreachable();

        internal override bool HasInterpolatedStringHandlerArgumentError => throw ExceptionUtilities.Unreachable();

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes) => throw ExceptionUtilities.Unreachable();
    }
}

