// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal abstract partial class Binder
    {
        /// <summary>
        /// Represents a small change from the enclosing/next binder.
        /// Can specify a BindingLocation and/or a ContainingMemberOrLambda.
        /// </summary>
        private sealed class DeltaBinder : Binder
        {
            private readonly Symbol containingMemberOrLambda;

            internal DeltaBinder(BinderFlags location, Binder next, Symbol containingMemberOrLambda = null)
                : base(next, location)
            {
                this.containingMemberOrLambda = containingMemberOrLambda ?? Next.ContainingMemberOrLambda;
            }

            internal override Symbol ContainingMemberOrLambda
            {
                get { return this.containingMemberOrLambda; }
            }
        }

        internal Binder WithFlags(BinderFlags flags)
        {
            return this.Flags == flags
                ? this
                : new DeltaBinder(flags, this);
        }

        internal Binder WithAdditionalFlags(BinderFlags flags)
        {
            return this.Flags.Includes(flags)
                ? this
                : new DeltaBinder(this.Flags | flags, this);
        }

        internal Binder WithContainingMemberOrLambda(Symbol containing)
        {
            Debug.Assert((object)containing != null);
            return new DeltaBinder(this.Flags, this, containing);
        }

        /// <remarks>
        /// It seems to be common to do both of these things at once, so provide a way to do so
        /// without adding two links to the binder chain.
        /// </remarks>
        internal Binder WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags flags, Symbol containing)
        {
            Debug.Assert((object)containing != null);
            return new DeltaBinder(this.Flags | flags, this, containing);
        }

        internal Binder WithUnsafeRegionIfNecessary(SyntaxTokenList modifiers)
        {
            return (this.Flags.Includes(BinderFlags.UnsafeRegion) || !modifiers.Any(SyntaxKind.UnsafeKeyword))
                ? this
                : new DeltaBinder(this.Flags | BinderFlags.UnsafeRegion, this);
        }

        internal Binder WithCheckedOrUncheckedRegion(bool @checked)
        {
            Debug.Assert(!this.Flags.Includes(BinderFlags.UncheckedRegion | BinderFlags.CheckedRegion));

            BinderFlags added = @checked ? BinderFlags.CheckedRegion : BinderFlags.UncheckedRegion;
            BinderFlags removed = @checked ? BinderFlags.UncheckedRegion : BinderFlags.CheckedRegion;

            return this.Flags.Includes(added)
                ? this
                : new DeltaBinder((this.Flags & ~removed) | added, this);
        }
    }
}
