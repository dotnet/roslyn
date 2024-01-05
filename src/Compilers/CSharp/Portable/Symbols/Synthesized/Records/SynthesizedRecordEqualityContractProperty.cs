// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

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
                modifiers: (containingType.IsSealed, containingType.BaseTypeNoUseSiteDiagnostics.IsObjectType()) switch
                {
                    (true, true) => DeclarationModifiers.Private,
                    (false, true) => DeclarationModifiers.Protected | DeclarationModifiers.Virtual,
                    (_, false) => DeclarationModifiers.Protected | DeclarationModifiers.Override
                },
                hasInitializer: false,
                isAutoProperty: false,
                isExpressionBodied: false,
                isInitOnly: false,
                RefKind.None,
                PropertyName,
                indexerNameAttributeLists: new SyntaxList<AttributeListSyntax>(),
                containingType.GetFirstLocation(),
                diagnostics)
        {
            Debug.Assert(!containingType.IsRecordStruct);
        }

        public override bool IsImplicitlyDeclared => true;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
            => new SyntaxList<AttributeListSyntax>();

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

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            var compilation = this.DeclaringCompilation;
            AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
        }

        internal static void VerifyOverridesEqualityContractFromBase(PropertySymbol overriding, BindingDiagnosticBag diagnostics)
        {
            if (overriding.ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType())
            {
                return;
            }

            bool reportAnError = false;

            if (!overriding.IsOverride)
            {
                reportAnError = true;
            }
            else
            {
                var overridden = overriding.OverriddenProperty;

                if (overridden is object &&
                    !overridden.ContainingType.Equals(overriding.ContainingType.BaseTypeNoUseSiteDiagnostics, TypeCompareKind.AllIgnoreOptions))
                {
                    reportAnError = true;
                }
            }

            if (reportAnError)
            {
                diagnostics.Add(ErrorCode.ERR_DoesNotOverrideBaseEqualityContract, overriding.GetFirstLocation(), overriding, overriding.ContainingType.BaseTypeNoUseSiteDiagnostics);
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

            internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
            {
                base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

                var compilation = this.DeclaringCompilation;
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
                Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }

            public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

            internal override bool SynthesizesLoweredBoundBody => true;
            internal override ExecutableCodeBinder? TryGetBodyBinder(BinderFactory? binderFactoryOpt = null, bool ignoreAccessibility = false) => throw ExceptionUtilities.Unreachable();

            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
            {
                var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);

                try
                {
                    F.CurrentFunction = this;
                    F.CloseMethod(F.Block(F.Return(F.Typeof(ContainingType))));
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
