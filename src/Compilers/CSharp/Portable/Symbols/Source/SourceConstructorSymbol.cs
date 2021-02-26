﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceConstructorSymbol : SourceConstructorSymbolBase
    {
        private readonly bool _isExpressionBodied;
        private readonly bool _hasThisInitializer;

        public static SourceConstructorSymbol CreateConstructorSymbol(
            SourceMemberContainerTypeSymbol containingType,
            ConstructorDeclarationSyntax syntax,
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics)
        {
            var methodKind = syntax.Modifiers.Any(SyntaxKind.StaticKeyword) ? MethodKind.StaticConstructor : MethodKind.Constructor;
            return new SourceConstructorSymbol(containingType, syntax.Identifier.GetLocation(), syntax, methodKind, isNullableAnalysisEnabled, diagnostics);
        }

        private SourceConstructorSymbol(
             SourceMemberContainerTypeSymbol containingType,
             Location location,
             ConstructorDeclarationSyntax syntax,
             MethodKind methodKind,
             bool isNullableAnalysisEnabled,
             BindingDiagnosticBag diagnostics) :
             base(containingType, location, syntax, SyntaxFacts.HasYieldOperations(syntax))
        {
            bool hasBlockBody = syntax.Body != null;
            _isExpressionBodied = !hasBlockBody && syntax.ExpressionBody != null;
            bool hasBody = hasBlockBody || _isExpressionBodied;

            _hasThisInitializer = syntax.Initializer?.Kind() == SyntaxKind.ThisConstructorInitializer;

            bool modifierErrors;
            var declarationModifiers = this.MakeModifiers(syntax.Modifiers, methodKind, hasBody, location, diagnostics, out modifierErrors);
            this.MakeFlags(methodKind, declarationModifiers, returnsVoid: true, isExtensionMethod: false, isNullableAnalysisEnabled: isNullableAnalysisEnabled);

            if (syntax.Identifier.ValueText != containingType.Name)
            {
                // This is probably a method declaration with the type missing.
                diagnostics.Add(ErrorCode.ERR_MemberNeedsType, location);
            }

            if (IsExtern)
            {
                if (methodKind == MethodKind.Constructor && syntax.Initializer != null)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasConstructorInitializer, location, this);
                }

                if (hasBody)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasBody, location, this);
                }
            }

            if (methodKind == MethodKind.StaticConstructor)
            {
                CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody, diagnostics);
            }

            var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers, this, isExplicitInterfaceImplementation: false);
            if (info != null)
            {
                diagnostics.Add(info, location);
            }

            if (!modifierErrors)
            {
                this.CheckModifiers(methodKind, hasBody, location, diagnostics);
            }

            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
        }

        internal ConstructorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (ConstructorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        protected override ParameterListSyntax GetParameterList()
        {
            return GetSyntax().ParameterList;
        }

        protected override CSharpSyntaxNode GetInitializer()
        {
            return GetSyntax().Initializer;
        }

        private DeclarationModifiers MakeModifiers(SyntaxTokenList modifiers, MethodKind methodKind, bool hasBody, Location location, BindingDiagnosticBag diagnostics, out bool modifierErrors)
        {
            var defaultAccess = (methodKind == MethodKind.StaticConstructor) ? DeclarationModifiers.None : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            const DeclarationModifiers allowedModifiers =
                DeclarationModifiers.AccessibilityMask |
                DeclarationModifiers.Static |
                DeclarationModifiers.Extern |
                DeclarationModifiers.Unsafe;

            var mods = ModifierUtils.MakeAndCheckNontypeMemberModifiers(modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            this.CheckUnsafeModifier(mods, diagnostics);

            if (methodKind == MethodKind.StaticConstructor)
            {
                if ((mods & DeclarationModifiers.AccessibilityMask) != 0)
                {
                    diagnostics.Add(ErrorCode.ERR_StaticConstructorWithAccessModifiers, location, this);
                    mods = mods & ~DeclarationModifiers.AccessibilityMask;
                    modifierErrors = true;
                }

                mods |= DeclarationModifiers.Private; // we mark static constructors private in the symbol table

                if (this.ContainingType.IsInterface)
                {
                    ModifierUtils.ReportDefaultInterfaceImplementationModifiers(hasBody, mods,
                                                                                DeclarationModifiers.Extern,
                                                                                location, diagnostics);
                }
            }

            return mods;
        }

        private void CheckModifiers(MethodKind methodKind, bool hasBody, Location location, BindingDiagnosticBag diagnostics)
        {
            if (!hasBody && !IsExtern)
            {
                diagnostics.Add(ErrorCode.ERR_ConcreteMissingBody, location, this);
            }
            else if (ContainingType.IsSealed && this.DeclaredAccessibility.HasProtected() && !this.IsOverride)
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), location, this);
            }
            else if (ContainingType.IsStatic && methodKind == MethodKind.Constructor)
            {
                diagnostics.Add(ErrorCode.ERR_ConstructorInStaticClass, location);
            }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return OneOrMany.Create(((ConstructorDeclarationSyntax)this.SyntaxNode).AttributeLists);
        }

        internal override bool IsExpressionBodied
        {
            get
            {
                return _isExpressionBodied;
            }
        }

        internal override bool IsNullableAnalysisEnabled()
        {
            return _hasThisInitializer ?
                flags.IsNullableAnalysisEnabled :
                ((SourceMemberContainerTypeSymbol)ContainingType).IsNullableEnabledForConstructorsAndInitializers(IsStatic);
        }

        protected override bool AllowRefOrOut
        {
            get
            {
                return true;
            }
        }

        protected override bool IsWithinExpressionOrBlockBody(int position, out int offset)
        {
            ConstructorDeclarationSyntax ctorSyntax = GetSyntax();
            if (ctorSyntax.Body?.Span.Contains(position) == true)
            {
                offset = position - ctorSyntax.Body.Span.Start;
                return true;
            }
            else if (ctorSyntax.ExpressionBody?.Span.Contains(position) == true)
            {
                offset = position - ctorSyntax.ExpressionBody.Span.Start;
                return true;
            }

            offset = -1;
            return false;
        }
    }
}
