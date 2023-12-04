// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class ThisParameterSymbol : ParameterSymbol
    {
        internal const string SymbolName = "this";

        private readonly MethodSymbol? _containingMethod;
        private readonly TypeSymbol _containingType;

        internal ThisParameterSymbol(MethodSymbol forMethod) : this(forMethod, forMethod.ContainingType)
        {
        }

        internal ThisParameterSymbol(MethodSymbol? forMethod, TypeSymbol containingType)
        {
            Debug.Assert(containingType is not null);
            _containingMethod = forMethod;
            _containingType = containingType;
        }

        public override string Name => SymbolName;

        public override bool IsDiscard => false;

        public override TypeWithAnnotations TypeWithAnnotations
            => TypeWithAnnotations.Create(_containingType, NullableAnnotation.NotAnnotated);

        public override RefKind RefKind
        {
            get
            {
                if (ContainingType?.TypeKind != TypeKind.Struct)
                {
                    return RefKind.None;
                }

                if (_containingMethod?.MethodKind == MethodKind.Constructor)
                {
                    return RefKind.Out;
                }

                if (_containingMethod?.IsEffectivelyReadOnly == true)
                {
                    return RefKind.In;
                }

                return RefKind.Ref;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return _containingMethod is not null ? _containingMethod.Locations : ImmutableArray<Location>.Empty; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        public override Symbol ContainingSymbol
        {
            get { return (Symbol?)_containingMethod ?? _containingType; }
        }

        internal override ConstantValue? ExplicitDefaultConstantValue
        {
            get { return null; }
        }

        internal override bool IsMetadataOptional
        {
            get { return false; }
        }

        public override bool IsParams
        {
            get { return false; }
        }

        internal override bool IsIDispatchConstant
        {
            get { return false; }
        }

        internal override bool IsIUnknownConstant
        {
            get { return false; }
        }

        internal override bool IsCallerFilePath
        {
            get { return false; }
        }

        internal override bool IsCallerLineNumber
        {
            get { return false; }
        }

        internal override bool IsCallerMemberName
        {
            get { return false; }
        }

        internal override int CallerArgumentExpressionParameterIndex
        {
            get { return -1; }
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return FlowAnalysisAnnotations.None; }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get { return ImmutableHashSet<string>.Empty; }
        }

        public override int Ordinal
        {
            get { return -1; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        // TODO: structs
        public override bool IsThis
        {
            get { return true; }
        }

        // "this" is never explicitly declared.
        public override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal override bool IsMetadataIn
        {
            get { return false; }
        }

        internal override bool IsMetadataOut
        {
            get { return false; }
        }

        internal override MarshalPseudoCustomAttributeData? MarshallingInformation
        {
            get { return null; }
        }

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => ImmutableArray<int>.Empty;

        internal override bool HasInterpolatedStringHandlerArgumentError => false;

        internal override ScopedKind EffectiveScope
        {
            get
            {
                var scope = _containingType.IsStructType() ? ScopedKind.ScopedRef : ScopedKind.None;
                if (scope != ScopedKind.None &&
                    HasUnscopedRefAttribute)
                {
                    return ScopedKind.None;
                }
                return scope;
            }
        }

        internal override bool HasUnscopedRefAttribute
            => _containingMethod.HasUnscopedRefAttributeOnMethodOrProperty();

        internal sealed override bool UseUpdatedEscapeRules
            => _containingMethod?.UseUpdatedEscapeRules ?? _containingType.ContainingModule.UseUpdatedEscapeRules;
    }
}
