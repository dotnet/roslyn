// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceDestructorSymbol : SourceMemberMethodSymbol
    {
        private TypeWithAnnotations _lazyReturnType;

        internal SourceDestructorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            DestructorDeclarationSyntax syntax,
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics) :
            base(containingType, syntax.GetReference(), syntax.Identifier.GetLocation(), isIterator: SyntaxFacts.HasYieldOperations(syntax.Body))
        {
            const MethodKind methodKind = MethodKind.Destructor;
            Location location = this.GetFirstLocation();

            bool modifierErrors;
            var declarationModifiers = MakeModifiers(syntax.Modifiers, location, diagnostics, out modifierErrors);

            bool hasBlockBody = syntax.Body != null;
            bool isExpressionBodied = !hasBlockBody && syntax.ExpressionBody != null;
            bool hasAnyBody = hasBlockBody || isExpressionBodied;

            this.MakeFlags(
                methodKind, RefKind.None, declarationModifiers, returnsVoid: true, hasAnyBody: hasAnyBody, isExpressionBodied: isExpressionBodied, isExtensionMethod: false,
                isVarArg: false, isNullableAnalysisEnabled: isNullableAnalysisEnabled);

            if (syntax.Identifier.ValueText != containingType.Name)
            {
                diagnostics.Add(ErrorCode.ERR_BadDestructorName, syntax.Identifier.GetLocation());
            }

            if (hasBlockBody || isExpressionBodied)
            {
                if (IsExtern)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasBody, location, this);
                }
            }

            if (!modifierErrors && !hasBlockBody && !isExpressionBodied && !IsExtern)
            {
                diagnostics.Add(ErrorCode.ERR_ConcreteMissingBody, location, this);
            }

            Debug.Assert(syntax.ParameterList.Parameters.Count == 0);

            if (containingType.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_DestructorInStaticClass, location);
            }
            else if (!containingType.IsReferenceType)
            {
                diagnostics.Add(ErrorCode.ERR_OnlyClassesCanContainDestructors, location);
            }

            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
        }

        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            var bodyBinder = this.DeclaringCompilation.GetBinderFactory(syntaxReferenceOpt.SyntaxTree).GetBinder(syntax, syntax, this);
            _lazyReturnType = TypeWithAnnotations.Create(bodyBinder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax));
        }

        internal DestructorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (DestructorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            return TryGetBodyBinderFromSyntax(binderFactoryOpt, ignoreAccessibility);
        }

        internal override int ParameterCount
        {
            get { return 0; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return ImmutableArray<ParameterSymbol>.Empty; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
            => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

        public override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
            => ImmutableArray<TypeParameterConstraintKind>.Empty;

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, Location location, BindingDiagnosticBag diagnostics, out bool modifierErrors)
        {
            // Check that the set of modifiers is allowed
            const DeclarationModifiers allowedModifiers = DeclarationModifiers.Extern | DeclarationModifiers.Unsafe;
            var mods = ModifierUtils.MakeAndCheckNonTypeMemberModifiers(isOrdinaryMethod: false, isForInterfaceMember: ContainingType.IsInterface, modifiers, DeclarationModifiers.None, allowedModifiers, location, diagnostics, out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            mods = (mods & ~DeclarationModifiers.AccessibilityMask) | DeclarationModifiers.Protected; // we mark destructors protected in the symbol table

            return mods;
        }

        public override string Name
        {
            get { return WellKnownMemberNames.DestructorName; }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // destructors can't have return type attributes
            return OneOrMany.Create(this.GetSyntax().AttributeLists);
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetReturnTypeAttributeDeclarations()
        {
            // destructors can't have return type attributes
            return OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return true;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return (object)this.ContainingType.BaseTypeNoUseSiteDiagnostics == null;
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }
    }
}
