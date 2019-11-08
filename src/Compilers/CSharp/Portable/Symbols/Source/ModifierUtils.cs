// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ModifierUtils
    {
        internal static DeclarationModifiers MakeAndCheckNontypeMemberModifiers(
            SyntaxTokenList modifiers,
            DeclarationModifiers defaultAccess,
            DeclarationModifiers allowedModifiers,
            Location errorLocation,
            DiagnosticBag diagnostics,
            out bool modifierErrors)
        {
            var result = modifiers.ToDeclarationModifiers(diagnostics);
            result = CheckModifiers(result, allowedModifiers, errorLocation, diagnostics, modifiers, out modifierErrors);

            if ((result & DeclarationModifiers.AccessibilityMask) == 0)
            {
                result |= defaultAccess;
            }

            return result;
        }

        internal static DeclarationModifiers CheckModifiers(
            DeclarationModifiers modifiers,
            DeclarationModifiers allowedModifiers,
            Location errorLocation,
            DiagnosticBag diagnostics,
            SyntaxTokenList? modifierTokens,
            out bool modifierErrors)
        {
            modifierErrors = false;
            DeclarationModifiers errorModifiers = modifiers & ~allowedModifiers;
            DeclarationModifiers result = modifiers & allowedModifiers;

            while (errorModifiers != DeclarationModifiers.None)
            {
                DeclarationModifiers oneError = errorModifiers & ~(errorModifiers - 1);
                Debug.Assert(oneError != DeclarationModifiers.None);
                errorModifiers = errorModifiers & ~oneError;

                switch (oneError)
                {
                    case DeclarationModifiers.Partial:
                        // Provide a specialized error message in the case of partial.
                        ReportPartialError(errorLocation, diagnostics, modifierTokens);
                        break;

                    default:
                        diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, ConvertSingleModifierToSyntaxText(oneError));
                        break;
                }

                modifierErrors = true;
            }

            bool isMethod = (allowedModifiers & (DeclarationModifiers.Partial | DeclarationModifiers.Virtual)) == (DeclarationModifiers.Partial | DeclarationModifiers.Virtual);
            if (isMethod && ((result & (DeclarationModifiers.Partial | DeclarationModifiers.Private)) == (DeclarationModifiers.Partial | DeclarationModifiers.Private)))
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodInvalidModifier, errorLocation);
                modifierErrors = true;
            }

            if ((result & DeclarationModifiers.PrivateProtected) != 0)
            {
                modifierErrors |= !Binder.CheckFeatureAvailability(errorLocation.SourceTree, MessageID.IDS_FeaturePrivateProtected, diagnostics, errorLocation);
            }

            return result;
        }

        private static void ReportPartialError(Location errorLocation, DiagnosticBag diagnostics, SyntaxTokenList? modifierTokens)
        {
            // If we can find the 'partial' token, report it on that.
            if (modifierTokens != null)
            {
                var partialToken = modifierTokens.Value.FirstOrDefault(SyntaxKind.PartialKeyword);
                if (partialToken != default)
                {
                    diagnostics.Add(ErrorCode.ERR_PartialMisplaced, partialToken.GetLocation());
                    return;
                }
            }

            diagnostics.Add(ErrorCode.ERR_PartialMisplaced, errorLocation);
        }

        internal static void ReportDefaultInterfaceImplementationModifiers(
            bool hasBody,
            DeclarationModifiers modifiers,
            DeclarationModifiers defaultInterfaceImplementationModifiers,
            Location errorLocation,
            DiagnosticBag diagnostics)
        {
            if (!hasBody && (modifiers & defaultInterfaceImplementationModifiers) != 0)
            {
                LanguageVersion availableVersion = ((CSharpParseOptions)errorLocation.SourceTree.Options).LanguageVersion;
                LanguageVersion requiredVersion = MessageID.IDS_DefaultInterfaceImplementation.RequiredVersion();
                if (availableVersion < requiredVersion)
                {
                    DeclarationModifiers errorModifiers = modifiers & defaultInterfaceImplementationModifiers;
                    var requiredVersionArgument = new CSharpRequiredLanguageVersion(requiredVersion);
                    var availableVersionArgument = availableVersion.ToDisplayString();
                    while (errorModifiers != DeclarationModifiers.None)
                    {
                        DeclarationModifiers oneError = errorModifiers & ~(errorModifiers - 1);
                        Debug.Assert(oneError != DeclarationModifiers.None);
                        errorModifiers = errorModifiers & ~oneError;
                        diagnostics.Add(ErrorCode.ERR_DefaultInterfaceImplementationModifier, errorLocation,
                                        ConvertSingleModifierToSyntaxText(oneError),
                                        availableVersionArgument,
                                        requiredVersionArgument);
                    }
                }
            }
        }

        internal static DeclarationModifiers AdjustModifiersForAnInterfaceMember(DeclarationModifiers mods, bool hasBody, bool isExplicitInterfaceImplementation)
        {
            if (isExplicitInterfaceImplementation)
            {
                if ((mods & DeclarationModifiers.Abstract) != 0)
                {
                    mods |= DeclarationModifiers.Sealed;
                }
            }
            else if ((mods & (DeclarationModifiers.Static | DeclarationModifiers.Private | DeclarationModifiers.Partial | DeclarationModifiers.Virtual | DeclarationModifiers.Abstract)) == 0)
            {
                Debug.Assert(!isExplicitInterfaceImplementation);

                if (hasBody || (mods & (DeclarationModifiers.Extern | DeclarationModifiers.Sealed)) != 0)
                {
                    if ((mods & DeclarationModifiers.Sealed) == 0)
                    {
                        mods |= DeclarationModifiers.Virtual;
                    }
                    else
                    {
                        mods &= ~DeclarationModifiers.Sealed;
                    }
                }
                else
                {
                    mods |= DeclarationModifiers.Abstract;
                }
            }

            if ((mods & DeclarationModifiers.AccessibilityMask) == 0)
            {
                if ((mods & DeclarationModifiers.Partial) == 0 && !isExplicitInterfaceImplementation)
                {
                    mods |= DeclarationModifiers.Public;
                }
                else
                {
                    mods |= DeclarationModifiers.Private;
                }
            }

            return mods;
        }

        private static string ConvertSingleModifierToSyntaxText(DeclarationModifiers modifier)
        {
            switch (modifier)
            {
                case DeclarationModifiers.Abstract:
                    return SyntaxFacts.GetText(SyntaxKind.AbstractKeyword);
                case DeclarationModifiers.Sealed:
                    return SyntaxFacts.GetText(SyntaxKind.SealedKeyword);
                case DeclarationModifiers.Static:
                    return SyntaxFacts.GetText(SyntaxKind.StaticKeyword);
                case DeclarationModifiers.New:
                    return SyntaxFacts.GetText(SyntaxKind.NewKeyword);
                case DeclarationModifiers.Public:
                    return SyntaxFacts.GetText(SyntaxKind.PublicKeyword);
                case DeclarationModifiers.Protected:
                    return SyntaxFacts.GetText(SyntaxKind.ProtectedKeyword);
                case DeclarationModifiers.Internal:
                    return SyntaxFacts.GetText(SyntaxKind.InternalKeyword);
                case DeclarationModifiers.ProtectedInternal:
                    return SyntaxFacts.GetText(SyntaxKind.ProtectedKeyword) + " " + SyntaxFacts.GetText(SyntaxKind.InternalKeyword);
                case DeclarationModifiers.Private:
                    return SyntaxFacts.GetText(SyntaxKind.PrivateKeyword);
                case DeclarationModifiers.PrivateProtected:
                    return SyntaxFacts.GetText(SyntaxKind.PrivateKeyword) + " " + SyntaxFacts.GetText(SyntaxKind.ProtectedKeyword);
                case DeclarationModifiers.ReadOnly:
                    return SyntaxFacts.GetText(SyntaxKind.ReadOnlyKeyword);
                case DeclarationModifiers.Const:
                    return SyntaxFacts.GetText(SyntaxKind.ConstKeyword);
                case DeclarationModifiers.Volatile:
                    return SyntaxFacts.GetText(SyntaxKind.VolatileKeyword);
                case DeclarationModifiers.Extern:
                    return SyntaxFacts.GetText(SyntaxKind.ExternKeyword);
                case DeclarationModifiers.Partial:
                    return SyntaxFacts.GetText(SyntaxKind.PartialKeyword);
                case DeclarationModifiers.Unsafe:
                    return SyntaxFacts.GetText(SyntaxKind.UnsafeKeyword);
                case DeclarationModifiers.Fixed:
                    return SyntaxFacts.GetText(SyntaxKind.FixedKeyword);
                case DeclarationModifiers.Virtual:
                    return SyntaxFacts.GetText(SyntaxKind.VirtualKeyword);
                case DeclarationModifiers.Override:
                    return SyntaxFacts.GetText(SyntaxKind.OverrideKeyword);
                case DeclarationModifiers.Async:
                    return SyntaxFacts.GetText(SyntaxKind.AsyncKeyword);
                case DeclarationModifiers.Ref:
                    return SyntaxFacts.GetText(SyntaxKind.RefKeyword);
                default:
                    throw ExceptionUtilities.UnexpectedValue(modifier);
            }
        }

        private static DeclarationModifiers ToDeclarationModifier(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.AbstractKeyword:
                    return DeclarationModifiers.Abstract;
                case SyntaxKind.AsyncKeyword:
                    return DeclarationModifiers.Async;
                case SyntaxKind.SealedKeyword:
                    return DeclarationModifiers.Sealed;
                case SyntaxKind.StaticKeyword:
                    return DeclarationModifiers.Static;
                case SyntaxKind.NewKeyword:
                    return DeclarationModifiers.New;
                case SyntaxKind.PublicKeyword:
                    return DeclarationModifiers.Public;
                case SyntaxKind.ProtectedKeyword:
                    return DeclarationModifiers.Protected;
                case SyntaxKind.InternalKeyword:
                    return DeclarationModifiers.Internal;
                case SyntaxKind.PrivateKeyword:
                    return DeclarationModifiers.Private;
                case SyntaxKind.ExternKeyword:
                    return DeclarationModifiers.Extern;
                case SyntaxKind.ReadOnlyKeyword:
                    return DeclarationModifiers.ReadOnly;
                case SyntaxKind.PartialKeyword:
                    return DeclarationModifiers.Partial;
                case SyntaxKind.UnsafeKeyword:
                    return DeclarationModifiers.Unsafe;
                case SyntaxKind.VirtualKeyword:
                    return DeclarationModifiers.Virtual;
                case SyntaxKind.OverrideKeyword:
                    return DeclarationModifiers.Override;
                case SyntaxKind.ConstKeyword:
                    return DeclarationModifiers.Const;
                case SyntaxKind.FixedKeyword:
                    return DeclarationModifiers.Fixed;
                case SyntaxKind.VolatileKeyword:
                    return DeclarationModifiers.Volatile;
                case SyntaxKind.RefKeyword:
                    return DeclarationModifiers.Ref;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        public static DeclarationModifiers ToDeclarationModifiers(
            this SyntaxTokenList modifiers, DiagnosticBag diagnostics)
        {
            var result = DeclarationModifiers.None;
            bool seenNoDuplicates = true;
            bool seenNoAccessibilityDuplicates = true;

            foreach (var modifier in modifiers)
            {
                DeclarationModifiers one = ToDeclarationModifier(modifier.ContextualKind());

                ReportDuplicateModifiers(
                    modifier, one, result,
                    ref seenNoDuplicates, ref seenNoAccessibilityDuplicates,
                    diagnostics);

                result |= one;
            }

            switch (result & DeclarationModifiers.AccessibilityMask)
            {
                case DeclarationModifiers.Protected | DeclarationModifiers.Internal:
                    // the two keywords "protected" and "internal" together are treated as one modifier.
                    result &= ~DeclarationModifiers.AccessibilityMask;
                    result |= DeclarationModifiers.ProtectedInternal;
                    break;

                case DeclarationModifiers.Private | DeclarationModifiers.Protected:
                    // the two keywords "private" and "protected" together are treated as one modifier.
                    result &= ~DeclarationModifiers.AccessibilityMask;
                    result |= DeclarationModifiers.PrivateProtected;
                    break;
            }

            return result;
        }

        private static void ReportDuplicateModifiers(
            SyntaxToken modifierToken,
            DeclarationModifiers modifierKind,
            DeclarationModifiers allModifiers,
            ref bool seenNoDuplicates,
            ref bool seenNoAccessibilityDuplicates,
            DiagnosticBag diagnostics)
        {
            if ((allModifiers & modifierKind) != 0)
            {
                if (seenNoDuplicates)
                {
                    diagnostics.Add(
                        ErrorCode.ERR_DuplicateModifier,
                        modifierToken.GetLocation(),
                        SyntaxFacts.GetText(modifierToken.Kind()));
                    seenNoDuplicates = false;
                }
            }
        }

        internal static CSDiagnosticInfo CheckAccessibility(DeclarationModifiers modifiers, Symbol symbol, bool isExplicitInterfaceImplementation)
        {
            if (!IsValidAccessibility(modifiers))
            {
                // error CS0107: More than one protection modifier
                return new CSDiagnosticInfo(ErrorCode.ERR_BadMemberProtection);
            }

            if (!isExplicitInterfaceImplementation &&
                (symbol.Kind != SymbolKind.Method || (modifiers & DeclarationModifiers.Partial) == 0) &&
                (modifiers & DeclarationModifiers.Static) == 0)
            {
                switch (modifiers & DeclarationModifiers.AccessibilityMask)
                {
                    case DeclarationModifiers.Protected:
                    case DeclarationModifiers.ProtectedInternal:
                    case DeclarationModifiers.PrivateProtected:

                        if (symbol is { ContainingType: { IsInterface: true }, ContainingAssembly: { RuntimeSupportsDefaultInterfaceImplementation: false } })
                        {
                            return new CSDiagnosticInfo(ErrorCode.ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember);
                        }
                        break;
                }
            }

            return null;
        }

        // Returns declared accessibility.
        // In a case of bogus accessibility (i.e. "public private"), defaults to public.
        internal static Accessibility EffectiveAccessibility(DeclarationModifiers modifiers)
        {
            switch (modifiers & DeclarationModifiers.AccessibilityMask)
            {
                case DeclarationModifiers.None:
                    return Accessibility.NotApplicable; // for explicit interface implementation
                case DeclarationModifiers.Private:
                    return Accessibility.Private;
                case DeclarationModifiers.Protected:
                    return Accessibility.Protected;
                case DeclarationModifiers.Internal:
                    return Accessibility.Internal;
                case DeclarationModifiers.Public:
                    return Accessibility.Public;
                case DeclarationModifiers.ProtectedInternal:
                    return Accessibility.ProtectedOrInternal;
                case DeclarationModifiers.PrivateProtected:
                    return Accessibility.ProtectedAndInternal;
                default:
                    // This happens when you have a mix of accessibilities.
                    //
                    // i.e.: public private void Goo()
                    return Accessibility.Public;
            }
        }

        internal static bool IsValidAccessibility(DeclarationModifiers modifiers)
        {
            switch (modifiers & DeclarationModifiers.AccessibilityMask)
            {
                case DeclarationModifiers.None:
                case DeclarationModifiers.Private:
                case DeclarationModifiers.Protected:
                case DeclarationModifiers.Internal:
                case DeclarationModifiers.Public:
                case DeclarationModifiers.ProtectedInternal:
                case DeclarationModifiers.PrivateProtected:
                    return true;

                default:
                    // This happens when you have a mix of accessibilities.
                    //
                    // i.e.: public private void Goo()
                    return false;
            }
        }
    }
}
