// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editing
{
    public struct DeclarationModifiers : IEquatable<DeclarationModifiers>
    {
        private readonly Modifiers _modifiers;

        private DeclarationModifiers(Modifiers modifiers)
        {
            _modifiers = modifiers;
        }

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
            bool isRef = false)
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
                  (isRef ? Modifiers.Ref : Modifiers.None))
        {
        }

        public static DeclarationModifiers From(ISymbol symbol)
        {
            var field = symbol as IFieldSymbol;
            var property = symbol as IPropertySymbol;
            var method = symbol as IMethodSymbol;

            return new DeclarationModifiers(
                isStatic: symbol.IsStatic,
                isAbstract: symbol.IsAbstract,
                ////isNew: (property != null && property.OverriddenProperty == null) || (method != null && method.OverriddenMethod == null),
                isReadOnly: (field != null && field.IsReadOnly) || (property != null && property.IsReadOnly),
                isVirtual: symbol.IsVirtual,
                isOverride: symbol.IsOverride,
                isSealed: symbol.IsSealed,
                isConst: field != null && field.IsConst,
                isUnsafe: symbol.IsUnsafe());
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

        public DeclarationModifiers WithIsStatic(bool isStatic)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Static, isStatic));
        }

        public DeclarationModifiers WithIsAbstract(bool isAbstract)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Abstract, isAbstract));
        }

        public DeclarationModifiers WithIsNew(bool isNew)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.New, isNew));
        }

        public DeclarationModifiers WithIsUnsafe(bool isUnsafe)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Unsafe, isUnsafe));
        }

        public DeclarationModifiers WithIsReadOnly(bool isReadOnly)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.ReadOnly, isReadOnly));
        }

        public DeclarationModifiers WithIsVirtual(bool isVirtual)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Virtual, isVirtual));
        }

        public DeclarationModifiers WithIsOverride(bool isOverride)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Override, isOverride));
        }

        public DeclarationModifiers WithIsSealed(bool isSealed)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Sealed, isSealed));
        }

        public DeclarationModifiers WithIsConst(bool isConst)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Const, isConst));
        }

        public DeclarationModifiers WithWithEvents(bool withEvents)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.WithEvents, withEvents));
        }

        public DeclarationModifiers WithPartial(bool isPartial)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Partial, isPartial));
        }

        public DeclarationModifiers WithAsync(bool isAsync)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Async, isAsync));
        }

        public DeclarationModifiers WithIsWriteOnly(bool isWriteOnly)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.WriteOnly, isWriteOnly));
        }

        public DeclarationModifiers WithIsRef(bool isRef)
        {
            return new DeclarationModifiers(SetFlag(_modifiers, Modifiers.Ref, isRef));
        }

        private static Modifiers SetFlag(Modifiers existing, Modifiers modifier, bool isSet)
        {
            return isSet ? (existing | modifier) : (existing & ~modifier);
        }

        [Flags]
        private enum Modifiers
        {
            None = 0x0000,
            Static = 0x0001,
            Abstract = 0x0002,
            New = 0x0004,
            Unsafe = 0x0008,
            ReadOnly = 0x0010,
            Virtual = 0x0020,
            Override = 0x0040,
            Sealed = 0x0080,
            Const = 0x0100,
            WithEvents = 0x0200,
            Partial = 0x0400,
            Async = 0x0800,
            WriteOnly = 0x1000,
            Ref = 0x2000,
        }

        public static DeclarationModifiers None => default;

        public static DeclarationModifiers Static => new DeclarationModifiers(Modifiers.Static);
        public static DeclarationModifiers Abstract => new DeclarationModifiers(Modifiers.Abstract);
        public static DeclarationModifiers New => new DeclarationModifiers(Modifiers.New);
        public static DeclarationModifiers Unsafe => new DeclarationModifiers(Modifiers.Unsafe);
        public static DeclarationModifiers ReadOnly => new DeclarationModifiers(Modifiers.ReadOnly);
        public static DeclarationModifiers Virtual => new DeclarationModifiers(Modifiers.Virtual);
        public static DeclarationModifiers Override => new DeclarationModifiers(Modifiers.Override);
        public static DeclarationModifiers Sealed => new DeclarationModifiers(Modifiers.Sealed);
        public static DeclarationModifiers Const => new DeclarationModifiers(Modifiers.Const);
        public static DeclarationModifiers WithEvents => new DeclarationModifiers(Modifiers.WithEvents);
        public static DeclarationModifiers Partial => new DeclarationModifiers(Modifiers.Partial);
        public static DeclarationModifiers Async => new DeclarationModifiers(Modifiers.Async);
        public static DeclarationModifiers WriteOnly => new DeclarationModifiers(Modifiers.WriteOnly);
        public static DeclarationModifiers Ref => new DeclarationModifiers(Modifiers.Ref);

        public static DeclarationModifiers operator |(DeclarationModifiers left, DeclarationModifiers right)
        {
            return new DeclarationModifiers(left._modifiers | right._modifiers);
        }

        public static DeclarationModifiers operator &(DeclarationModifiers left, DeclarationModifiers right)
        {
            return new DeclarationModifiers(left._modifiers & right._modifiers);
        }

        public static DeclarationModifiers operator +(DeclarationModifiers left, DeclarationModifiers right)
        {
            return new DeclarationModifiers(left._modifiers | right._modifiers);
        }

        public static DeclarationModifiers operator -(DeclarationModifiers left, DeclarationModifiers right)
        {
            return new DeclarationModifiers(left._modifiers & ~right._modifiers);
        }

        public bool Equals(DeclarationModifiers modifiers)
        {
            return _modifiers == modifiers._modifiers;
        }

        public override bool Equals(object obj)
        {
            return obj is DeclarationModifiers && Equals((DeclarationModifiers)obj);
        }

        public override int GetHashCode()
        {
            return (int)_modifiers;
        }

        public static bool operator ==(DeclarationModifiers left, DeclarationModifiers right)
        {
            return left._modifiers == right._modifiers;
        }

        public static bool operator !=(DeclarationModifiers left, DeclarationModifiers right)
        {
            return left._modifiers != right._modifiers;
        }

        public override string ToString()
        {
            return _modifiers.ToString();
        }

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
