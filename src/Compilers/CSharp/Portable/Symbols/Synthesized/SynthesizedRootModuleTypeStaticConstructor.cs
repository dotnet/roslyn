// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRootModuleTypeStaticConstructor : MethodSymbol
    {
        private readonly RootModuleType _rootModuleType;

        internal SynthesizedRootModuleTypeStaticConstructor(RootModuleType rootModuleType, NamedTypeSymbol containingType)
        {
            _rootModuleType = rootModuleType;
            ContainingType = containingType;
        }

        public override NamedTypeSymbol ContainingType { get; }

        public override Symbol ContainingSymbol => ContainingType;

        public override string Name => WellKnownMemberNames.StaticConstructorName;

        internal override bool HasSpecialName => true;

        public override bool HidesBaseMethodsByName => true;

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            var body = ArrayBuilder<BoundStatement>.GetInstance();

            foreach (var moduleInitializerMethod in _rootModuleType.GetModuleInitializerMethods())
            {
                body.Add(F.ExpressionStatement(
                    F.StaticCall((MethodSymbol)moduleInitializerMethod, ImmutableArray<BoundExpression>.Empty)));
            }

            body.Add(F.Return());

            F.CloseMethod(F.Block(body.ToImmutableAndFree()));
        }

        internal override bool GenerateDebugInfo => false;

        public override bool IsImplicitlyDeclared => true;

        public override TypeWithAnnotations ReturnTypeWithAnnotations => TypeWithAnnotations.Create(ContainingAssembly.GetSpecialType(SpecialType.System_Void));

        public override bool AreLocalsZeroed => ContainingModule.AreLocalsZeroed;

        public override MethodKind MethodKind => MethodKind.StaticConstructor;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => true;

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Symbol? AssociatedSymbol => null;

        public override ImmutableArray<Location> Locations => ImmutableArray<Location>.Empty;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override Accessibility DeclaredAccessibility => Accessibility.Private;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override MethodImplAttributes ImplementationAttributes => default;

        internal override bool HasDeclarativeSecurity => false;

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;

        internal override bool RequiresSecurityObject => false;

        internal override bool IsDeclaredReadOnly => false;

        internal override CallingConvention CallingConvention => CallingConvention.Default;

        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

        public override DllImportData? GetDllImportData() => null;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => throw ExceptionUtilities.Unreachable;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;
    }
}
