// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal class NamingStyleRules
    {
        public ImmutableArray<NamingRule> NamingRules { get; }

        private readonly ImmutableArray<SymbolKind> _symbolKindsThatCanBeOverridden =
            ImmutableArray.Create(
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Event);

        public NamingStyleRules(ImmutableArray<NamingRule> namingRules)
        {
            NamingRules = namingRules;
        }

        internal bool TryGetApplicableRule(ISymbol symbol, out NamingRule applicableRule)
        {
            if (NamingRules != null &&
                IsSymbolNameAnalyzable(symbol))
            {
                foreach (var namingRule in NamingRules)
                {
                    if (namingRule.SymbolSpecification.AppliesTo(symbol))
                    {
                        applicableRule = namingRule;
                        return true;
                    }
                }
            }

            applicableRule = default;
            return false;
        }

        private bool IsSymbolNameAnalyzable(ISymbol symbol)
        {
            if (_symbolKindsThatCanBeOverridden.Contains(symbol.Kind) && DoesSymbolImplementAnotherSymbol(symbol))
            {
                return false;
            }

            if (symbol is IMethodSymbol method)
            {
                return method.MethodKind == MethodKind.Ordinary ||
                       method.MethodKind == MethodKind.LocalFunction;
            }

            if (symbol is IPropertySymbol property)
            {
                return !property.IsIndexer;
            }

            return true;
        }

        private bool DoesSymbolImplementAnotherSymbol(ISymbol symbol)
        {
            if (symbol.IsStatic)
            {
                return false;
            }

            var containingType = symbol.ContainingType;
            if (containingType.TypeKind != TypeKind.Class && containingType.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            return symbol.IsOverride ||
                symbol.ExplicitInterfaceImplementations().Any() ||
                IsInterfaceImplementation(symbol);
        }

        /// <summary>
        /// This does not handle the case where a method in a base type implicitly implements an
        /// interface method on behalf of one of its derived types.
        /// </summary>
        private bool IsInterfaceImplementation(ISymbol symbol)
        {
            if (symbol.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            var containingType = symbol.ContainingType;
            var implementedInterfaces = containingType.AllInterfaces;

            foreach (var implementedInterface in implementedInterfaces)
            {
                var implementedInterfaceMembersWithSameName = implementedInterface.GetMembers(symbol.Name);
                foreach (var implementedInterfaceMember in implementedInterfaceMembersWithSameName)
                {
                    if (symbol.Equals(containingType.FindImplementationForInterfaceMember(implementedInterfaceMember)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
