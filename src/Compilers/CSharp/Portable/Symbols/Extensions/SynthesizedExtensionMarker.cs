// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This method encodes the information needed to round-trip extension types
    /// through metadata.
    ///
    /// It encodes receiver parameter - the only parameter of the method
    /// </summary>
    internal sealed class SynthesizedExtensionMarker : SynthesizedMethodSymbol
    {
        private readonly NamedTypeSymbol _extensionType;
        private readonly TypeSymbol _returnType;
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        internal SynthesizedExtensionMarker(NamedTypeSymbol extensionType, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(extensionType.IsDefinition);
            Debug.Assert(extensionType.IsExtension);
            Debug.Assert(extensionType.ExtensionParameter is not null);

            _extensionType = extensionType;
            _returnType = Binder.GetSpecialType(DeclaringCompilation, SpecialType.System_Void, extensionType.GetFirstLocation(), diagnostics);
            _parameters = [new ReceiverParameterSymbol(this, extensionType.ExtensionParameter)];

            return;
        }

        public override string Name => WellKnownMemberNames.ExtensionMarkerMethodName;

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes => default;

        public override bool IsVararg => false;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        internal override int ParameterCount => _parameters.Length;

        public override ImmutableArray<ParameterSymbol> Parameters => _parameters;

        public override RefKind RefKind => RefKind.None;

        public override TypeWithAnnotations ReturnTypeWithAnnotations => TypeWithAnnotations.Create(_returnType);

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        internal sealed override bool GenerateDebugInfo => false;

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override bool IsMetadataFinal => false;

        internal override bool IsInitOnly => false;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        internal override bool HasSpecialName => true; // PROTOTYPE: reconcile with spec

        internal override bool HasRuntimeSpecialName => false;

        internal override bool HasDeclarativeSecurity => false;

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;

        internal override bool RequiresSecurityObject => false;

        public override bool HidesBaseMethodsByName => false;

        public override bool ReturnsVoid => true;

        public override bool IsAsync => false;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Symbol? AssociatedSymbol => null;

        protected override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal override CallingConvention CallingConvention => CallingConvention.Default;

        public override Symbol ContainingSymbol => _extensionType;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CloseMethod(F.Return());
        }

        public override string GetDocumentationCommentXml(CultureInfo? preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default)
            => "";

        public override DllImportData? GetDllImportData() => null;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
            => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
            => ImmutableArray<string>.Empty;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None) => false;

        private sealed class ReceiverParameterSymbol : RewrittenParameterSymbol
        {
            private readonly SynthesizedExtensionMarker _containingMethod;

            public ReceiverParameterSymbol(SynthesizedExtensionMarker containingMethod, ParameterSymbol originalParameter) :
                base(originalParameter)
            {
                _containingMethod = containingMethod;
            }

            public override Symbol ContainingSymbol
            {
                get { return _containingMethod; }
            }
        }
    }
}

