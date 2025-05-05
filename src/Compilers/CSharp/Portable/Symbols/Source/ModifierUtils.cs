// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static class ModifierUtils
    {
        internal static DeclarationModifiers MakeAndCheckNonTypeMemberModifiers(
            bool isOrdinaryMethod,
            bool isForInterfaceMember,
            SyntaxTokenList modifiers,
            DeclarationModifiers defaultAccess,
            DeclarationModifiers allowedModifiers,
            Location errorLocation,
            BindingDiagnosticBag diagnostics,
            out bool modifierErrors,
            out bool hasExplicitAccessModifier)
        {
            var result = modifiers.ToDeclarationModifiers(isForTypeDeclaration: false, diagnostics.DiagnosticBag ?? new DiagnosticBag(), isOrdinaryMethod: isOrdinaryMethod);
            result = CheckModifiers(isForTypeDeclaration: false, isForInterfaceMember, result, allowedModifiers, errorLocation, diagnostics, modifiers, out modifierErrors);

            var readonlyToken = modifiers.FirstOrDefault(SyntaxKind.ReadOnlyKeyword);
            if (readonlyToken.Parent is MethodDeclarationSyntax or AccessorDeclarationSyntax or BasePropertyDeclarationSyntax or EventDeclarationSyntax)
                modifierErrors |= !MessageID.IDS_FeatureReadOnlyMembers.CheckFeatureAvailability(diagnostics, readonlyToken);

            hasExplicitAccessModifier = (result & DeclarationModifiers.AccessibilityMask) != 0;
            if (!hasExplicitAccessModifier)
                result |= defaultAccess;

            return result;
        }

        internal static DeclarationModifiers CheckModifiers(
            bool isForTypeDeclaration,
            bool isForInterfaceMember,
            DeclarationModifiers modifiers,
            DeclarationModifiers allowedModifiers,
            Location errorLocation,
            BindingDiagnosticBag diagnostics,
            SyntaxTokenList? modifierTokens,
            out bool modifierErrors)
        {
            Debug.Assert(!isForTypeDeclaration || !isForInterfaceMember);

            modifierErrors = false;
            DeclarationModifiers reportStaticNotVirtualForModifiers = DeclarationModifiers.None;

            if (isForTypeDeclaration)
            {
                Debug.Assert((allowedModifiers & (DeclarationModifiers.Override | DeclarationModifiers.Virtual)) == 0);
            }
            else if ((modifiers & allowedModifiers & DeclarationModifiers.Static) != 0)
            {
                if (isForInterfaceMember)
                {
                    reportStaticNotVirtualForModifiers = allowedModifiers & DeclarationModifiers.Override;
                }
                else
                {
                    reportStaticNotVirtualForModifiers = allowedModifiers & (DeclarationModifiers.Abstract | DeclarationModifiers.Override | DeclarationModifiers.Virtual);
                }

                allowedModifiers &= ~reportStaticNotVirtualForModifiers;
            }

            DeclarationModifiers errorModifiers = modifiers & ~allowedModifiers;
            DeclarationModifiers result = modifiers & allowedModifiers;

            while (errorModifiers != DeclarationModifiers.None)
            {
                DeclarationModifiers oneError = errorModifiers & ~(errorModifiers - 1);
                Debug.Assert(oneError != DeclarationModifiers.None);
                errorModifiers &= ~oneError;

                switch (oneError)
                {
                    case DeclarationModifiers.Partial:
                        // Provide a specialized error message in the case of partial.
                        ReportPartialError(errorLocation, diagnostics, modifierTokens);
                        break;

                    case DeclarationModifiers.Abstract:
                    case DeclarationModifiers.Override:
                    case DeclarationModifiers.Virtual:
                        if ((reportStaticNotVirtualForModifiers & oneError) == 0)
                        {
                            goto default;
                        }

                        diagnostics.Add(ErrorCode.ERR_StaticNotVirtual, errorLocation, ModifierUtils.ConvertSingleModifierToSyntaxText(oneError));
                        break;

                    default:
                        diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, ConvertSingleModifierToSyntaxText(oneError));
                        break;
                }

                modifierErrors = true;
            }

            modifierErrors |=
                checkFeature(DeclarationModifiers.PrivateProtected, MessageID.IDS_FeaturePrivateProtected) |
                checkFeature(DeclarationModifiers.Required, MessageID.IDS_FeatureRequiredMembers) |
                checkFeature(DeclarationModifiers.File, MessageID.IDS_FeatureFileTypes) |
                checkFeature(DeclarationModifiers.Async, MessageID.IDS_FeatureAsync);

            return result;

            bool checkFeature(DeclarationModifiers modifier, MessageID featureID)
                => ((result & modifier) != 0) && !Binder.CheckFeatureAvailability(errorLocation.SourceTree, featureID, diagnostics, errorLocation);
        }

        internal static void CheckScopedModifierAvailability(CSharpSyntaxNode syntax, SyntaxToken modifier, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(modifier.Kind() == SyntaxKind.ScopedKeyword);

            if (MessageID.IDS_FeatureRefFields.GetFeatureAvailabilityDiagnosticInfo((CSharpParseOptions)syntax.SyntaxTree.Options) is { } diagnosticInfo)
            {
                diagnostics.Add(diagnosticInfo, modifier.GetLocation());
            }
        }

        private static void ReportPartialError(Location errorLocation, BindingDiagnosticBag diagnostics, SyntaxTokenList? modifierTokens)
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
            BindingDiagnosticBag diagnostics)
        {
            if ((modifiers & defaultInterfaceImplementationModifiers) != 0)
            {
                LanguageVersion availableVersion = ((CSharpParseOptions)errorLocation.SourceTree.Options).LanguageVersion;
                LanguageVersion requiredVersion;

                if ((modifiers & defaultInterfaceImplementationModifiers & DeclarationModifiers.Static) != 0 &&
                    (modifiers & defaultInterfaceImplementationModifiers & (DeclarationModifiers.Sealed | DeclarationModifiers.Abstract | DeclarationModifiers.Virtual)) != 0)
                {
                    var reportModifiers = DeclarationModifiers.Sealed | DeclarationModifiers.Abstract | DeclarationModifiers.Virtual;
                    if ((modifiers & defaultInterfaceImplementationModifiers & DeclarationModifiers.Sealed) != 0 &&
                        (modifiers & defaultInterfaceImplementationModifiers & (DeclarationModifiers.Abstract | DeclarationModifiers.Virtual)) != 0)
                    {
                        diagnostics.Add(ErrorCode.ERR_BadMemberFlag, errorLocation, ConvertSingleModifierToSyntaxText(DeclarationModifiers.Sealed));
                        reportModifiers &= ~DeclarationModifiers.Sealed;
                    }

                    requiredVersion = MessageID.IDS_FeatureStaticAbstractMembersInInterfaces.RequiredVersion();
                    if (availableVersion < requiredVersion)
                    {
                        ReportUnsupportedModifiersForLanguageVersion(modifiers, reportModifiers, errorLocation, diagnostics, availableVersion, requiredVersion);
                    }

                    return; // below we will either ask for an earlier version of the language, or will not report anything
                }

                if (hasBody)
                {
                    if ((modifiers & defaultInterfaceImplementationModifiers & DeclarationModifiers.Static) != 0)
                    {
                        Binder.CheckFeatureAvailability(errorLocation.SourceTree, MessageID.IDS_DefaultInterfaceImplementation, diagnostics, errorLocation);
                    }
                }
                else
                {
                    requiredVersion = MessageID.IDS_DefaultInterfaceImplementation.RequiredVersion();
                    if (availableVersion < requiredVersion)
                    {
                        ReportUnsupportedModifiersForLanguageVersion(modifiers, defaultInterfaceImplementationModifiers, errorLocation, diagnostics, availableVersion, requiredVersion);
                    }
                }
            }
        }

        internal static void ReportUnsupportedModifiersForLanguageVersion(DeclarationModifiers modifiers, DeclarationModifiers unsupportedModifiers, Location errorLocation, BindingDiagnosticBag diagnostics, LanguageVersion availableVersion, LanguageVersion requiredVersion)
        {
            DeclarationModifiers errorModifiers = modifiers & unsupportedModifiers;
            var requiredVersionArgument = new CSharpRequiredLanguageVersion(requiredVersion);
            var availableVersionArgument = availableVersion.ToDisplayString();
            while (errorModifiers != DeclarationModifiers.None)
            {
                DeclarationModifiers oneError = errorModifiers & ~(errorModifiers - 1);
                Debug.Assert(oneError != DeclarationModifiers.None);
                errorModifiers = errorModifiers & ~oneError;
                diagnostics.Add(ErrorCode.ERR_InvalidModifierForLanguageVersion, errorLocation,
                                ConvertSingleModifierToSyntaxText(oneError),
                                availableVersionArgument,
                                requiredVersionArgument);
            }
        }

        internal static void CheckFeatureAvailabilityForStaticAbstractMembersInInterfacesIfNeeded(DeclarationModifiers mods, bool isExplicitInterfaceImplementation, Location location, BindingDiagnosticBag diagnostics)
        {
            if (isExplicitInterfaceImplementation && (mods & DeclarationModifiers.Static) != 0)
            {
                Debug.Assert(location.SourceTree is not null);

                LanguageVersion availableVersion = ((CSharpParseOptions)location.SourceTree.Options).LanguageVersion;
                LanguageVersion requiredVersion = MessageID.IDS_FeatureStaticAbstractMembersInInterfaces.RequiredVersion();
                if (availableVersion < requiredVersion)
                {
                    ModifierUtils.ReportUnsupportedModifiersForLanguageVersion(mods, DeclarationModifiers.Static, location, diagnostics, availableVersion, requiredVersion);
                }
            }
        }

