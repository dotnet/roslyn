// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceDestructorSymbol : SourceMethodSymbol
    {
        private TypeSymbolWithAnnotations _lazyReturnType;

        internal SourceDestructorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            DestructorDeclarationSyntax syntax,
            DiagnosticBag diagnostics) :
            base(containingType, syntax.GetReference(), syntax.Body?.GetReference(), syntax.Identifier.GetLocation())
        {
            const MethodKind methodKind = MethodKind.Destructor;
            Location location = this.Locations[0];

            bool modifierErrors;
            var declarationModifiers = MakeModifiers(syntax.Modifiers, location, diagnostics, out modifierErrors);
            this.MakeFlags(methodKind, declarationModifiers, returnsVoid: true, isExtensionMethod: false);

            var bodyOpt = syntax.Body;
            if (bodyOpt != null)
            {
                if (IsExtern)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasBody, location, this);
                }
            }

            if (!modifierErrors && bodySyntaxReferenceOpt == null && !IsExtern)
            {
                diagnostics.Add(ErrorCode.ERR_ConcreteMissingBody, location, this);
            }

            Debug.Assert(syntax.ParameterList.Parameters.Count == 0);

            if (containingType.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_DestructorInStaticClass, location, this);
            }
            else if (!containingType.IsReferenceType)
            {
                diagnostics.Add(ErrorCode.ERR_OnlyClassesCanContainDestructors, location, this);
            }
        }

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            var bodyBinder = this.DeclaringCompilation.GetBinder(syntaxReferenceOpt);
            _lazyReturnType = TypeSymbolWithAnnotations.Create(bodyBinder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax));
        }

        internal DestructorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (DestructorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        public override bool IsVararg
        {
            get { return false; }
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

        internal override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeSymbolWithAnnotations ReturnType
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, Location location, DiagnosticBag diagnostics, out bool modifierErrors)
        {
            // Check that the set of modifiers is allowed
            const DeclarationModifiers allowedModifiers = DeclarationModifiers.Extern | DeclarationModifiers.Unsafe;
            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(modifiers, DeclarationModifiers.None, allowedModifiers, location, diagnostics, out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            mods = (mods & ~DeclarationModifiers.AccessibilityMask) | DeclarationModifiers.Protected; // we mark destructors protected in the symbol table

            return mods;
        }

        public override string Name
        {
            get { return WellKnownMemberNames.DestructorName; }
        }

        internal override bool IsExpressionBodied
        {
            get { return false; }
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
