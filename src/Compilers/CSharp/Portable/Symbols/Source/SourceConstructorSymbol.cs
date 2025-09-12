// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceConstructorSymbol : SourceConstructorSymbolBase
    {
        private SourceConstructorSymbol? _otherPartOfPartial;

#nullable disable
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
                if (syntax.Identifier.Text == "extension")
                {
                    bool reported = !MessageID.IDS_FeatureExtensions.CheckFeatureAvailability(diagnostics, syntax);
                    Debug.Assert(reported);
                }
                else
                {
                    // This is probably a method declaration with the type missing.
                    diagnostics.Add(ErrorCode.ERR_MemberNeedsType, location);
                }
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

            if (IsPartialDefinition && syntax.Initializer is { } initializer)
            {
                diagnostics.Add(ErrorCode.ERR_PartialConstructorInitializer, initializer, this);
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
            bool hasAnyBody = syntax.HasAnyBody();
            DeclarationModifiers declarationModifiers = MakeModifiers(containingType, syntax, methodKind, hasAnyBody, location, diagnostics, out modifierErrors, out bool hasExplicitAccessModifier, out report_ERR_StaticConstructorWithAccessModifiers);
            Flags flags = new Flags(
                methodKind, RefKind.None, declarationModifiers, returnsVoid: true, returnsVoidIsSet: true, hasAnyBody: hasAnyBody,
                isExpressionBodied: syntax.IsExpressionBodied(), isExtensionMethod: false, isVararg: syntax.IsVarArg(),
                isNullableAnalysisEnabled: isNullableAnalysisEnabled, isExplicitInterfaceImplementation: false,
                hasThisInitializer: hasThisInitializer, hasExplicitAccessModifier: hasExplicitAccessModifier);

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
            out bool modifierErrors, out bool hasExplicitAccessModifier, out bool report_ERR_StaticConstructorWithAccessModifiers)
        {
            var defaultAccess = (methodKind == MethodKind.StaticConstructor) ? DeclarationModifiers.None : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            DeclarationModifiers allowedModifiers =
                DeclarationModifiers.AccessibilityMask |
                DeclarationModifiers.Static |
                DeclarationModifiers.Extern |
                DeclarationModifiers.Unsafe;

            if (methodKind == MethodKind.Constructor)
            {
                allowedModifiers |= DeclarationModifiers.Partial;
            }

            bool isInterface = containingType.IsInterface;
            var mods = ModifierUtils.MakeAndCheckNonTypeMemberModifiers(isOrdinaryMethod: false, isForInterfaceMember: isInterface, syntax.Modifiers, defaultAccess, allowedModifiers, location, diagnostics, out modifierErrors, out hasExplicitAccessModifier);

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
            if (!hasBody && !IsExtern && !IsPartial)
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
            else if (IsPartial && !ContainingType.IsPartial())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberOnlyInPartialClass, location);
            }
        }

#nullable enable
        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // Attributes on partial constructors are owned by the definition part.
            // If this symbol has a non-null PartialDefinitionPart, we should have accessed this method through that definition symbol instead.
            Debug.Assert(PartialDefinitionPart is null);

            if (SourcePartialImplementationPart is { } implementationPart)
            {
                return OneOrMany.Create(
                    this.AttributeDeclarationSyntaxList,
                    implementationPart.AttributeDeclarationSyntaxList);
            }

            return OneOrMany.Create(this.AttributeDeclarationSyntaxList);
        }

        private SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
        {
            get
            {
                if (this.ContainingType is SourceMemberContainerTypeSymbol { AnyMemberHasAttributes: true })
                {
                    return this.GetSyntax().AttributeLists;
                }

                return default;
            }
        }

        protected override SourceMemberMethodSymbol? BoundAttributesSource => SourcePartialDefinitionPart;
#nullable disable

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

