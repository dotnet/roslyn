// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable RS1024 // Use 'SymbolEqualityComparer' when comparing symbols (https://github.com/dotnet/roslyn/issues/78583)

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISymbolExtensions
{
    /// <summary>
    /// Checks if 'symbol' is accessible from within 'within'.
    /// </summary>
    public static bool IsAccessibleWithin(
        this ISymbol symbol,
        ISymbol within,
        ITypeSymbol? throughType = null)
    {
        if (within is IAssemblySymbol assembly)
        {
            return symbol.IsAccessibleWithin(assembly, throughType);
        }
        else if (within is INamedTypeSymbol namedType)
        {
            return symbol.IsAccessibleWithin(namedType, throughType);
        }
        else
        {
            throw new ArgumentException();
        }
    }

    /// <summary>
    /// Checks if 'symbol' is accessible from within assembly 'within'.
    /// </summary>
    public static bool IsAccessibleWithin(
        this ISymbol symbol,
        IAssemblySymbol within,
        ITypeSymbol? throughType = null)
    {
        return IsSymbolAccessibleCore(symbol, within, throughType, out _);
    }

    /// <summary>
    /// Checks if 'symbol' is accessible from within name type 'within', with an optional
    /// qualifier of type "throughTypeOpt".
    /// </summary>
    public static bool IsAccessibleWithin(
        this ISymbol symbol,
        INamedTypeSymbol within,
        ITypeSymbol? throughType = null)
    {
        return IsSymbolAccessible(symbol, within, throughType, out _);
    }

    /// <summary>
    /// Checks if 'symbol' is accessible from within assembly 'within', with an qualifier of
    /// type "throughTypeOpt". Sets "failedThroughTypeCheck" to true if it failed the "through
    /// type" check.
    /// </summary>
    private static bool IsSymbolAccessible(
        ISymbol symbol,
        INamedTypeSymbol within,
        ITypeSymbol? throughType,
        out bool failedThroughTypeCheck)
    {
        return IsSymbolAccessibleCore(symbol, within, throughType, out failedThroughTypeCheck);
    }

    /// <summary>
    /// Checks if 'symbol' is accessible from within 'within', which must be a INamedTypeSymbol
    /// or an IAssemblySymbol.  If 'symbol' is accessed off of an expression then
    /// 'throughTypeOpt' is the type of that expression. This is needed to properly do protected
    /// access checks. Sets "failedThroughTypeCheck" to true if this protected check failed.
    /// </summary>
    //// NOTE(cyrusn): I expect this function to be called a lot.  As such, I do not do any memory
    //// allocations in the function itself (including not making any iterators).  This does mean
    //// that certain helper functions that we'd like to call are inlined in this method to
    //// prevent the overhead of returning collections or enumerators.  
    private static bool IsSymbolAccessibleCore(
        ISymbol symbol,
        ISymbol within,  // must be assembly or named type symbol
        ITypeSymbol? throughType,
        out bool failedThroughTypeCheck)
    {
        Contract.ThrowIfNull(symbol);
        Contract.ThrowIfNull(within);
        Debug.Assert(within is INamedTypeSymbol or IAssemblySymbol);

        failedThroughTypeCheck = false;
        switch (symbol.Kind)
        {
            case SymbolKind.Alias:
                return IsSymbolAccessibleCore(((IAliasSymbol)symbol).Target, within, throughType, out failedThroughTypeCheck);

            case SymbolKind.ArrayType:
                return IsSymbolAccessibleCore(((IArrayTypeSymbol)symbol).ElementType, within, null, out failedThroughTypeCheck);

            case SymbolKind.PointerType:
                return IsSymbolAccessibleCore(((IPointerTypeSymbol)symbol).PointedAtType, within, null, out failedThroughTypeCheck);

            case SymbolKind.FunctionPointerType:
                var funcPtrSignature = ((IFunctionPointerTypeSymbol)symbol).Signature;
                if (!IsSymbolAccessibleCore(funcPtrSignature.ReturnType, within, null, out failedThroughTypeCheck))
                {
                    return false;
                }

                foreach (var param in funcPtrSignature.Parameters)
                {
                    if (!IsSymbolAccessibleCore(param.Type, within, null, out failedThroughTypeCheck))
                    {
                        return false;
                    }
                }

                return true;

            case SymbolKind.NamedType:
                return IsNamedTypeAccessible((INamedTypeSymbol)symbol, within);

            case SymbolKind.ErrorType:
            case SymbolKind.Discard:
                return true;

            case SymbolKind.TypeParameter:
            case SymbolKind.Parameter:
            case SymbolKind.Local:
            case SymbolKind.Label:
            case SymbolKind.Namespace:
            case SymbolKind.DynamicType:
            case SymbolKind.Assembly:
            case SymbolKind.NetModule:
            case SymbolKind.RangeVariable:
            case SymbolKind.Preprocessing:
                // These types of symbols are always accessible (if visible).
                return true;

            case SymbolKind.Method:
            case SymbolKind.Property:
            case SymbolKind.Field:
            case SymbolKind.Event:
                if (symbol.IsStatic)
                {
                    // static members aren't accessed "through" an "instance" of any type.  So we
                    // null out the "through" instance here.  This ensures that we'll understand
                    // accessing protected statics properly.
                    throughType = null;
                }

                // If this is a synthesized operator of dynamic, it's always accessible.
                if (symbol is IMethodSymbol { MethodKind: MethodKind.BuiltinOperator })
                {
                    if (symbol.ContainingSymbol.IsKind(SymbolKind.DynamicType))
                        return true;

                    // If it's a synthesized operator on a pointer, use the pointer's PointedAtType.
                    // Note: there are currently no synthesized operators on function pointer types. If that
                    // ever changes, updated the below assert and fix the code
                    if (symbol.ContainingSymbol is IPointerTypeSymbol pointerType)
                        return IsSymbolAccessibleCore(pointerType.PointedAtType, within, null, out failedThroughTypeCheck);
                }

                return IsMemberAccessible(symbol.ContainingType, symbol.DeclaredAccessibility, within, throughType, out failedThroughTypeCheck);

            default:
                throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        }
    }

    // Is the named type "type" accessible from within "within", which must be a named type or
    // an assembly.
    private static bool IsNamedTypeAccessible(INamedTypeSymbol type, ISymbol within)
    {
        Debug.Assert(within is INamedTypeSymbol or IAssemblySymbol);
        Contract.ThrowIfNull(type);

        if (type.IsErrorType())
        {
            // Always assume that error types are accessible.
            return true;
        }

        if (!type.IsDefinition)
        {
            // All type argument must be accessible.
            foreach (var typeArg in type.TypeArguments)
            {
                // type parameters are always accessible, so don't check those (so common it's
                // worth optimizing this).
                if (typeArg.Kind != SymbolKind.TypeParameter &&
                    typeArg.TypeKind != TypeKind.Error &&
                    !IsSymbolAccessibleCore(typeArg, within, null, out _))
                {
                    return false;
                }
            }
        }

        var containingType = type.ContainingType;
        return containingType == null
            ? IsNonNestedTypeAccessible(type.ContainingAssembly, type.DeclaredAccessibility, within)
            : IsMemberAccessible(type.ContainingType, type.DeclaredAccessibility, within, null, out _);
    }

    // Is a top-level type with accessibility "declaredAccessibility" inside assembly "assembly"
    // accessible from "within", which must be a named type of an assembly.
    private static bool IsNonNestedTypeAccessible(
        IAssemblySymbol assembly,
        Accessibility declaredAccessibility,
        ISymbol within)
    {
        Debug.Assert(within is INamedTypeSymbol or IAssemblySymbol);
        Contract.ThrowIfNull(assembly);
        var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;

        switch (declaredAccessibility)
        {
            case Accessibility.NotApplicable:
            case Accessibility.Public:
                // Public symbols are always accessible from any context
                return true;

            case Accessibility.Private:
            case Accessibility.Protected:
            case Accessibility.ProtectedAndInternal:
                // Shouldn't happen except in error cases.
                return false;

            case Accessibility.Internal:
            case Accessibility.ProtectedOrInternal:
                // An internal type is accessible if we're in the same assembly or we have
                // friend access to the assembly it was defined in.
                return withinAssembly.IsSameAssemblyOrHasFriendAccessTo(assembly);

            default:
                throw ExceptionUtilities.UnexpectedValue(declaredAccessibility);
        }
    }

    // Is a member with declared accessibility "declaredAccessibility" accessible from within
    // "within", which must be a named type or an assembly.
    private static bool IsMemberAccessible(
        INamedTypeSymbol containingType,
        Accessibility declaredAccessibility,
        ISymbol within,
        ITypeSymbol? throughType,
        out bool failedThroughTypeCheck)
    {
        Debug.Assert(within is INamedTypeSymbol or IAssemblySymbol);
        Contract.ThrowIfNull(containingType);

        failedThroughTypeCheck = false;

        var originalContainingType = containingType.OriginalDefinition;
        var withinNamedType = within as INamedTypeSymbol;
        var withinAssembly = (within as IAssemblySymbol) ?? ((INamedTypeSymbol)within).ContainingAssembly;

        // A nested symbol is only accessible to us if its container is accessible as well.
        if (!IsNamedTypeAccessible(containingType, within))
        {
            return false;
        }

        switch (declaredAccessibility)
        {
            case Accessibility.NotApplicable:
                // TODO(cyrusn): Is this the right thing to do here?  Should the caller ever be
                // asking about the accessibility of a symbol that has "NotApplicable" as its
                // value?  For now, I'm preserving the behavior of the existing code.  But perhaps
                // we should fail here and require the caller to not do this?
                return true;

            case Accessibility.Public:
                // Public symbols are always accessible from any context
                return true;

            case Accessibility.Private:
                // All expressions in the current submission (top-level or nested in a method or
                // type) can access previous submission's private top-level members. Previous
                // submissions are treated like outer classes for the current submission - the
                // inner class can access private members of the outer class.
                if (withinAssembly.IsInteractive && containingType.IsScriptClass)
                {
                    return true;
                }

                // private members never accessible from outside a type.
                return withinNamedType != null && IsPrivateSymbolAccessible(withinNamedType, originalContainingType);

            case Accessibility.Internal:
                // An internal type is accessible if we're in the same assembly or we have
                // friend access to the assembly it was defined in.
                return withinAssembly.IsSameAssemblyOrHasFriendAccessTo(containingType.ContainingAssembly);

            case Accessibility.ProtectedAndInternal:
                if (!withinAssembly.IsSameAssemblyOrHasFriendAccessTo(containingType.ContainingAssembly))
                {
                    // We require internal access.  If we don't have it, then this symbol is
                    // definitely not accessible to us.
                    return false;
                }

                // We had internal access.  Also have to make sure we have protected access.
                return IsProtectedSymbolAccessible(withinNamedType, withinAssembly, throughType, originalContainingType, out failedThroughTypeCheck);

            case Accessibility.ProtectedOrInternal:
                if (withinAssembly.IsSameAssemblyOrHasFriendAccessTo(containingType.ContainingAssembly))
                {
                    // If we have internal access to this symbol, then that's sufficient.  no
                    // need to do the complicated protected case.
                    return true;
                }

                // We don't have internal access.  But if we have protected access then that's
                // sufficient.
                return IsProtectedSymbolAccessible(withinNamedType, withinAssembly, throughType, originalContainingType, out failedThroughTypeCheck);

            case Accessibility.Protected:
                return IsProtectedSymbolAccessible(withinNamedType, withinAssembly, throughType, originalContainingType, out failedThroughTypeCheck);

            default:
                throw ExceptionUtilities.UnexpectedValue(declaredAccessibility);
        }
    }

    // Is a protected symbol inside "originalContainingType" accessible from within "within",
    // which much be a named type or an assembly.
    private static bool IsProtectedSymbolAccessible(
        INamedTypeSymbol? withinType,
        IAssemblySymbol withinAssembly,
        ITypeSymbol? throughType,
        INamedTypeSymbol originalContainingType,
        out bool failedThroughTypeCheck)
    {
        failedThroughTypeCheck = false;

        // It is not an error to define protected member in a sealed Script class, 
        // it's just a warning. The member behaves like a private one - it is visible 
        // in all subsequent submissions.
        if (withinAssembly.IsInteractive && originalContainingType.IsScriptClass)
        {
            return true;
        }

        if (withinType == null)
        {
            // If we're not within a type, we can't access a protected symbol
            return false;
        }

        // A protected symbol is accessible if we're (optionally nested) inside the type that it
        // was defined in. 

        // NOTE(ericli): It is helpful to consider 'protected' as *increasing* the
        // accessibility domain of a private member, rather than *decreasing* that of a public
        // member. Members are naturally private; the protected, internal and public access
        // modifiers all increase the accessibility domain. Since private members are accessible
        // to nested types, so are protected members.

        // NOTE(cyrusn): We do this check up front as it is very fast and easy to do.
        if (IsNestedWithinOriginalContainingType(withinType, originalContainingType))
        {
            return true;
        }

        // Protected is really confusing.  Check out 3.5.3 of the language spec "protected access
        // for instance members" to see how it works.  I actually got the code for this from
        // LangCompiler::CheckAccessCore
        {
            var current = withinType.OriginalDefinition;
            var originalThroughType = throughType?.OriginalDefinition;
            while (current != null)
            {
                Debug.Assert(current.IsDefinition);

                if (current.InheritsFromOrImplementsOrEqualsIgnoringConstruction(originalContainingType))
                {
                    // NOTE(cyrusn): We're continually walking up the 'throughType's inheritance
                    // chain.  We could compute it up front and cache it in a set.  However, i
                    // don't want to allocate memory in this function.  Also, in practice
                    // inheritance chains should be very short.  As such, it might actually be
                    // slower to create and check inside the set versus just walking the
                    // inheritance chain.
                    if (originalThroughType == null ||
                        originalThroughType.InheritsFromOrImplementsOrEqualsIgnoringConstruction(current))
                    {
                        return true;
                    }
                    else
                    {
                        failedThroughTypeCheck = true;
                    }
                }

                // NOTE(cyrusn): The container of an original type is always original.
                current = current.ContainingType;
            }
        }

        return false;
    }

    // Is a private symbol access
    private static bool IsPrivateSymbolAccessible(
        ISymbol within,
        INamedTypeSymbol originalContainingType)
    {
        Debug.Assert(within is INamedTypeSymbol or IAssemblySymbol);

        if (within is not INamedTypeSymbol withinType)
        {
            // If we're not within a type, we can't access a private symbol
            return false;
        }

        // A private symbol is accessible if we're (optionally nested) inside the type that it
        // was defined in.
        return IsNestedWithinOriginalContainingType(withinType, originalContainingType);
    }

    // Is the type "withinType" nested within the original type "originalContainingType".
    private static bool IsNestedWithinOriginalContainingType(
        INamedTypeSymbol withinType,
        INamedTypeSymbol originalContainingType)
    {
        Contract.ThrowIfNull(withinType);
        Contract.ThrowIfNull(originalContainingType);

        // Walk up my parent chain and see if I eventually hit the owner.  If so then I'm a
        // nested type of that owner and I'm allowed access to everything inside of it.
        var current = withinType.OriginalDefinition;
        while (current != null)
        {
            Debug.Assert(current.IsDefinition);
            if (current.Equals(originalContainingType))
            {
                return true;
            }

            // NOTE(cyrusn): The container of an 'original' type is always original. 
            current = current.ContainingType;
        }

        return false;
    }
}
