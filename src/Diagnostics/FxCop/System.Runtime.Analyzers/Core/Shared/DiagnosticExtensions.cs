// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace System.Runtime.Analyzers
{
    internal static class DiagnosticExtensions
    {
        public static IEnumerable<Diagnostic> CreateDiagnostics(
            this IEnumerable<SyntaxNode> nodes,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            foreach (var node in nodes)
            {
                yield return node.CreateDiagnostic(rule, args);
            }
        }

        public static Diagnostic CreateDiagnostic(
            this SyntaxNode node,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            return node.GetLocation().CreateDiagnostic(rule, args);
        }

        public static IEnumerable<Diagnostic> CreateDiagnostics(
            this IEnumerable<SyntaxToken> tokens,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            foreach (var token in tokens)
            {
                yield return token.CreateDiagnostic(rule, args);
            }
        }

        public static Diagnostic CreateDiagnostic(
            this SyntaxToken token,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            return token.GetLocation().CreateDiagnostic(rule, args);
        }

        public static IEnumerable<Diagnostic> CreateDiagnostics(
            this IEnumerable<SyntaxNodeOrToken> nodesOrTokens,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            foreach (var nodeOrToken in nodesOrTokens)
            {
                yield return nodeOrToken.CreateDiagnostic(rule, args);
            }
        }

        public static Diagnostic CreateDiagnostic(
            this SyntaxNodeOrToken nodeOrToken,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            return nodeOrToken.GetLocation().CreateDiagnostic(rule, args);
        }

        public static IEnumerable<Diagnostic> CreateDiagnostics(
            this IEnumerable<ISymbol> symbols,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            foreach (var symbol in symbols)
            {
                yield return symbol.CreateDiagnostic(rule, args);
            }
        }

        public static Diagnostic CreateDiagnostic(
            this ISymbol symbol,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            return symbol.Locations.CreateDiagnostic(rule, args);
        }

        public static Diagnostic CreateDiagnostic(
            this Location location,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            if (!location.IsInSource)
            {
                return Diagnostic.Create(rule, null, args);
            }

            return Diagnostic.Create(rule, location, args);
        }

        public static IEnumerable<Diagnostic> CreateDiagnostics(
            this IEnumerable<IEnumerable<Location>> setOfLocations,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            foreach (var locations in setOfLocations)
            {
                yield return locations.CreateDiagnostic(rule, args);
            }
        }

        public static Diagnostic CreateDiagnostic(
            this IEnumerable<Location> locations,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            var location = locations.First(l => l.IsInSource);
            var additionalLocations = locations.Where(l => l.IsInSource).Skip(1);
            return Diagnostic.Create(rule,
                     location: location,
                     additionalLocations: additionalLocations,
                     messageArgs: args);
        }

        public static Diagnostic CreateDiagnostic(
            this IEnumerable<Location> locations,
            DiagnosticDescriptor rule,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            var location = locations.First(l => l.IsInSource);
            var additionalLocations = locations.Where(l => l.IsInSource).Skip(1);
            return Diagnostic.Create(rule,
                     location: location,
                     additionalLocations: additionalLocations,
                     properties: properties,
                     messageArgs: args);
        }
    }
}
