// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Groups the information computed by MakeOverriddenOrHiddenMembers.
    /// </summary>
    internal sealed class OverriddenOrHiddenMembersResult
    {
        public static readonly OverriddenOrHiddenMembersResult Empty =
            new OverriddenOrHiddenMembersResult(
                ImmutableArray<Symbol>.Empty,
                ImmutableArray<Symbol>.Empty,
                ImmutableArray<Symbol>.Empty);

        private readonly ImmutableArray<Symbol> overriddenMembers;
        public ImmutableArray<Symbol> OverriddenMembers { get { return overriddenMembers; } }

        private readonly ImmutableArray<Symbol> hiddenMembers;
        public ImmutableArray<Symbol> HiddenMembers { get { return hiddenMembers; } }

        private readonly ImmutableArray<Symbol> runtimeOverriddenMembers;
        public ImmutableArray<Symbol> RuntimeOverriddenMembers { get { return this.runtimeOverriddenMembers; } }

        private OverriddenOrHiddenMembersResult(
            ImmutableArray<Symbol> overriddenMembers,
            ImmutableArray<Symbol> hiddenMembers,
            ImmutableArray<Symbol> runtimeOverriddenMembers)
        {
            this.overriddenMembers = overriddenMembers;
            this.hiddenMembers = hiddenMembers;
            this.runtimeOverriddenMembers = runtimeOverriddenMembers;
        }

        public static OverriddenOrHiddenMembersResult Create(
            ImmutableArray<Symbol> overriddenMembers,
            ImmutableArray<Symbol> hiddenMembers,
            ImmutableArray<Symbol> runtimeOverriddenMembers)
        {
            if (overriddenMembers.IsEmpty && hiddenMembers.IsEmpty && runtimeOverriddenMembers.IsEmpty)
            {
                return Empty;
            }
            else
            {
                return new OverriddenOrHiddenMembersResult(overriddenMembers, hiddenMembers, runtimeOverriddenMembers);
            }
        }

        internal Symbol GetOverriddenMember()
        {
            foreach (var overriddenMember in overriddenMembers)
            {
                if (overriddenMember.IsAbstract || overriddenMember.IsVirtual || overriddenMember.IsOverride)
                {
                    return overriddenMember;
                }
            }

            return null;
        }
    }
}