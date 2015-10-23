// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

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

        private readonly ImmutableArray<Symbol> _overriddenMembers;
        public ImmutableArray<Symbol> OverriddenMembers { get { return _overriddenMembers; } }

        private readonly ImmutableArray<Symbol> _hiddenMembers;
        public ImmutableArray<Symbol> HiddenMembers { get { return _hiddenMembers; } }

        private readonly ImmutableArray<Symbol> _runtimeOverriddenMembers;
        public ImmutableArray<Symbol> RuntimeOverriddenMembers { get { return _runtimeOverriddenMembers; } }

        private OverriddenOrHiddenMembersResult(
            ImmutableArray<Symbol> overriddenMembers,
            ImmutableArray<Symbol> hiddenMembers,
            ImmutableArray<Symbol> runtimeOverriddenMembers)
        {
            _overriddenMembers = overriddenMembers;
            _hiddenMembers = hiddenMembers;
            _runtimeOverriddenMembers = runtimeOverriddenMembers;
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
            foreach (var overriddenMember in _overriddenMembers)
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