#nullable enable
        internal static void CheckFeatureAvailabilityForPartialEventsAndConstructors(Location location, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(location.SourceTree is not null);

            LanguageVersion availableVersion = ((CSharpParseOptions)location.SourceTree.Options).LanguageVersion;
            LanguageVersion requiredVersion = MessageID.IDS_FeaturePartialEventsAndConstructors.RequiredVersion();
            if (availableVersion < requiredVersion)
            {
                ReportUnsupportedModifiersForLanguageVersion(
                    DeclarationModifiers.Partial,
                    DeclarationModifiers.Partial,
                    location,
                    diagnostics,
                    availableVersion,
                    requiredVersion);
            }
        }
#nullable disable

        internal static DeclarationModifiers AdjustModifiersForAnInterfaceMember(DeclarationModifiers mods, bool hasBody, bool isExplicitInterfaceImplementation, bool forMethod)
        {
            // Interface partial non-method members are implicitly public and virtual just like their non-partial counterparts.
            // Interface partial methods are implicitly private and not virtual (this is a spec violation but being preserved to avoid breaks).
            bool notPartialOrNewPartialBehavior = (mods & DeclarationModifiers.Partial) == 0 || !forMethod;

            if ((mods & DeclarationModifiers.AccessibilityMask) == 0)
            {
                if (!isExplicitInterfaceImplementation && notPartialOrNewPartialBehavior)
                {
                    mods |= DeclarationModifiers.Public;
                }
                else
                {
                    mods |= DeclarationModifiers.Private;
                }
            }

            if (isExplicitInterfaceImplementation)
            {
                if ((mods & DeclarationModifiers.Abstract) != 0)
                {
                    mods |= DeclarationModifiers.Sealed;
                }
            }
            else if ((mods & DeclarationModifiers.Static) != 0)
            {
                mods &= ~DeclarationModifiers.Sealed;
            }
            else if ((mods & (DeclarationModifiers.Private | DeclarationModifiers.Virtual | DeclarationModifiers.Abstract)) == 0 && notPartialOrNewPartialBehavior)
            {
                Debug.Assert(!isExplicitInterfaceImplementation);

                if (hasBody || (mods & (DeclarationModifiers.Extern | DeclarationModifiers.Partial | DeclarationModifiers.Sealed)) != 0)
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

            return mods;
        }

        internal static string ConvertSingleModifierToSyntaxText(DeclarationModifiers modifier)
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
                case DeclarationModifiers.Required:
                    return SyntaxFacts.GetText(SyntaxKind.RequiredKeyword);
                case DeclarationModifiers.Scoped:
                    return SyntaxFacts.GetText(SyntaxKind.ScopedKeyword);
                case DeclarationModifiers.File:
                    return SyntaxFacts.GetText(SyntaxKind.FileKeyword);
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
                case SyntaxKind.RequiredKeyword:
                    return DeclarationModifiers.Required;
                case SyntaxKind.ScopedKeyword:
                    return DeclarationModifiers.Scoped;
                case SyntaxKind.FileKeyword:
                    return DeclarationModifiers.File;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        public static DeclarationModifiers ToDeclarationModifiers(
            this SyntaxTokenList modifiers, bool isForTypeDeclaration, DiagnosticBag diagnostics, bool isOrdinaryMethod = false)
        {
            var result = DeclarationModifiers.None;
            bool seenNoDuplicates = true;

            for (int i = 0; i < modifiers.Count; i++)
            {
                SyntaxToken modifier = modifiers[i];
                DeclarationModifiers one = ToDeclarationModifier(modifier.ContextualKind());

                ReportDuplicateModifiers(
                    modifier, one, result,
                    ref seenNoDuplicates,
                    diagnostics);

                if (one == DeclarationModifiers.Partial)
                {
                    var messageId = isForTypeDeclaration ? MessageID.IDS_FeaturePartialTypes : MessageID.IDS_FeaturePartialMethod;
                    messageId.CheckFeatureAvailability(diagnostics, modifier);

                    // `partial` must always be the last modifier according to the language.  However, there was a bug
                    // where we allowed `partial async` at the end of modifiers on methods. We keep this behavior for
                    // backcompat.
                    var isLast = i == modifiers.Count - 1;
                    var isPartialAsyncMethod = isOrdinaryMethod && i == modifiers.Count - 2 && modifiers[i + 1].ContextualKind() is SyntaxKind.AsyncKeyword;
                    if (!isLast && !isPartialAsyncMethod)
                    {
                        diagnostics.Add(
                            ErrorCode.ERR_PartialMisplaced,
                            modifier.GetLocation());
                    }
                }

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

        internal static bool CheckAccessibility(DeclarationModifiers modifiers, Symbol symbol, bool isExplicitInterfaceImplementation, BindingDiagnosticBag diagnostics, Location errorLocation)
        {
            if (!IsValidAccessibility(modifiers))
            {
                // error CS0107: More than one protection modifier
                diagnostics.Add(ErrorCode.ERR_BadMemberProtection, errorLocation);
                return true;
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

                        if (symbol.ContainingType?.IsInterface == true && !symbol.ContainingAssembly.RuntimeSupportsDefaultInterfaceImplementation)
                        {
                            diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportProtectedAccessForInterfaceMember, errorLocation);
                            return true;
                        }
                        break;
                }
            }

            if ((modifiers & DeclarationModifiers.Required) != 0)
            {
                var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(futureDestination: diagnostics, assemblyBeingBuilt: symbol.ContainingAssembly);
                bool reportedError = false;

                switch (symbol)
                {
                    case FieldSymbol when !symbol.IsAsRestrictive(symbol.ContainingType, ref useSiteInfo):
                    case PropertySymbol { SetMethod: { } method } when !method.IsAsRestrictive(symbol.ContainingType, ref useSiteInfo):
                        // Required member '{0}' cannot be less visible or have a setter less visible than the containing type '{1}'.
                        diagnostics.Add(ErrorCode.ERR_RequiredMemberCannotBeLessVisibleThanContainingType, errorLocation, symbol, symbol.ContainingType);
                        reportedError = true;
                        break;
                    case PropertySymbol { SetMethod: null }:
                    case FieldSymbol when (modifiers & DeclarationModifiers.ReadOnly) != 0:
                        // Required member '{0}' must be settable.
                        diagnostics.Add(ErrorCode.ERR_RequiredMemberMustBeSettable, errorLocation, symbol);
                        reportedError = true;
                        break;
                }

                diagnostics.Add(errorLocation, useSiteInfo);
                return reportedError;
            }

            return false;
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
