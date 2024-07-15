// Licensed to the .NET Foundation under one or more agreements.
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
             base(containingType, location, syntax, SyntaxFacts.HasYieldOperations(syntax),
                  MakeModifiersAndFlags(
                      containingType, syntax, methodKind, isNullableAnalysisEnabled, syntax.Initializer?.Kind() == SyntaxKind.ThisConstructorInitializer, location, diagnostics, out bool modifierErrors, out bool report_ERR_StaticConstructorWithAccessModifiers))
        {
            this.CheckUnsafeModifier(DeclarationModifiers, diagnostics);

            if (report_ERR_StaticConstructorWithAccessModifiers)
            {
                diagnostics.Add(ErrorCode.ERR_StaticConstructorWithAccessModifiers, location, this);
            }

            if (syntax.Identifier.ValueText != containingType.Name)
            {
                // This is probably a method declaration with the type missing.
                diagnostics.Add(ErrorCode.ERR_MemberNeedsType, location);
            }

            bool hasAnyBody = syntax.HasAnyBody();

            if (IsExtern)
            {
                if (methodKind == MethodKind.Constructor && syntax.Initializer != null)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasConstructorInitializer, location, this);
                }

                if (hasAnyBody)
                {
                    diagnostics.Add(ErrorCode.ERR_ExternHasBody, location, this);
                }
            }

            if (methodKind == MethodKind.StaticConstructor)
            {
                CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasAnyBody, diagnostics);
            }

            ModifierUtils.CheckAccessibility(this.DeclarationModifiers, this, isExplicitInterfaceImplementation: false, diagnostics, location);

            if (!modifierErrors)
            {
                this.CheckModifiers(methodKind, hasAnyBody, location, diagnostics);
            }

            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
        }

        private static (DeclarationModifiers, Flags) MakeModifiersAndFlags(
            NamedTypeSymbol containingType,
            ConstructorDeclarationSyntax syntax,
            MethodKind methodKind,
            bool isNullableAnalysisEnabled,
            bool hasThisInitializer,
            Location location,
            BindingDiagnosticBag diagnostics,
            out bool modifierErrors,
            out bool report_ERR_StaticConstructorWithAccessModifiers)
        {
            DeclarationModifiers declarationModifiers = MakeModifiers(containingType, syntax, methodKind, syntax.HasAnyBody(), location, diagnostics, out modifierErrors, out report_ERR_StaticConstructorWithAccessModifiers);
            Flags flags = MakeFlags(
                methodKind, RefKind.None, declarationModifiers, returnsVoid: true, returnsVoidIsSet: true,
                isExpressionBodied: syntax.IsExpressionBodied(), isExtensionMethod: false, isVarArg: syntax.IsVarArg(),
                isNullableAnalysisEnabled: isNullableAnalysisEnabled, isExplicitInterfaceImplementation: false,
                hasThisInitializer: hasThisInitializer);

            return (declarationModifiers, flags);
        }

        internal ConstructorDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (ConstructorDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        internal override ExecutableCodeBinder TryGetBodyBinder(BinderFactory binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            return TryGetBodyBinderFromSyntax(binderFactoryOpt, ignoreAccessibility);
        }

        protected override ParameterListSyntax GetParameterList()
        {
            return GetSyntax().ParameterList;
        }

        protected override CSharpSyntaxNode GetInitializer()
        {
            return GetSyntax().Initializer;
        }

        private static DeclarationModifiers MakeModifiers(
            NamedTypeSymbol containingType, ConstructorDeclarationSyntax syntax, MethodKind methodKind, bool hasBody, Location location, BindingDiagnosticBag diagnostics,
            out bool modifierErrors, out bool report_ERR_StaticConstructorWithAccessModifiers)
        {
            var defaultAccess = (methodKind == MethodKind.StaticConstructor) ? DeclarationModifiers.None : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            const DeclarationModifiers allowedModifiers =
                DeclarationModifiers.AccessibilityMask |
                DeclarationModifiers.Static |
                DeclarationModifiers.Extern |
                DeclarationModifiers.Unsafe;

            bool isInterface = containingType.IsInterface;
            var mods = ModifierUtils.MakeAndCheckNonTypeMemberModifiers(isOrdinaryMethod: false, isForInterfaceMember: isInterface, syntax.Modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors);

            report_ERR_StaticConstructorWithAccessModifiers = false;
            if (methodKind == MethodKind.StaticConstructor)
            {
                // Don't report ERR_StaticConstructorWithAccessModifiers if the ctor symbol name doesn't match the containing type name.
                // This avoids extra unnecessary errors.
                // There will already be a diagnostic saying Method must have a return type.
                if ((mods & DeclarationModifiers.AccessibilityMask) != 0 &&
                    containingType.Name == syntax.Identifier.ValueText)
                {
                    mods = mods & ~DeclarationModifiers.AccessibilityMask;
                    report_ERR_StaticConstructorWithAccessModifiers = true;
                    modifierErrors = true;
                }

                mods |= DeclarationModifiers.Private; // we mark static constructors private in the symbol table

                if (isInterface)
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

        internal override bool IsNullableAnalysisEnabled()
            => flags.HasThisInitializer
                ? flags.IsNullableAnalysisEnabled
                : ((SourceMemberContainerTypeSymbol)ContainingType).IsNullableEnabledForConstructorsAndInitializers(IsStatic);

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
