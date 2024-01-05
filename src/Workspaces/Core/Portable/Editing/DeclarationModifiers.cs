// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;

#if CODE_STYLE
namespace Microsoft.CodeAnalysis.Internal.Editing
#else
namespace Microsoft.CodeAnalysis.Editing
#endif
{
    public readonly record struct DeclarationModifiers
    {
        private readonly Modifiers _modifiers;

        private DeclarationModifiers(Modifiers modifiers)
            => _modifiers = modifiers;

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
            bool isFile = false)
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
                  (isFile ? Modifiers.File : Modifiers.None))
        {
        }

        public static DeclarationModifiers From(ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol or
                 IFieldSymbol or
                 IPropertySymbol or
                 IMethodSymbol or
                 IEventSymbol)
            {
                var field = symbol as IFieldSymbol;
                var property = symbol as IPropertySymbol;
                var method = symbol as IMethodSymbol;
                var type = symbol as INamedTypeSymbol;
                var isConst = field?.IsConst == true;

                return new DeclarationModifiers(
                    isStatic: symbol.IsStatic && !isConst,
                    isAbstract: symbol.IsAbstract,
                    isReadOnly: field?.IsReadOnly == true || property?.IsReadOnly == true || type?.IsReadOnly == true || method?.IsReadOnly == true,
                    isVirtual: symbol.IsVirtual,
                    isOverride: symbol.IsOverride,
                    isSealed: symbol.IsSealed,
                    isConst: isConst,
                    isUnsafe: symbol.RequiresUnsafeModifier(),
                    isRef: field?.RefKind is RefKind.Ref or RefKind.RefReadOnly,
                    isVolatile: field?.IsVolatile == true,
                    isExtern: symbol.IsExtern,
                    isAsync: method?.IsAsync == true,
                    isRequired: symbol.IsRequired(),
                    isFile: type?.IsFileLocal == true);
            }

            // Only named types, members of named types, and local functions have modifiers.
            // Everything else has none.
            return DeclarationModifiers.None;
        }

        public bool IsStatic => (_modifiers & Modifiers.Static) != 0;

        public bool IsAbstract => (_modifiers & Modifiers.Abstract) != 0;

        public bool IsNew => (_modifiers & Modifiers.New) != 0;

        public bool IsUnsafe => (_modifiers & Modifiers.Unsafe) != 0;

        public bool IsReadOnly => (_modifiers & Modifiers.ReadOnly) != 0;

        public bool IsVirtual => (_modifiers & Modifiers.Virtual) != 0;

        public bool IsOverride => (_modifiers & Modifiers.Override) != 0;

        public bool IsSealed => (_modifiers & Modifiers.Sealed) != 0;

        public bool IsConst => (_modifiers & Modifiers.Const) != 0;

        public bool IsWithEvents => (_modifiers & Modifiers.WithEvents) != 0;

        public bool IsPartial => (_modifiers & Modifiers.Partial) != 0;

        public bool IsAsync => (_modifiers & Modifiers.Async) != 0;

        public bool IsWriteOnly => (_modifiers & Modifiers.WriteOnly) != 0;

        public bool IsRef => (_modifiers & Modifiers.Ref) != 0;

        public bool IsVolatile => (_modifiers & Modifiers.Volatile) != 0;

        public bool IsExtern => (_modifiers & Modifiers.Extern) != 0;

        public bool IsRequired => (_modifiers & Modifiers.Required) != 0;

        public bool IsFile => (_modifiers & Modifiers.File) != 0;

        public DeclarationModifiers WithIsStatic(bool isStatic)
            => new(SetFlag(_modifiers, Modifiers.Static, isStatic));

        public DeclarationModifiers WithIsAbstract(bool isAbstract)
            => new(SetFlag(_modifiers, Modifiers.Abstract, isAbstract));

        public DeclarationModifiers WithIsNew(bool isNew)
            => new(SetFlag(_modifiers, Modifiers.New, isNew));

        public DeclarationModifiers WithIsUnsafe(bool isUnsafe)
            => new(SetFlag(_modifiers, Modifiers.Unsafe, isUnsafe));

        public DeclarationModifiers WithIsReadOnly(bool isReadOnly)
            => new(SetFlag(_modifiers, Modifiers.ReadOnly, isReadOnly));

        public DeclarationModifiers WithIsVirtual(bool isVirtual)
            => new(SetFlag(_modifiers, Modifiers.Virtual, isVirtual));

        public DeclarationModifiers WithIsOverride(bool isOverride)
            => new(SetFlag(_modifiers, Modifiers.Override, isOverride));

        public DeclarationModifiers WithIsSealed(bool isSealed)
            => new(SetFlag(_modifiers, Modifiers.Sealed, isSealed));

        public DeclarationModifiers WithIsConst(bool isConst)
            => new(SetFlag(_modifiers, Modifiers.Const, isConst));

        public DeclarationModifiers WithWithEvents(bool withEvents)
            => new(SetFlag(_modifiers, Modifiers.WithEvents, withEvents));

        public DeclarationModifiers WithPartial(bool isPartial)
            => new(SetFlag(_modifiers, Modifiers.Partial, isPartial));

        [SuppressMessage("Style", "VSTHRD200:Use \"Async\" suffix for async methods", Justification = "Public API.")]
        public DeclarationModifiers WithAsync(bool isAsync)
            => new(SetFlag(_modifiers, Modifiers.Async, isAsync));

        public DeclarationModifiers WithIsWriteOnly(bool isWriteOnly)
            => new(SetFlag(_modifiers, Modifiers.WriteOnly, isWriteOnly));

        public DeclarationModifiers WithIsRef(bool isRef)
            => new(SetFlag(_modifiers, Modifiers.Ref, isRef));

        public DeclarationModifiers WithIsVolatile(bool isVolatile)
            => new(SetFlag(_modifiers, Modifiers.Volatile, isVolatile));

        public DeclarationModifiers WithIsExtern(bool isExtern)
            => new(SetFlag(_modifiers, Modifiers.Extern, isExtern));

        public DeclarationModifiers WithIsRequired(bool isRequired)
            => new(SetFlag(_modifiers, Modifiers.Required, isRequired));

        public DeclarationModifiers WithIsFile(bool isFile)
            => new(SetFlag(_modifiers, Modifiers.File, isFile));

        private static Modifiers SetFlag(Modifiers existing, Modifiers modifier, bool isSet)
            => isSet ? (existing | modifier) : (existing & ~modifier);

        [Flags]
        private enum Modifiers
        {
#pragma warning disable format
            None        = 0,
            Static      = 1 << 0,
            Abstract    = 1 << 1,
            New         = 1 << 2,
            Unsafe      = 1 << 3,
            ReadOnly    = 1 << 4,
            Virtual     = 1 << 5,
            Override    = 1 << 6,
            Sealed      = 1 << 7,
            Const       = 1 << 8,
            WithEvents  = 1 << 9,
            Partial     = 1 << 10,
            Async       = 1 << 11,
            WriteOnly   = 1 << 12,
            Ref         = 1 << 13,
            Volatile    = 1 << 14,
            Extern      = 1 << 15,
            Required    = 1 << 16,
            File        = 1 << 17,
#pragma warning restore format
        }

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

        public static DeclarationModifiers operator |(DeclarationModifiers left, DeclarationModifiers right)
            => new(left._modifiers | right._modifiers);

        public static DeclarationModifiers operator &(DeclarationModifiers left, DeclarationModifiers right)
            => new(left._modifiers & right._modifiers);

        public static DeclarationModifiers operator +(DeclarationModifiers left, DeclarationModifiers right)
            => new(left._modifiers | right._modifiers);

        public static DeclarationModifiers operator -(DeclarationModifiers left, DeclarationModifiers right)
            => new(left._modifiers & ~right._modifiers);

        public override string ToString()
            => _modifiers.ToString();

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
}
