// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedStaticConstructor : MethodSymbol
    {
        private readonly NamedTypeSymbol _containingType;
        private ThreeState _lazyShouldEmit = ThreeState.Unknown;

        internal SynthesizedStaticConstructor(NamedTypeSymbol containingType)
        {
            _containingType = containingType;
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _containingType;
            }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _containingType;
            }
        }

        public override string Name
        {
            get
            {
                return WellKnownMemberNames.StaticConstructorName;
            }
        }

        internal override bool HasSpecialName
        {
            get { return true; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        public override bool IsVararg
        {
            get
            {
                return false;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        internal override int ParameterCount
        {
            get
            {
                return 0;
            }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                return ImmutableArray<ParameterSymbol>.Empty;
            }
        }

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            thisParameter = null;
            return true;
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                //Same as for explicitly-declared static constructors
                //(see SourceConstructorSymbol.MakeModifiers)
                return Accessibility.Private;
            }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            //For the sake of matching the metadata output of the native compiler, make synthesized constructors appear last in the metadata.
            //This is not critical, but it makes it easier on tools that are comparing metadata.
            return LexicalSortKey.SynthesizedCCtor;
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ContainingType.Locations;
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray<SyntaxReference>.Empty;
            }
        }

        public override RefKind RefKind
        {
            get
            {
                return RefKind.None;
            }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                return TypeWithAnnotations.Create(ContainingAssembly.GetSpecialType(SpecialType.System_Void));
            }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                return ImmutableArray<CustomModifier>.Empty;
            }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get
            {
                return ImmutableArray<TypeWithAnnotations>.Empty;
            }
        }

        public override Symbol? AssociatedSymbol
        {
            get
            {
                return null;
            }
        }

        public override int Arity
        {
            get
            {
                return 0;
            }
        }

        public override bool ReturnsVoid
        {
            get
            {
                return true;
            }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return MethodKind.StaticConstructor;
            }
        }

        public override bool IsExtern
        {
            get
            {
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return false;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return false;
            }
        }

        public override bool IsOverride
        {
            get
            {
                return false;
            }
        }

        public override bool IsVirtual
        {
            get
            {
                return false;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return true;
            }
        }

        public override bool IsAsync
        {
            get
            {
                return false;
            }
        }

        public override bool HidesBaseMethodsByName
        {
            get
            {
                return false;
            }
        }

        public override bool IsExtensionMethod
        {
            get
            {
                return false;
            }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get
            {
                //this matches the value in SourceMethodSymbol.CallingConvention for static methods
                return Microsoft.Cci.CallingConvention.Default;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                return ImmutableArray<MethodSymbol>.Empty;
            }
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => false;

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return true;
            }
        }

        internal sealed override bool GenerateDebugInfo
        {
            get
            {
                // debugging static field initializers
                return true;
            }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal sealed override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
        {
            return false;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        internal override bool RequiresSecurityObject
        {
            get
            {
                return false;
            }
        }

        public override DllImportData? GetDllImportData()
        {
            return null;
        }

        public sealed override bool AreLocalsZeroed
        {
            get { return ContainingType.AreLocalsZeroed; }
        }

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override ObsoleteAttributeData? ObsoleteAttributeData
        {
            get { return null; }
        }

        internal sealed override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => null;

        internal sealed override bool HasSpecialNameAttribute => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            var containingType = (SourceMemberContainerTypeSymbol)this.ContainingType;
            return containingType.CalculateSyntaxOffsetInSynthesizedConstructor(localPosition, localTree, isStatic: true);
        }

        internal sealed override bool IsNullableAnalysisEnabled() =>
            (ContainingType as SourceMemberContainerTypeSymbol)?.IsNullableEnabledForConstructorsAndInitializers(useStatic: true) ?? false;

        internal bool ShouldEmit(ImmutableArray<BoundInitializer> boundInitializersOpt = default)
        {
            if (_lazyShouldEmit.HasValue())
            {
                return _lazyShouldEmit.Value();
            }

            var shouldEmit = CalculateShouldEmit(boundInitializersOpt);
            _lazyShouldEmit = shouldEmit.ToThreeState();
            return shouldEmit;
        }

        private bool CalculateShouldEmit(ImmutableArray<BoundInitializer> boundInitializersOpt = default)
        {
            if (boundInitializersOpt.IsDefault)
            {
                if (!(ContainingType is SourceMemberContainerTypeSymbol sourceType))
                {
                    Debug.Assert(ContainingType is SynthesizedClosureEnvironment);
                    return true;
                }

                boundInitializersOpt = Binder.BindFieldInitializers(
                    DeclaringCompilation,
                    sourceType.IsScriptClass ? sourceType.GetScriptInitializer() : null,
                    sourceType.StaticInitializers,
                    BindingDiagnosticBag.Discarded,
                    out _);
            }

            foreach (var initializer in boundInitializersOpt)
            {
                if (!(initializer is BoundFieldEqualsValue { Value: { } value }))
                {
                    // this isn't a BoundFieldEqualsValue, so this initializer is doing
                    // something we don't understand. Better just emit it.
                    return true;
                }

                if (!value.IsDefaultValue())
                {
                    return true;
                }
            }

            return false;
        }

        protected sealed override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal sealed override bool HasUnscopedRefAttribute => false;

        internal sealed override bool UseUpdatedEscapeRules => false;

        internal sealed override CallerUnsafeMode CallerUnsafeMode => CallerUnsafeMode.None;

        internal sealed override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal sealed override int TryGetOverloadResolutionPriority()
        {
            return 0;
        }
    }
}
