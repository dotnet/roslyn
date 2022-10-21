// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using Roslyn.Utilities;

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
                ImmutableArray<Symbol>.Empty);

        private readonly ImmutableArray<Symbol> _overriddenMembers;
        public ImmutableArray<Symbol> OverriddenMembers { get { return _overriddenMembers; } }

        private readonly ImmutableArray<Symbol> _hiddenMembers;
        public ImmutableArray<Symbol> HiddenMembers { get { return _hiddenMembers; } }

        private OverriddenOrHiddenMembersResult(
            ImmutableArray<Symbol> overriddenMembers,
            ImmutableArray<Symbol> hiddenMembers)
        {
            _overriddenMembers = overriddenMembers;
            _hiddenMembers = hiddenMembers;
        }

        public static OverriddenOrHiddenMembersResult Create(
            ImmutableArray<Symbol> overriddenMembers,
            ImmutableArray<Symbol> hiddenMembers)
        {
            if (overriddenMembers.IsEmpty && hiddenMembers.IsEmpty)
            {
                return Empty;
            }
            else
            {
                return new OverriddenOrHiddenMembersResult(overriddenMembers, hiddenMembers);
            }
        }

        internal static Symbol GetOverriddenMember(Symbol substitutedOverridingMember, Symbol overriddenByDefinitionMember)
        {
            Debug.Assert(!substitutedOverridingMember.IsDefinition);

            if ((object)overriddenByDefinitionMember != null)
            {
                NamedTypeSymbol overriddenByDefinitionContaining = overriddenByDefinitionMember.ContainingType;
                NamedTypeSymbol overriddenByDefinitionContainingTypeDefinition = overriddenByDefinitionContaining.OriginalDefinition;
                for (NamedTypeSymbol baseType = substitutedOverridingMember.ContainingType.BaseTypeNoUseSiteDiagnostics;
                    (object)baseType != null;
                    baseType = baseType.BaseTypeNoUseSiteDiagnostics)
                {
                    if (TypeSymbol.Equals(baseType.OriginalDefinition, overriddenByDefinitionContainingTypeDefinition, TypeCompareKind.ConsiderEverything2))
                    {
                        if (TypeSymbol.Equals(baseType, overriddenByDefinitionContaining, TypeCompareKind.ConsiderEverything2))
                        {
                            return overriddenByDefinitionMember;
                        }

                        return overriddenByDefinitionMember.OriginalDefinition.SymbolAsMember(baseType);
                    }
                }

                throw ExceptionUtilities.Unreachable();
            }

            return null;
        }

        /// <summary>
        /// It is not suitable to call this method on a <see cref="OverriddenOrHiddenMembersResult"/> object
        /// associated with a member within substituted type, <see cref="GetOverriddenMember(Symbol, Symbol)"/>
        /// should be used instead.
        /// </summary>
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
