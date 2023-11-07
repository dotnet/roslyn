// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class FunctionPointerParameterSymbol : ParameterSymbol
    {
        private readonly FunctionPointerMethodSymbol _containingSymbol;

        public FunctionPointerParameterSymbol(TypeWithAnnotations typeWithAnnotations, RefKind refKind, int ordinal, FunctionPointerMethodSymbol containingSymbol, ImmutableArray<CustomModifier> refCustomModifiers)
        {
            Debug.Assert(typeWithAnnotations.HasType);
            TypeWithAnnotations = typeWithAnnotations;
            RefKind = refKind;
            Ordinal = ordinal;
            _containingSymbol = containingSymbol;
            RefCustomModifiers = refCustomModifiers;
        }

        public override TypeWithAnnotations TypeWithAnnotations { get; }
        public override RefKind RefKind { get; }
        public override int Ordinal { get; }
        public override Symbol ContainingSymbol => _containingSymbol;
        public override ImmutableArray<CustomModifier> RefCustomModifiers { get; }

        internal override ScopedKind EffectiveScope
            => ParameterHelpers.IsRefScopedByDefault(this) ? ScopedKind.ScopedRef : ScopedKind.None;
        internal override bool HasUnscopedRefAttribute => false;
        internal override bool UseUpdatedEscapeRules => _containingSymbol.UseUpdatedEscapeRules;

        public override bool Equals(Symbol other, TypeCompareKind compareKind)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (!(other is FunctionPointerParameterSymbol param))
            {
                return false;
            }

            return Equals(param, compareKind);
        }

        internal bool Equals(FunctionPointerParameterSymbol other, TypeCompareKind compareKind)
            => other.Ordinal == Ordinal
               && _containingSymbol.Equals(other._containingSymbol, compareKind);

        internal bool MethodEqualityChecks(FunctionPointerParameterSymbol other, TypeCompareKind compareKind)
            => FunctionPointerTypeSymbol.RefKindEquals(compareKind, RefKind, other.RefKind)
               && ((compareKind & TypeCompareKind.IgnoreCustomModifiersAndArraySizesAndLowerBounds) != 0
                    || RefCustomModifiers.SequenceEqual(other.RefCustomModifiers))
               && TypeWithAnnotations.Equals(other.TypeWithAnnotations, compareKind);

        public override int GetHashCode()
        {
            return Hash.Combine(_containingSymbol.GetHashCode(), Ordinal + 1);
        }

        internal int MethodHashCode()
            => Hash.Combine(TypeWithAnnotations.GetHashCode(), ((int)FunctionPointerTypeSymbol.GetRefKindForHashCode(RefKind)).GetHashCode());

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;
        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;
        public override bool IsDiscard => false;
        public override bool IsParams => false;
        public override bool IsImplicitlyDeclared => true;
        internal override MarshalPseudoCustomAttributeData? MarshallingInformation => null;
        internal override bool IsMetadataOptional => false;
        internal override bool IsMetadataIn => RefKind is RefKind.In or RefKind.RefReadOnlyParameter;
        internal override bool IsMetadataOut => RefKind == RefKind.Out;
        internal override ConstantValue? ExplicitDefaultConstantValue => null;
        internal override bool IsIDispatchConstant => false;
        internal override bool IsIUnknownConstant => false;
        internal override bool IsCallerFilePath => false;
        internal override bool IsCallerLineNumber => false;
        internal override bool IsCallerMemberName => false;
        internal override int CallerArgumentExpressionParameterIndex => -1;
        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;
        internal override ImmutableHashSet<string> NotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;
        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => ImmutableArray<int>.Empty;
        internal override bool HasInterpolatedStringHandlerArgumentError => false;
    }
}
