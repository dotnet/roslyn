// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CodeGeneration
{
    public struct SymbolModifiers
    {
        public bool IsStatic { get; private set; }
        public bool IsAbstract { get; private set; }
        public bool IsNew { get; private set; }
        public bool IsUnsafe { get; private set; }
        public bool IsReadOnly { get; private set; }
        public bool IsVirtual { get; private set; }
        public bool IsOverride { get; private set; }
        public bool IsSealed { get; private set; }
        public bool IsConst { get; private set; }
        public bool IsWithEvents { get; private set; }
        public bool IsPartial { get; private set; }
        public bool IsAsync { get; private set; }

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
            : this()
        {
            this.IsStatic = isStatic;
            this.IsAbstract = isAbstract;
            this.IsNew = isNew;
            this.IsUnsafe = isUnsafe;
            this.IsReadOnly = isReadOnly;
            this.IsVirtual = isVirtual;
            this.IsOverride = isOverride;
            this.IsSealed = isSealed;
            this.IsConst = isConst;
            this.IsWithEvents = isWithEvents;
            this.IsPartial = isPartial;
            this.IsAsync = isAsync;
        }

        internal SymbolModifiers WithIsUnsafe(bool isUnsafe)
        {
            return this.IsUnsafe == isUnsafe
                ? this
                : new SymbolModifiers(IsStatic, IsAbstract, IsNew, isUnsafe, IsReadOnly, IsVirtual, IsOverride, IsSealed, IsConst, IsWithEvents);
        }

        internal SymbolModifiers WithIsAbstract(bool isAbstract)
        {
            return this.IsAbstract == isAbstract
                ? this
                : new SymbolModifiers(IsStatic, isAbstract, IsNew, IsUnsafe, IsReadOnly, IsVirtual, IsOverride, IsSealed, IsConst, IsWithEvents);
        }
    }
}