#nullable enable
        internal sealed override void ForceComplete(SourceLocation? locationOpt, Predicate<Symbol>? filter, CancellationToken cancellationToken)
        {
            SourcePartialImplementationPart?.ForceComplete(locationOpt, filter, cancellationToken);
            base.ForceComplete(locationOpt, filter, cancellationToken);
        }

        protected override void PartialConstructorChecks(BindingDiagnosticBag diagnostics)
        {
            if (SourcePartialImplementationPart is { } implementation)
            {
                PartialConstructorChecks(implementation, diagnostics);
            }
        }

        private void PartialConstructorChecks(SourceConstructorSymbol implementation, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(this.IsPartialDefinition);
            Debug.Assert(!ReferenceEquals(this, implementation));
            Debug.Assert(ReferenceEquals(this.OtherPartOfPartial, implementation));

            if (MemberSignatureComparer.ConsideringTupleNamesCreatesDifference(this, implementation))
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberInconsistentTupleNames, implementation.GetFirstLocation(), this, implementation);
            }
            else if (!MemberSignatureComparer.PartialMethodsStrictComparer.Equals(this, implementation)
                || !Parameters.SequenceEqual(implementation.Parameters, static (a, b) => a.Name == b.Name))
            {
                diagnostics.Add(ErrorCode.WRN_PartialMemberSignatureDifference, implementation.GetFirstLocation(),
                    new FormattedSymbol(this, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    new FormattedSymbol(implementation, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }

            if (IsUnsafe != implementation.IsUnsafe && this.CompilationAllowsUnsafe())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberUnsafeDifference, implementation.GetFirstLocation());
            }

            if (this.IsParams() != implementation.IsParams())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberParamsDifference, implementation.GetFirstLocation());
            }

            if (DeclaredAccessibility != implementation.DeclaredAccessibility
                || HasExplicitAccessModifier != implementation.HasExplicitAccessModifier)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberAccessibilityDifference, implementation.GetFirstLocation());
            }

            Debug.Assert(this.ParameterCount == implementation.ParameterCount);
            for (var i = 0; i < this.ParameterCount; i++)
            {
                // An error is only reported for a modifier difference here, regardless of whether the difference is safe or not.
                // Presence of UnscopedRefAttribute is also not considered when checking partial signatures, because when the attribute is used, it will affect both parts the same way.
                var definitionParameter = (SourceParameterSymbol)this.Parameters[i];
                var implementationParameter = (SourceParameterSymbol)implementation.Parameters[i];
                if (definitionParameter.DeclaredScope != implementationParameter.DeclaredScope)
                {
                    diagnostics.Add(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, implementation.GetFirstLocation(), new FormattedSymbol(implementation.Parameters[i], SymbolDisplayFormat.ShortFormat));
                }
            }
        }

        public sealed override bool IsExtern => PartialImplementationPart is { } implementation ? implementation.IsExtern : HasExternModifier;

        private bool HasAnyBody => flags.HasAnyBody;

        private bool HasExplicitAccessModifier => flags.HasExplicitAccessModifier;

        internal bool IsPartialDefinition => IsPartial && !HasAnyBody && !HasExternModifier;

        internal bool IsPartialImplementation => IsPartial && (HasAnyBody || HasExternModifier);

        internal SourceConstructorSymbol? OtherPartOfPartial => _otherPartOfPartial;

        internal SourceConstructorSymbol? SourcePartialDefinitionPart => IsPartialImplementation ? OtherPartOfPartial : null;

        internal SourceConstructorSymbol? SourcePartialImplementationPart => IsPartialDefinition ? OtherPartOfPartial : null;

        public sealed override MethodSymbol? PartialDefinitionPart => SourcePartialDefinitionPart;

        public sealed override MethodSymbol? PartialImplementationPart => SourcePartialImplementationPart;

        internal static void InitializePartialConstructorParts(SourceConstructorSymbol definition, SourceConstructorSymbol implementation)
        {
            Debug.Assert(definition.IsPartialDefinition);
            Debug.Assert(implementation.IsPartialImplementation);

            Debug.Assert(definition._otherPartOfPartial is not { } alreadySetImplPart || ReferenceEquals(alreadySetImplPart, implementation));
            Debug.Assert(implementation._otherPartOfPartial is not { } alreadySetDefPart || ReferenceEquals(alreadySetDefPart, definition));

            definition._otherPartOfPartial = implementation;
            implementation._otherPartOfPartial = definition;

            Debug.Assert(ReferenceEquals(definition._otherPartOfPartial, implementation));
            Debug.Assert(ReferenceEquals(implementation._otherPartOfPartial, definition));
        }
    }
}
