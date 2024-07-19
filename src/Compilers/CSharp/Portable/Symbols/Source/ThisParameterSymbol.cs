// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class ThisParameterSymbolBase : ParameterSymbol
    {
        internal const string SymbolName = "this";

        public sealed override string Name => SymbolName;

        public sealed override bool IsDiscard => false;

        public sealed override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        internal sealed override ConstantValue? ExplicitDefaultConstantValue
        {
            get { return null; }
        }

        internal sealed override bool IsMetadataOptional
        {
            get { return false; }
        }

        public sealed override bool IsParamsArray
        {
            get { return false; }
        }

        public sealed override bool IsParamsCollection
        {
            get { return false; }
        }

        internal sealed override bool IsIDispatchConstant
        {
            get { return false; }
        }

        internal sealed override bool IsIUnknownConstant
        {
            get { return false; }
        }

        internal sealed override bool IsCallerFilePath
        {
            get { return false; }
        }

        internal sealed override bool IsCallerLineNumber
        {
            get { return false; }
        }

        internal sealed override bool IsCallerMemberName
        {
            get { return false; }
        }

        internal sealed override int CallerArgumentExpressionParameterIndex
        {
            get { return -1; }
        }

        internal sealed override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { return FlowAnalysisAnnotations.None; }
        }

        internal sealed override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get { return ImmutableHashSet<string>.Empty; }
        }

        public sealed override int Ordinal
        {
            get { return -1; }
        }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public sealed override bool IsThis
        {
            get { return true; }
        }

        // "this" is never explicitly declared.
        public sealed override bool IsImplicitlyDeclared
        {
            get { return true; }
        }

        internal sealed override bool IsMetadataIn
        {
            get { return false; }
        }

        internal sealed override bool IsMetadataOut
        {
            get { return false; }
        }

        internal sealed override MarshalPseudoCustomAttributeData? MarshallingInformation
        {
            get { return null; }
        }

        internal sealed override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => ImmutableArray<int>.Empty;

        internal sealed override bool HasInterpolatedStringHandlerArgumentError => false;
    }

    internal sealed class ThisParameterSymbol : ThisParameterSymbolBase
    {
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

        public override TypeWithAnnotations TypeWithAnnotations
            => TypeWithAnnotations.Create(_containingType, NullableAnnotation.NotAnnotated);

        public override RefKind RefKind
        {
            get
            {
                if (_containingMethod is not null &&
                    ContainingType.OriginalDefinition.TryGetCorrespondingStaticMetadataExtensionMember(_containingMethod.OriginalDefinition) is MethodSymbol staticMetadataSymbol)
                {
                    // PROTOTYPE(roles): It looks like SemanticModel and probably some other consumers might create this symbol without a method.
                    //                   Figure out what the scenarios and what RefKind will be appropriate for extension types extending
                    //                   types not known to be a reference type.
                    return staticMetadataSymbol.Parameters[0].RefKind;
                }

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

        public override Symbol ContainingSymbol
        {
            get { return (Symbol?)_containingMethod ?? _containingType; }
        }

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
