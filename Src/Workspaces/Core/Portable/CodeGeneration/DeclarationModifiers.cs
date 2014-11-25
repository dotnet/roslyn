// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    public struct DeclarationModifiers : IEquatable<DeclarationModifiers>
    {
        private readonly Modifiers modifiers;

        private DeclarationModifiers(Modifiers modifiers)
        {
            this.modifiers = modifiers;
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
            bool isAsync = false)
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
                  (isAsync ? Modifiers.Async : Modifiers.None))
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
                isConst: field != null && field.IsConst);
        }

        public bool IsStatic
        {
            get { return (this.modifiers & Modifiers.Static) != 0; }
        }

        public bool IsAbstract
        {
            get { return (this.modifiers & Modifiers.Abstract) != 0; }
        }

        public bool IsNew
        {
            get { return (this.modifiers & Modifiers.New) != 0; }
        }

        public bool IsUnsafe
        {
            get { return (this.modifiers & Modifiers.Unsafe) != 0; }
        }

        public bool IsReadOnly
        {
            get { return (this.modifiers & Modifiers.ReadOnly) != 0; }
        }

        public bool IsVirtual
        {
            get { return (this.modifiers & Modifiers.Virtual) != 0; }
        }

        public bool IsOverride
        {
            get { return (this.modifiers & Modifiers.Override) != 0; }
        }

        public bool IsSealed
        {
            get { return (this.modifiers & Modifiers.Sealed) != 0; }
        }

        public bool IsConst
        {
            get { return (this.modifiers & Modifiers.Const) != 0; }
        }

        public bool IsWithEvents
        {
            get { return (this.modifiers & Modifiers.WithEvents) != 0; }
        }

        public bool IsPartial
        {
            get { return (this.modifiers & Modifiers.Partial) != 0; }
        }

        public bool IsAsync
        {
            get { return (this.modifiers & Modifiers.Async) != 0; }
        }

        public DeclarationModifiers WithIsStatic(bool isStatic)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.Static, isStatic));
        }

        public DeclarationModifiers WithIsAbstract(bool isAbstract)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.Abstract, isAbstract));
        }

        public DeclarationModifiers WithIsNew(bool isNew)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.New, isNew));
        }

        public DeclarationModifiers WithIsUnsafe(bool isUnsafe)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.Unsafe, isUnsafe));
        }

        public DeclarationModifiers WithIsReadOnly(bool isReadOnly)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.ReadOnly, isReadOnly));
        }

        public DeclarationModifiers WithIsVirtual(bool isVirtual)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.Virtual, isVirtual));
        }

        public DeclarationModifiers WithIsOverride(bool isOverride)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.Override, isOverride));
        }

        public DeclarationModifiers WithIsSealed(bool isSealed)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.Sealed, isSealed));
        }

        public DeclarationModifiers WithIsConst(bool isConst)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.Const, isConst));
        }

        public DeclarationModifiers WithWithEvents(bool withEvents)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.WithEvents, withEvents));
        }

        public DeclarationModifiers WithPartial(bool isPartial)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.Partial, isPartial));
        }

        public DeclarationModifiers WithAsync(bool isAsync)
        {
            return new DeclarationModifiers(SetFlag(this.modifiers, Modifiers.Async, isAsync));
        }

        private static Modifiers SetFlag(Modifiers existing, Modifiers modifier, bool isSet)
        {
            return isSet ? (existing | modifier) : (existing & ~modifier);
        }

        [Flags]
        private enum Modifiers
        {
            None        = 0x0000,
            Static      = 0x0001,
            Abstract    = 0x0002,
            New         = 0x0004,
            Unsafe      = 0x0008,
            ReadOnly    = 0x0010,
            Virtual     = 0x0020,
            Override    = 0x0040,
            Sealed      = 0x0080,
            Const       = 0x0100,
            WithEvents  = 0x0200,
            Partial     = 0x0400,
            Async       = 0x0800
        }

        public static readonly DeclarationModifiers None = default(DeclarationModifiers);

        public static readonly DeclarationModifiers Static = new DeclarationModifiers(Modifiers.Static);
        public static readonly DeclarationModifiers Abstract = new DeclarationModifiers(Modifiers.Abstract);
        public static readonly DeclarationModifiers New = new DeclarationModifiers(Modifiers.New);
        public static readonly DeclarationModifiers Unsafe = new DeclarationModifiers(Modifiers.Unsafe);
        public static readonly DeclarationModifiers ReadOnly = new DeclarationModifiers(Modifiers.ReadOnly);
        public static readonly DeclarationModifiers Virtual = new DeclarationModifiers(Modifiers.Virtual);
        public static readonly DeclarationModifiers Override = new DeclarationModifiers(Modifiers.Override);
        public static readonly DeclarationModifiers Sealed = new DeclarationModifiers(Modifiers.Sealed);
        public static readonly DeclarationModifiers Const = new DeclarationModifiers(Modifiers.Const);
        public static readonly DeclarationModifiers WithEvents = new DeclarationModifiers(Modifiers.WithEvents);
        public static readonly DeclarationModifiers Partial = new DeclarationModifiers(Modifiers.Partial);
        public static readonly DeclarationModifiers Async = new DeclarationModifiers(Modifiers.Async);

        public static DeclarationModifiers operator |(DeclarationModifiers left, DeclarationModifiers right)
        {
            return new DeclarationModifiers(left.modifiers | right.modifiers);
        }

        public static DeclarationModifiers operator &(DeclarationModifiers left, DeclarationModifiers right)
        {
            return new DeclarationModifiers(left.modifiers & right.modifiers);
        }

        public static DeclarationModifiers operator +(DeclarationModifiers left, DeclarationModifiers right)
        {
            return new DeclarationModifiers(left.modifiers | right.modifiers);
        }

        public static DeclarationModifiers operator -(DeclarationModifiers left, DeclarationModifiers right)
        {
            return new DeclarationModifiers(left.modifiers & ~right.modifiers);
        }

        public bool Equals(DeclarationModifiers modifiers)
        {
            return this.modifiers == modifiers.modifiers;
        }

        public override bool Equals(object obj)
        {
            return obj is DeclarationModifiers && Equals((DeclarationModifiers)obj);
        }

        public override int GetHashCode()
        {
            return (int)this.modifiers;
        }

        public static bool operator ==(DeclarationModifiers left, DeclarationModifiers right)
        {
            return left.modifiers == right.modifiers;
        }

        public static bool operator !=(DeclarationModifiers left, DeclarationModifiers right)
        {
            return left.modifiers != right.modifiers;
        }

        public override string ToString()
        {
            return this.modifiers.ToString();
        }
    }
}