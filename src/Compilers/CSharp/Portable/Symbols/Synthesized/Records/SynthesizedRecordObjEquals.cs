// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordObjEquals : SynthesizedInstanceMethodSymbol
    {
        private readonly MethodSymbol _typedRecordEquals;
        private readonly int _memberOffset;

        public override NamedTypeSymbol ContainingType { get; }

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public SynthesizedRecordObjEquals(NamedTypeSymbol containingType, MethodSymbol typedRecordEquals, int memberOffset)
        {
            var compilation = containingType.DeclaringCompilation;
            _typedRecordEquals = typedRecordEquals;
            _memberOffset = memberOffset;
            ContainingType = containingType;
            Parameters = ImmutableArray.Create<ParameterSymbol>(SynthesizedParameterSymbol.Create(
                this,
                TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Object), NullableAnnotation.Annotated),
                ordinal: 0,
                RefKind.None));
            ReturnTypeWithAnnotations = TypeWithAnnotations.Create(compilation.GetSpecialType(SpecialType.System_Boolean));
        }

        public override string Name => "Equals";

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName => false;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => false;

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
            => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Symbol? AssociatedSymbol => null;

        public override Symbol ContainingSymbol => ContainingType;

        public override ImmutableArray<Location> Locations => ContainingType.Locations;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override bool IsOverride => true;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override bool HasSpecialName => false;

        internal override MethodImplAttributes ImplementationAttributes => MethodImplAttributes.Managed;

        internal override bool HasDeclarativeSecurity => false;

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;

        internal override bool RequiresSecurityObject => false;

        internal override CallingConvention CallingConvention => CallingConvention.HasThis;

        internal override bool GenerateDebugInfo => false;

        public override DllImportData? GetDllImportData() => null;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
            => ImmutableArray<string>.Empty;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
            => Array.Empty<SecurityAttribute>();

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => true;

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            var paramAccess = F.Parameter(Parameters[0]);

            BoundExpression expression;
            if (ContainingType.IsStructType())
            {
                // For structs:
                //
                //      return param is ContainingType i ? this.Equals(in i) : false;
                expression = F.Conditional(
                    F.Is(paramAccess, ContainingType),
                    F.Call(
                        F.This(),
                        _typedRecordEquals,
                        ImmutableArray.Create<RefKind>(RefKind.In),
                        ImmutableArray.Create<BoundExpression>(F.Convert(ContainingType, paramAccess))),
                    F.Literal(false),
                    F.SpecialType(SpecialType.System_Boolean));
            }
            else
            {
                // For classes:
                //      return this.Equals(param as ContainingType);
                expression = F.Call(F.This(), _typedRecordEquals, F.As(paramAccess, ContainingType));
            }

            F.CloseMethod(F.Block(ImmutableArray.Create<BoundStatement>(F.Return(expression))));
        }
    }
}
