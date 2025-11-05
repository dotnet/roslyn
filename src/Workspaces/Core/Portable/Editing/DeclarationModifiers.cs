// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editing;

public readonly record struct DeclarationModifiers
{
    internal readonly Modifiers Modifiers;

    internal DeclarationModifiers(Modifiers modifiers)
        => Modifiers = modifiers;

    internal DeclarationModifiers(
        bool isStatic = false,
        bool isAbstract = false,
        bool isNew = false,
        bool isUnsafe = false,
        bool isReadOnly = false,
        bool isVirtual = false,
        bool isOverride = false,
        bool isSealed = false,
        bool isConst = false,
        bool isWithEvents = false,
        bool isPartial = false,
        bool isAsync = false,
        bool isWriteOnly = false,
        bool isRef = false,
        bool isVolatile = false,
        bool isExtern = false,
        bool isRequired = false,
        bool isFile = false,
        bool isFixed = false)
        : this(
              (isStatic ? Modifiers.Static : Modifiers.None) |
              (isAbstract ? Modifiers.Abstract : Modifiers.None) |
              (isNew ? Modifiers.New : Modifiers.None) |
              (isUnsafe ? Modifiers.Unsafe : Modifiers.None) |
              (isReadOnly ? Modifiers.ReadOnly : Modifiers.None) |
              (isVirtual ? Modifiers.Virtual : Modifiers.None) |
              (isOverride ? Modifiers.Override : Modifiers.None) |
              (isSealed ? Modifiers.Sealed : Modifiers.None) |
              (isConst ? Modifiers.Const : Modifiers.None) |
              (isWithEvents ? Modifiers.WithEvents : Modifiers.None) |
              (isPartial ? Modifiers.Partial : Modifiers.None) |
              (isAsync ? Modifiers.Async : Modifiers.None) |
              (isWriteOnly ? Modifiers.WriteOnly : Modifiers.None) |
              (isRef ? Modifiers.Ref : Modifiers.None) |
              (isVolatile ? Modifiers.Volatile : Modifiers.None) |
              (isExtern ? Modifiers.Extern : Modifiers.None) |
              (isRequired ? Modifiers.Required : Modifiers.None) |
              (isFile ? Modifiers.File : Modifiers.None) |
              (isFixed ? Modifiers.Fixed : Modifiers.None))
    {
    }

    public static DeclarationModifiers From(ISymbol symbol)
    {
        if (symbol
                is INamedTypeSymbol
                or IFieldSymbol
                or IPropertySymbol
                or IMethodSymbol
                or IEventSymbol)
        {
            var field = symbol as IFieldSymbol;
            var property = symbol as IPropertySymbol;
            var method = symbol as IMethodSymbol;
            var @event = symbol as IEventSymbol;
            var type = symbol as INamedTypeSymbol;
            var isConst = field?.IsConst == true;

            // A symbol is partial if it's a partial definition or a partial implementation
            var isPartial = method?.IsPartialDefinition == true ||
                            method?.PartialDefinitionPart != null ||
                            property?.IsPartialDefinition == true ||
                            property?.PartialDefinitionPart != null ||
                            @event?.IsPartialDefinition == true ||
                            @event?.PartialDefinitionPart != null;

            return new DeclarationModifiers(
                isStatic: symbol.IsStatic && !isConst,
                isAbstract: symbol.IsAbstract,
                isReadOnly: field?.IsReadOnly == true || property?.IsReadOnly == true || type?.IsReadOnly == true || method?.IsReadOnly == true,
                isVirtual: symbol.IsVirtual,
                isOverride: symbol.IsOverride,
                isSealed: symbol.IsSealed,
                isConst: isConst,
                isUnsafe: symbol.RequiresUnsafeModifier(),
                isRef: field?.RefKind is RefKind.Ref or RefKind.RefReadOnly || type?.IsRefLikeType == true,
                isVolatile: field?.IsVolatile == true,
                isExtern: symbol.IsExtern,
                isAsync: method?.IsAsync == true,
                isRequired: symbol.IsRequired(),
                isFile: type?.IsFileLocal == true,
                isFixed: field?.IsFixedSizeBuffer == true,
                isPartial: isPartial);
        }

        // Only named types, members of named types, and local functions have modifiers.
        // Everything else has none.
        return DeclarationModifiers.None;
    }

    public bool IsStatic => (Modifiers & Modifiers.Static) != 0;

    public bool IsAbstract => (Modifiers & Modifiers.Abstract) != 0;

    public bool IsNew => (Modifiers & Modifiers.New) != 0;

    public bool IsUnsafe => (Modifiers & Modifiers.Unsafe) != 0;

    public bool IsReadOnly => (Modifiers & Modifiers.ReadOnly) != 0;

    public bool IsVirtual => (Modifiers & Modifiers.Virtual) != 0;

    public bool IsOverride => (Modifiers & Modifiers.Override) != 0;

    public bool IsSealed => (Modifiers & Modifiers.Sealed) != 0;

    public bool IsConst => (Modifiers & Modifiers.Const) != 0;

    public bool IsWithEvents => (Modifiers & Modifiers.WithEvents) != 0;

    public bool IsPartial => (Modifiers & Modifiers.Partial) != 0;

    public bool IsAsync => (Modifiers & Modifiers.Async) != 0;

    public bool IsWriteOnly => (Modifiers & Modifiers.WriteOnly) != 0;

    public bool IsRef => (Modifiers & Modifiers.Ref) != 0;

    public bool IsVolatile => (Modifiers & Modifiers.Volatile) != 0;

    public bool IsExtern => (Modifiers & Modifiers.Extern) != 0;

    public bool IsRequired => (Modifiers & Modifiers.Required) != 0;

    public bool IsFile => (Modifiers & Modifiers.File) != 0;

    internal bool IsFixed => (Modifiers & Modifiers.Fixed) != 0;

    public DeclarationModifiers WithIsStatic(bool isStatic)
        => new(SetFlag(Modifiers, Modifiers.Static, isStatic));

    public DeclarationModifiers WithIsAbstract(bool isAbstract)
        => new(SetFlag(Modifiers, Modifiers.Abstract, isAbstract));

    public DeclarationModifiers WithIsNew(bool isNew)
        => new(SetFlag(Modifiers, Modifiers.New, isNew));

    public DeclarationModifiers WithIsUnsafe(bool isUnsafe)
        => new(SetFlag(Modifiers, Modifiers.Unsafe, isUnsafe));

    public DeclarationModifiers WithIsReadOnly(bool isReadOnly)
        => new(SetFlag(Modifiers, Modifiers.ReadOnly, isReadOnly));

    public DeclarationModifiers WithIsVirtual(bool isVirtual)
        => new(SetFlag(Modifiers, Modifiers.Virtual, isVirtual));

    public DeclarationModifiers WithIsOverride(bool isOverride)
        => new(SetFlag(Modifiers, Modifiers.Override, isOverride));

    public DeclarationModifiers WithIsSealed(bool isSealed)
        => new(SetFlag(Modifiers, Modifiers.Sealed, isSealed));

    public DeclarationModifiers WithIsConst(bool isConst)
        => new(SetFlag(Modifiers, Modifiers.Const, isConst));

    public DeclarationModifiers WithWithEvents(bool withEvents)
        => new(SetFlag(Modifiers, Modifiers.WithEvents, withEvents));

    public DeclarationModifiers WithPartial(bool isPartial)
        => new(SetFlag(Modifiers, Modifiers.Partial, isPartial));

    [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Public API.")]
    public DeclarationModifiers WithAsync(bool isAsync)
        => new(SetFlag(Modifiers, Modifiers.Async, isAsync));

    public DeclarationModifiers WithIsWriteOnly(bool isWriteOnly)
        => new(SetFlag(Modifiers, Modifiers.WriteOnly, isWriteOnly));

    public DeclarationModifiers WithIsRef(bool isRef)
        => new(SetFlag(Modifiers, Modifiers.Ref, isRef));

    public DeclarationModifiers WithIsVolatile(bool isVolatile)
        => new(SetFlag(Modifiers, Modifiers.Volatile, isVolatile));

    public DeclarationModifiers WithIsExtern(bool isExtern)
        => new(SetFlag(Modifiers, Modifiers.Extern, isExtern));

    public DeclarationModifiers WithIsRequired(bool isRequired)
        => new(SetFlag(Modifiers, Modifiers.Required, isRequired));

    public DeclarationModifiers WithIsFile(bool isFile)
        => new(SetFlag(Modifiers, Modifiers.File, isFile));

    private static Modifiers SetFlag(Modifiers existing, Modifiers modifier, bool isSet)
        => isSet ? (existing | modifier) : (existing & ~modifier);

    public static DeclarationModifiers None => default;

    public static DeclarationModifiers Static => new(Modifiers.Static);
    public static DeclarationModifiers Abstract => new(Modifiers.Abstract);
    public static DeclarationModifiers New => new(Modifiers.New);
    public static DeclarationModifiers Unsafe => new(Modifiers.Unsafe);
    public static DeclarationModifiers ReadOnly => new(Modifiers.ReadOnly);
    public static DeclarationModifiers Virtual => new(Modifiers.Virtual);
    public static DeclarationModifiers Override => new(Modifiers.Override);
    public static DeclarationModifiers Sealed => new(Modifiers.Sealed);
    public static DeclarationModifiers Const => new(Modifiers.Const);
    public static DeclarationModifiers WithEvents => new(Modifiers.WithEvents);
    public static DeclarationModifiers Partial => new(Modifiers.Partial);
    public static DeclarationModifiers Async => new(Modifiers.Async);
    public static DeclarationModifiers WriteOnly => new(Modifiers.WriteOnly);
    public static DeclarationModifiers Ref => new(Modifiers.Ref);
    public static DeclarationModifiers Volatile => new(Modifiers.Volatile);
    public static DeclarationModifiers Extern => new(Modifiers.Extern);
    public static DeclarationModifiers Required => new(Modifiers.Required);
    public static DeclarationModifiers File => new(Modifiers.File);
    internal static DeclarationModifiers Fixed => new(Modifiers.Fixed);

    public static DeclarationModifiers operator |(DeclarationModifiers left, DeclarationModifiers right)
        => new(left.Modifiers | right.Modifiers);

    public static DeclarationModifiers operator &(DeclarationModifiers left, DeclarationModifiers right)
        => new(left.Modifiers & right.Modifiers);

    public static DeclarationModifiers operator +(DeclarationModifiers left, DeclarationModifiers right)
        => new(left.Modifiers | right.Modifiers);

    public static DeclarationModifiers operator -(DeclarationModifiers left, DeclarationModifiers right)
        => new(left.Modifiers & ~right.Modifiers);

    public override string ToString()
        => Modifiers.ToString();

    public static bool TryParse(string value, out DeclarationModifiers modifiers)
    {
        if (Enum.TryParse(value, out Modifiers mods))
        {
            modifiers = new DeclarationModifiers(mods);
            return true;
        }
        else
        {
            modifiers = default;
            return false;
        }
    }
}
