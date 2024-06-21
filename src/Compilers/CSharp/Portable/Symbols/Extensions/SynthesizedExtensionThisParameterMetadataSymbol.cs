// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedExtensionThisParameterMetadataSymbol : ParameterSymbol
    {
        private readonly Symbol _containingSymbol;

        internal SynthesizedExtensionThisParameterMetadataSymbol(SourceExtensionMetadataMethodSymbol forMethod)
        {
            _containingSymbol = forMethod;
        }

        internal SynthesizedExtensionThisParameterMetadataSymbol(SourceExtensionMetadataPropertySymbol forProperty)
        {
            _containingSymbol = forProperty;
        }

        // PROTOTYPE(roles): Confirm what name should we use for this parameter
        //                   Is this name going to hurt or help EE when this parameter is lifted into a closure?
        public override string Name => GeneratedNames.ThisProxyFieldName();

        public override bool IsDiscard => false;

        public override TypeWithAnnotations TypeWithAnnotations
            => TypeWithAnnotations.Create(
                    ((SourceExtensionTypeSymbol)_containingSymbol.ContainingSymbol).GetExtendedTypeNoUseSiteDiagnostics(null),
                    NullableAnnotation.NotAnnotated,
                    ImmutableArray.Create<CustomModifier>(
                        CSharpCustomModifier.CreateOptional(
                            _containingSymbol.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Runtime_CompilerServices_ExtensionAttribute))));

        public override RefKind RefKind
        {
            get
            {
                if (TypeWithAnnotations.Type.IsReferenceType)
                {
                    return RefKind.None;
                }

                // PROTOTYPE(roles): State machine operates on a copy of a struct type, therefore capturing that
                //                   parameter by value for 'async'/iterator methods should probably work.
                return RefKind.Ref;
            }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return _containingSymbol.Locations; }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get { return ImmutableArray<SyntaxReference>.Empty; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        internal override ConstantValue? ExplicitDefaultConstantValue
        {
            get { return null; }
        }

        internal override bool IsMetadataOptional
        {
            get { return false; }
        }

        public override bool IsParamsArray
        {
            get { return false; }
        }

        public override bool IsParamsCollection
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
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override FlowAnalysisAnnotations FlowAnalysisAnnotations
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override ImmutableHashSet<string> NotNullIfParameterNotNull
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public override int Ordinal
        {
            get { return 0; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

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

        internal override ImmutableArray<int> InterpolatedStringHandlerArgumentIndexes => throw ExceptionUtilities.Unreachable();

        internal override bool HasInterpolatedStringHandlerArgumentError => throw ExceptionUtilities.Unreachable();

        internal override ScopedKind EffectiveScope
        {
            get
            {
                // PROTOTYPE(roles): Confirm this is good enough.
                return ScopedKind.None;
            }
        }

        internal override bool HasUnscopedRefAttribute => false;

        internal sealed override bool UseUpdatedEscapeRules => throw ExceptionUtilities.Unreachable();
    }
}
