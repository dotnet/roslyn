// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Inferred delegate type state, recorded during testing only.
    /// </summary>
    internal sealed class InferredDelegateTypeData
    {
        /// <summary>
        /// Number of delegate types calculated in the compilation.
        /// </summary>
        internal int InferredDelegateCount;
    }

    /// <summary>
    /// A <see cref="TypeSymbol"/> implementation that represents the lazily-inferred signature of a
    /// lambda expression or method group. This is implemented as a <see cref="TypeSymbol"/>
    /// to allow types and function signatures to be treated similarly in <see cref="ConversionsBase"/>,
    /// <see cref="BestTypeInferrer"/>, and <see cref="MethodTypeInferrer"/>. Instances of this type
    /// should only be used in those code paths and should not be exposed from the symbol model.
    /// The actual delegate signature is calculated on demand in <see cref="GetInternalDelegateType()"/>.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class FunctionTypeSymbol : TypeSymbol
    {
        private static readonly NamedTypeSymbol Uninitialized = new UnsupportedMetadataTypeSymbol();

        private readonly Binder? _binder;
        private readonly Func<Binder, BoundExpression, NamedTypeSymbol?>? _calculateDelegate;

        private BoundExpression? _expression;
        private NamedTypeSymbol? _lazyDelegateType;

        internal static FunctionTypeSymbol? CreateIfFeatureEnabled(SyntaxNode syntax, Binder binder, Func<Binder, BoundExpression, NamedTypeSymbol?> calculateDelegate)
        {
            return syntax.IsFeatureEnabled(MessageID.IDS_FeatureInferredDelegateType) ?
                new FunctionTypeSymbol(binder, calculateDelegate) :
                null;
        }

        private FunctionTypeSymbol(Binder binder, Func<Binder, BoundExpression, NamedTypeSymbol?> calculateDelegate)
        {
            _binder = binder;
            _calculateDelegate = calculateDelegate;
            _lazyDelegateType = Uninitialized;
        }

        internal FunctionTypeSymbol(NamedTypeSymbol delegateType)
        {
            _lazyDelegateType = delegateType;
        }

        internal void SetExpression(BoundExpression expression)
        {
            Debug.Assert((object?)_lazyDelegateType == Uninitialized);
            Debug.Assert(_expression is null);
            Debug.Assert(expression.Kind is BoundKind.MethodGroup or BoundKind.UnboundLambda);

            _expression = expression;
        }

        /// <summary>
        /// Returns the inferred signature as a delegate type
        /// or null if the signature could not be inferred.
        /// </summary>
        internal NamedTypeSymbol? GetInternalDelegateType()
        {
            if ((object?)_lazyDelegateType == Uninitialized)
            {
                Debug.Assert(_binder is { });
                Debug.Assert(_calculateDelegate is { });
                Debug.Assert(_expression is { });

                var delegateType = _calculateDelegate(_binder, _expression);
                var result = Interlocked.CompareExchange(ref _lazyDelegateType, delegateType, Uninitialized);

                if (_binder.Compilation.TestOnlyCompilationData is InferredDelegateTypeData data &&
                    (object?)result == Uninitialized)
                {
                    Interlocked.Increment(ref data.InferredDelegateCount);
                }
            }

            return _lazyDelegateType;
        }

        public override bool IsReferenceType => true;

        public override bool IsValueType => false;

        public override TypeKind TypeKind => TypeKindInternal.FunctionType;

        public override bool IsRefLikeType => false;

        public override bool IsReadOnly => true;

        public override SymbolKind Kind => SymbolKindInternal.FunctionType;

        public override Symbol? ContainingSymbol => null;

        public override ImmutableArray<Location> Locations => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => throw ExceptionUtilities.Unreachable();

        public override Accessibility DeclaredAccessibility => throw ExceptionUtilities.Unreachable();

        public override bool IsStatic => false;

        public override bool IsAbstract => throw ExceptionUtilities.Unreachable();

        public override bool IsSealed => throw ExceptionUtilities.Unreachable();

        internal override NamedTypeSymbol? BaseTypeNoUseSiteDiagnostics => null;

        internal override bool IsRecord => throw ExceptionUtilities.Unreachable();

        internal override bool IsRecordStruct => throw ExceptionUtilities.Unreachable();

        internal override ObsoleteAttributeData ObsoleteAttributeData => throw ExceptionUtilities.Unreachable();

        public override void Accept(CSharpSymbolVisitor visitor) => throw ExceptionUtilities.Unreachable();

        public override TResult Accept<TResult>(CSharpSymbolVisitor<TResult> visitor) => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<Symbol> GetMembers() => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<Symbol> GetMembers(string name) => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers() => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<NamedTypeSymbol> GetTypeMembers(ReadOnlyMemory<char> name) => throw ExceptionUtilities.Unreachable();

        protected override ISymbol CreateISymbol() => throw ExceptionUtilities.Unreachable();

        protected override ITypeSymbol CreateITypeSymbol(CodeAnalysis.NullableAnnotation nullableAnnotation) => throw ExceptionUtilities.Unreachable();

        internal override TResult Accept<TArgument, TResult>(CSharpSymbolVisitor<TArgument, TResult> visitor, TArgument a) => throw ExceptionUtilities.Unreachable();

        internal override void AddNullableTransforms(ArrayBuilder<byte> transforms) => throw ExceptionUtilities.Unreachable();

        internal override bool ApplyNullableTransforms(byte defaultTransformFlag, ImmutableArray<byte> transforms, ref int position, out TypeSymbol result) => throw ExceptionUtilities.Unreachable();

        internal override ManagedKind GetManagedKind(ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo) => throw ExceptionUtilities.Unreachable();

        internal override bool GetUnificationUseSiteDiagnosticRecursive(ref DiagnosticInfo result, Symbol owner, ref HashSet<TypeSymbol> checkedTypes) => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<NamedTypeSymbol> InterfacesNoUseSiteDiagnostics(ConsList<TypeSymbol>? basesBeingResolved = null) => ImmutableArray<NamedTypeSymbol>.Empty;

        internal override TypeSymbol MergeEquivalentTypes(TypeSymbol other, VarianceKind variance)
        {
            Debug.Assert(this.Equals(other, TypeCompareKind.IgnoreDynamicAndTupleNames | TypeCompareKind.IgnoreNullableModifiersForReferenceTypes));

            var thisDelegateType = GetInternalDelegateType();
            var otherType = (FunctionTypeSymbol)other;
            var otherDelegateType = otherType.GetInternalDelegateType();

            if (thisDelegateType is null || otherDelegateType is null)
            {
                return this;
            }

            var mergedDelegateType = (NamedTypeSymbol)thisDelegateType.MergeEquivalentTypes(otherDelegateType, variance);
            return (object)thisDelegateType == mergedDelegateType ?
                this :
                otherType.WithDelegateType(mergedDelegateType);
        }

        internal override TypeSymbol SetNullabilityForReferenceTypes(Func<TypeWithAnnotations, TypeWithAnnotations> transform)
        {
            var thisDelegateType = GetInternalDelegateType();
            if (thisDelegateType is null)
            {
                return this;
            }
            return WithDelegateType((NamedTypeSymbol)thisDelegateType.SetNullabilityForReferenceTypes(transform));
        }

        private FunctionTypeSymbol WithDelegateType(NamedTypeSymbol delegateType)
        {
            var thisDelegateType = GetInternalDelegateType();
            return (object?)thisDelegateType == delegateType ?
                this :
                new FunctionTypeSymbol(delegateType);
        }

        internal override IEnumerable<(MethodSymbol Body, MethodSymbol Implemented)> SynthesizedInterfaceMethodImpls() => throw ExceptionUtilities.Unreachable();

        internal override bool HasInlineArrayAttribute(out int length)
        {
            length = 0;
            return false;
        }

        internal override bool Equals(TypeSymbol t2, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, t2))
            {
                return true;
            }

            if (t2 is FunctionTypeSymbol otherType)
            {
                var thisDelegateType = GetInternalDelegateType();
                var otherDelegateType = otherType.GetInternalDelegateType();

                if (thisDelegateType is null || otherDelegateType is null)
                {
                    return false;
                }

                return Equals(thisDelegateType, otherDelegateType, compareKind);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return GetInternalDelegateType()?.GetHashCode() ?? 0;
        }

        internal override string GetDebuggerDisplay()
        {
            return $"FunctionTypeSymbol: {GetInternalDelegateType()?.GetDebuggerDisplay()}";
        }
    }
}
