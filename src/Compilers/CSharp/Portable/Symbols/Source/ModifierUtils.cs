// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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
            out bool modifierErrors,
            bool ignoreParameterModifiers = false)
        {
            var result = modifiers.ToDeclarationModifiers(ignoreParameterModifiers);
            result = CheckModifiers(result, allowedModifiers, errorLocation, diagnostics, out modifierErrors);

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
                        diagnostics.Add(ErrorCode.ERR_PartialMethodOnlyMethods, errorLocation);
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
            }
            return result;
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
                default:
                    throw ExceptionUtilities.UnexpectedValue(modifier);
            }
        }

        public static DeclarationModifiers ToDeclarationModifiers(this SyntaxTokenList modifiers, bool ignoreParameterModifiers = false)
        {
            var result = DeclarationModifiers.None;

            foreach (var modifier in modifiers)
            {
                DeclarationModifiers one;
                switch (modifier.ContextualKind())
                {
                    case SyntaxKind.AbstractKeyword:
                        one = DeclarationModifiers.Abstract;
                        break;

                    case SyntaxKind.AsyncKeyword:
                        one = DeclarationModifiers.Async;
                        break;

                    case SyntaxKind.SealedKeyword:
                        one = DeclarationModifiers.Sealed;
                        break;

                    case SyntaxKind.StaticKeyword:
                        one = DeclarationModifiers.Static;
                        break;

                    case SyntaxKind.NewKeyword:
                        one = DeclarationModifiers.New;
                        break;

                    case SyntaxKind.PublicKeyword:
                        one = DeclarationModifiers.Public;
                        break;

                    case SyntaxKind.ProtectedKeyword:
                        one = DeclarationModifiers.Protected;
                        break;

                    case SyntaxKind.InternalKeyword:
                        one = DeclarationModifiers.Internal;
                        break;

                    case SyntaxKind.PrivateKeyword:
                        one = DeclarationModifiers.Private;
                        break;

                    case SyntaxKind.ExternKeyword:
                        one = DeclarationModifiers.Extern;
                        break;

                    case SyntaxKind.ReadOnlyKeyword:
                        one = DeclarationModifiers.ReadOnly;
                        break;

                    case SyntaxKind.PartialKeyword:
                        one = DeclarationModifiers.Partial;
                        break;

                    case SyntaxKind.UnsafeKeyword:
                        one = DeclarationModifiers.Unsafe;
                        break;

                    case SyntaxKind.VirtualKeyword:
                        one = DeclarationModifiers.Virtual;
                        break;

                    case SyntaxKind.OverrideKeyword:
                        one = DeclarationModifiers.Override;
                        break;

                    case SyntaxKind.ConstKeyword:
                        one = DeclarationModifiers.Const;
                        break;

                    case SyntaxKind.FixedKeyword:
                        one = DeclarationModifiers.Fixed;
                        break;

                    case SyntaxKind.VolatileKeyword:
                        one = DeclarationModifiers.Volatile;
                        break;

                    case SyntaxKind.ThisKeyword:
                    case SyntaxKind.RefKeyword:
                    case SyntaxKind.OutKeyword:
                    case SyntaxKind.ParamsKeyword:
                        if (ignoreParameterModifiers)
                        {
                            continue;
                        }

                        goto default;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(modifier.ContextualKind());
                }

                result |= one;
            }

            // the two keywords "protected" and "internal" together are treated as one modifier.
            if ((result & DeclarationModifiers.AccessibilityMask) == (DeclarationModifiers.Protected | DeclarationModifiers.Internal))
            {
                result &= ~DeclarationModifiers.AccessibilityMask;
                result |= DeclarationModifiers.ProtectedInternal;
            }

            return result;
        }

        internal static CSDiagnosticInfo CheckAccessibility(DeclarationModifiers modifiers)
        {
            if (!IsValidAccessibility(modifiers))
            {
                // error CS0107: More than one protection modifier
                return new CSDiagnosticInfo(ErrorCode.ERR_BadMemberProtection);
            }

            return null;
        }

        // Returns declared accessibility.
        // In a case of bogus accessibility (i.e. "public private"), defaults to public.
        internal static Accessibility EffectiveAccessibility(DeclarationModifiers modifiers)
        {
            var acc = modifiers & DeclarationModifiers.AccessibilityMask;
            switch (acc)
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
                default:
                    // This happens when you have a mix of accessibilities.
                    //
                    // i.e.: public private void Foo()
                    return Accessibility.Public;
            }
        }

        internal static bool IsValidAccessibility(DeclarationModifiers modifiers)
        {
            var acc = modifiers & DeclarationModifiers.AccessibilityMask;
            switch (acc)
            {
                case DeclarationModifiers.None:
                case DeclarationModifiers.Private:
                case DeclarationModifiers.Protected:
                case DeclarationModifiers.Internal:
                case DeclarationModifiers.Public:
                case DeclarationModifiers.ProtectedInternal:
                    return true;

                default:
                    // This happens when you have a mix of accessibilities.
                    //
                    // i.e.: public private void Foo()
                    return false;
            }
        }
    }
}
