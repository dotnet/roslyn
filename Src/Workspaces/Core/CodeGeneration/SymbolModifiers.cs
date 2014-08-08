// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGeneration
{
    public struct SymbolModifiers
    {
        private readonly Modifiers modifiers;

        private SymbolModifiers(Modifiers modifiers)
        {
            this.modifiers = modifiers;
        }

        public SymbolModifiers(
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

        public static SymbolModifiers From(ISymbol symbol)
        {
            var field = symbol as IFieldSymbol;
            var property = symbol as IPropertySymbol;
            var method = symbol as IMethodSymbol;

            return new SymbolModifiers(
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

        public SymbolModifiers WithIsStatic(bool isStatic)
        {
            return new SymbolModifiers(SetFlag(this.modifiers, Modifiers.Static, isStatic));
        }

        public SymbolModifiers WithIsAbstract(bool isAbstract)
        {
            return new SymbolModifiers(SetFlag(this.modifiers, Modifiers.Abstract, isAbstract));
        }

        public SymbolModifiers WithIsNew(bool isNew)
        {
            return new SymbolModifiers(SetFlag(this.modifiers, Modifiers.New, isNew));
        }

        public SymbolModifiers WithIsUnsafe(bool isUnsafe)
        {
            return new SymbolModifiers(SetFlag(this.modifiers, Modifiers.Unsafe, isUnsafe));
        }

        public SymbolModifiers WithIsReadOnly(bool isReadOnly)
        {
            return new SymbolModifiers(SetFlag(this.modifiers, Modifiers.ReadOnly, isReadOnly));
        }

        public SymbolModifiers WithIsVirtual(bool isVirtual)
        {
            return new SymbolModifiers(SetFlag(this.modifiers, Modifiers.Virtual, isVirtual));
        }

        public SymbolModifiers WithIsOverride(bool isOverride)
        {
            return new SymbolModifiers(SetFlag(this.modifiers, Modifiers.Override, isOverride));
        }

        public SymbolModifiers WithIsSealed(bool isSealed)
        {
            return new SymbolModifiers(SetFlag(this.modifiers, Modifiers.Sealed, isSealed));
        }

        public SymbolModifiers WithIsConst(bool isConst)
        {
            return new SymbolModifiers(SetFlag(this.modifiers, Modifiers.Const, isConst));
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

        public static readonly SymbolModifiers None = default(SymbolModifiers);

        public static readonly SymbolModifiers Static = new SymbolModifiers(Modifiers.Static);
        public static readonly SymbolModifiers Abstract = new SymbolModifiers(Modifiers.Abstract);
        public static readonly SymbolModifiers New = new SymbolModifiers(Modifiers.New);
        public static readonly SymbolModifiers Unsafe = new SymbolModifiers(Modifiers.Unsafe);
        public static readonly SymbolModifiers ReadOnly = new SymbolModifiers(Modifiers.ReadOnly);
        public static readonly SymbolModifiers Virtual = new SymbolModifiers(Modifiers.Virtual);
        public static readonly SymbolModifiers Override = new SymbolModifiers(Modifiers.Override);
        public static readonly SymbolModifiers Sealed = new SymbolModifiers(Modifiers.Sealed);
        public static readonly SymbolModifiers Const = new SymbolModifiers(Modifiers.Const);
        public static readonly SymbolModifiers WithEvents = new SymbolModifiers(Modifiers.WithEvents);
        public static readonly SymbolModifiers Partial = new SymbolModifiers(Modifiers.Partial);
        public static readonly SymbolModifiers Async = new SymbolModifiers(Modifiers.Async);

        public static SymbolModifiers operator |(SymbolModifiers left, SymbolModifiers right)
        {
            return new SymbolModifiers(left.modifiers | right.modifiers);
        }

        public static SymbolModifiers operator &(SymbolModifiers left, SymbolModifiers right)
        {
            return new SymbolModifiers(left.modifiers & right.modifiers);
        }

        public static SymbolModifiers operator +(SymbolModifiers left, SymbolModifiers right)
        {
            return new SymbolModifiers(left.modifiers | right.modifiers);
        }

        public static SymbolModifiers operator -(SymbolModifiers left, SymbolModifiers right)
        {
            return new SymbolModifiers(left.modifiers & ~right.modifiers);
        }
    }
}