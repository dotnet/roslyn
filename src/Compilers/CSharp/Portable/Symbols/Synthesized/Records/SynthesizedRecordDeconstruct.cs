// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordDeconstruct : SynthesizedInstanceMethodSymbol
    {
        private readonly int _memberOffset;
        private readonly ImmutableArray<PropertySymbol> _properties;

        public SynthesizedRecordDeconstruct(
            SourceMemberContainerTypeSymbol containingType,
            ImmutableArray<ParameterSymbol> ctorParameters,
            ImmutableArray<PropertySymbol> properties,
            int memberOffset)
        {
            _memberOffset = memberOffset;
            Debug.Assert(properties.All(prop => prop.GetMethod is object));
            _properties = properties;
            ContainingType = containingType;
            ReturnTypeWithAnnotations = TypeWithAnnotations.Create(
                ContainingType.DeclaringCompilation.GetSpecialType(SpecialType.System_Void));
            Parameters = ctorParameters.SelectAsArray(
                (param, ordinal, _) =>
                    SynthesizedParameterSymbol.Create(
                        this,
                        param.TypeWithAnnotations,
                        ordinal,
                        RefKind.Out,
                        param.Name),
                arg: (object?)null);
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public override NamedTypeSymbol ContainingType { get; }

        public override string Name => WellKnownMemberNames.DeconstructMethodName;

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        internal override bool HasSpecialName => false;

        internal override MethodImplAttributes ImplementationAttributes => MethodImplAttributes.Managed;

        internal override bool HasDeclarativeSecurity => false;

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;

        internal override bool RequiresSecurityObject => false;

        public override bool HidesBaseMethodsByName => false;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => true;

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
            => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Symbol? AssociatedSymbol => null;

        internal override CallingConvention CallingConvention => CallingConvention.HasThis;

        internal override bool GenerateDebugInfo => false;

        public override Symbol ContainingSymbol => ContainingType;

        public override ImmutableArray<Location> Locations => ContainingType.Locations;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            Debug.Assert(Parameters.Length == _properties.Length);
            var statementsBuilder = ArrayBuilder<BoundStatement>.GetInstance(_properties.Length + 1);
            for (int i = 0; i < _properties.Length; i++)
            {
                var parameter = Parameters[i];
                var property = _properties[i];

                // parameter_i = property_i;
                statementsBuilder.Add(F.Assignment(F.Parameter(parameter), F.Property(F.This(), property)));
            }
            statementsBuilder.Add(F.Return());
            F.CloseMethod(F.Block(statementsBuilder.ToImmutableAndFree()));
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        public override DllImportData? GetDllImportData() => null;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => throw ExceptionUtilities.Unreachable;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;
    }
}
