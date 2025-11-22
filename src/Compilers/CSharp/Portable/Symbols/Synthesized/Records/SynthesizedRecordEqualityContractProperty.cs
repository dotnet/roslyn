// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordEqualityContractProperty : SourcePropertySymbolBase
    {
        internal const string PropertyName = "EqualityContract";

        public SynthesizedRecordEqualityContractProperty(SourceMemberContainerTypeSymbol containingType, BindingDiagnosticBag diagnostics)
            : base(
                containingType,
                syntax: (CSharpSyntaxNode)containingType.SyntaxReferences[0].GetSyntax(),
                hasGetAccessor: true,
                hasSetAccessor: false,
                isExplicitInterfaceImplementation: false,
                explicitInterfaceType: null,
                aliasQualifierOpt: null,
                modifiers: MakeModifiers(containingType),
                hasInitializer: false,
                hasExplicitAccessMod: false,
                hasAutoPropertyGet: false,
                hasAutoPropertySet: false,
                isExpressionBodied: false,
                accessorsHaveImplementation: true,
                getterUsesFieldKeyword: false,
                setterUsesFieldKeyword: false,
                RefKind.None,
                PropertyName,
                indexerNameAttributeLists: new SyntaxList<AttributeListSyntax>(),
                containingType.GetFirstLocation(),
                diagnostics)
        {
            Debug.Assert(!containingType.IsRecordStruct);
        }

        private static DeclarationModifiers MakeModifiers(SourceMemberContainerTypeSymbol containingType)
        {
            var baseType = containingType.BaseTypeNoUseSiteDiagnostics;

            // Only mark as override if the base type is actually a record.
            // If it's not a record, ERR_BadRecordBase will be reported separately.
            if (!baseType.IsObjectType() && SynthesizedRecordClone.BaseTypeIsRecordNoUseSiteDiagnostics(baseType))
                return DeclarationModifiers.Protected | DeclarationModifiers.Override;

            return containingType.IsSealed
                ? DeclarationModifiers.Private
                : DeclarationModifiers.Protected | DeclarationModifiers.Virtual;
        }

        public override bool IsImplicitlyDeclared => true;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
            => OneOrMany<SyntaxList<AttributeListSyntax>>.Empty;

        protected override SourcePropertySymbolBase? BoundAttributesSource => null;

        public override IAttributeTargetSymbol AttributesOwner => this;

        protected override Location TypeLocation
            => ContainingType.GetFirstLocation();

        protected override SourcePropertyAccessorSymbol CreateGetAccessorSymbol(bool isAutoPropertyAccessor, BindingDiagnosticBag diagnostics)
        {
            return SourcePropertyAccessorSymbol.CreateAccessorSymbol(
                ContainingType,
                this,
                _modifiers,
                ContainingType.GetFirstLocation(),
                (CSharpSyntaxNode)((SourceMemberContainerTypeSymbol)ContainingType).SyntaxReferences[0].GetSyntax(),
                diagnostics);
        }

        protected override SourcePropertyAccessorSymbol CreateSetAccessorSymbol(bool isAutoPropertyAccessor, BindingDiagnosticBag diagnostics)
        {
            throw ExceptionUtilities.Unreachable();
        }

        protected override (TypeWithAnnotations Type, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindType(BindingDiagnosticBag diagnostics)
        {
            return (TypeWithAnnotations.Create(Binder.GetWellKnownType(DeclaringCompilation, WellKnownType.System_Type, diagnostics, Location), NullableAnnotation.NotAnnotated),
                    ImmutableArray<ParameterSymbol>.Empty);
        }

        protected override void ValidatePropertyType(BindingDiagnosticBag diagnostics)
        {
            base.ValidatePropertyType(diagnostics);
            VerifyOverridesEqualityContractFromBase(this, diagnostics);
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

        internal static void VerifyOverridesEqualityContractFromBase(PropertySymbol overriding, BindingDiagnosticBag diagnostics)
        {
            var baseType = overriding.ContainingType.BaseTypeNoUseSiteDiagnostics;

            // If the base type is not a record, ERR_BadRecordBase will already be reported.
            // Don't cascade an override error in this case.
            {
                return;
            }

            bool reportAnError = false;
            if (!baseType.IsObjectType() && SynthesizedRecordClone.BaseTypeIsRecordNoUseSiteDiagnostics(baseType))
            {
                if (!overriding.IsOverride)
                {
                    reportAnError = true;
                }
                else
                {
                    var overridden = overriding.OverriddenProperty;

                    if (overridden is object &&
                        !overridden.ContainingType.Equals(baseType, TypeCompareKind.AllIgnoreOptions))
                    {
                        reportAnError = true;
                    }
                }
            }

            if (reportAnError)
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, overriding.GetFirstLocation(), overriding, baseType);
            }
        }

        internal sealed class GetAccessorSymbol : SourcePropertyAccessorSymbol
        {
            internal GetAccessorSymbol(
                NamedTypeSymbol containingType,
                SourcePropertySymbolBase property,
                DeclarationModifiers propertyModifiers,
                Location location,
                CSharpSyntaxNode syntax,
                BindingDiagnosticBag diagnostics)
                : base(
                       containingType,
                       property,
                       propertyModifiers,
                       location,
                       syntax,
                       hasBlockBody: true,
                       hasExpressionBody: false,
                       isIterator: false,
                       modifiers: default,
                       MethodKind.PropertyGet,
                       usesInit: false,
                       isAutoPropertyAccessor: false,
                       isNullableAnalysisEnabled: false,
                       diagnostics)
            {
            }

            public override bool IsImplicitlyDeclared => true;

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

            internal override bool SynthesizesLoweredBoundBody => true;
            internal override ExecutableCodeBinder? TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false) => throw ExceptionUtilities.Unreachable();

            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
            {
                var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);

                try
                {
                    F.CurrentFunction = this;
                    F.CloseMethod(F.Block(F.Return(F.Typeof(ContainingType, ReturnType))));
                }
                catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
                {
                    diagnostics.Add(ex.Diagnostic);
                    F.CloseMethod(F.ThrowNull());
                }
            }
        }
    }
}
