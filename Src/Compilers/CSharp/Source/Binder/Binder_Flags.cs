// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// Represents a small change from the enclosing/next binder.
        /// Can specify a BindingLocation and a ContainingMemberOrLambda.
        /// </summary>
        private sealed class BinderWithContainingMemberOrLambda : Binder
        {
            private readonly Symbol containingMemberOrLambda;

            internal BinderWithContainingMemberOrLambda(Binder next, Symbol containingMemberOrLambda)
                : base(next)
            {
                this.containingMemberOrLambda = containingMemberOrLambda;
            }

            internal BinderWithContainingMemberOrLambda(Binder next, BinderFlags flags, Symbol containingMemberOrLambda)
                : base(next, flags)
            {
                this.containingMemberOrLambda = containingMemberOrLambda;
            }

            internal override Symbol ContainingMemberOrLambda
            {
                get { return this.containingMemberOrLambda; }
            }
        }

        /// <summary>
        /// Represents a small change from the enclosing/next binder.
        /// Can specify a receiver Expression for containing conditional member access.
        /// </summary>
        private sealed class BinderWithConditionalReceiver : Binder
        {
            internal readonly BoundExpression receiverExpression;

            internal BinderWithConditionalReceiver(Binder next, BoundExpression receiverExpression)
                : base(next)
            {
                this.receiverExpression = receiverExpression;
            }
        }

        internal Binder WithFlags(BinderFlags flags)
        {
            return this.Flags == flags
                ? this
                : new Binder(this, flags);
        }

        internal Binder WithAdditionalFlags(BinderFlags flags)
        {
            return this.Flags.Includes(flags)
                ? this
                : new Binder(this, this.Flags | flags);
        }

        internal Binder WithContainingMemberOrLambda(Symbol containing)
        {
            Debug.Assert((object)containing != null);
            return new BinderWithContainingMemberOrLambda(this, containing);
        }

        /// <remarks>
        /// It seems to be common to do both of these things at once, so provide a way to do so
        /// without adding two links to the binder chain.
        /// </remarks>
        internal Binder WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags flags, Symbol containing)
        {
            Debug.Assert((object)containing != null);
            return new BinderWithContainingMemberOrLambda(this, this.Flags | flags, containing);
        }

        internal Binder WithUnsafeRegionIfNecessary(SyntaxTokenList modifiers)
        {
            return (this.Flags.Includes(BinderFlags.UnsafeRegion) || !modifiers.Any(SyntaxKind.UnsafeKeyword))
                ? this
                : new Binder(this, this.Flags | BinderFlags.UnsafeRegion);
        }

        internal Binder WithCheckedOrUncheckedRegion(bool @checked)
        {
            Debug.Assert(!this.Flags.Includes(BinderFlags.UncheckedRegion | BinderFlags.CheckedRegion));

            BinderFlags added = @checked ? BinderFlags.CheckedRegion : BinderFlags.UncheckedRegion;
            BinderFlags removed = @checked ? BinderFlags.UncheckedRegion : BinderFlags.CheckedRegion;

            return this.Flags.Includes(added)
                ? this
                : new Binder(this, (this.Flags & ~removed) | added);
        }
    }
}
