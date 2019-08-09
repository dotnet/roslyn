// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities.Extensions
{
    internal static class DiagnosticExtensions
    {
        public static IEnumerable<Diagnostic> CreateDiagnostics(
            this IEnumerable<SyntaxNode> nodes,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            foreach (SyntaxNode node in nodes)
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

        public static Diagnostic CreateDiagnostic(
            this SyntaxNode node,
            DiagnosticDescriptor rule,
            ImmutableDictionary<string, string> properties,
            params object[] args)
            => node
                .GetLocation()
                .CreateDiagnostic(
                    rule: rule,
                    properties: properties,
                    args: args);

        public static Diagnostic CreateDiagnostic(
            this IOperation operation,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            return operation.Syntax.CreateDiagnostic(rule, args);
        }

        public static IEnumerable<Diagnostic> CreateDiagnostics(
            this IEnumerable<SyntaxToken> tokens,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            foreach (SyntaxToken token in tokens)
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
            foreach (SyntaxNodeOrToken nodeOrToken in nodesOrTokens)
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
            foreach (ISymbol symbol in symbols)
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
            => location
                .CreateDiagnostic(
                    rule: rule,
                    properties: ImmutableDictionary<string, string>.Empty,
                    args: args);

        public static Diagnostic CreateDiagnostic(
            this Location location,
            DiagnosticDescriptor rule,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            if (!location.IsInSource)
            {
                location = null;
            }

            return Diagnostic.Create(
                descriptor: rule,
                location: location,
                properties: properties,
                messageArgs: args);
        }

        public static IEnumerable<Diagnostic> CreateDiagnostics(
            this IEnumerable<IEnumerable<Location>> setOfLocations,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            foreach (IEnumerable<Location> locations in setOfLocations)
            {
                yield return locations.CreateDiagnostic(rule, args);
            }
        }

        public static Diagnostic CreateDiagnostic(
            this IEnumerable<Location> locations,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            return locations.CreateDiagnostic(rule, null, args);
        }

        public static Diagnostic CreateDiagnostic(
            this IEnumerable<Location> locations,
            DiagnosticDescriptor rule,
            ImmutableDictionary<string, string> properties,
            params object[] args)
        {
            IEnumerable<Location> inSource = locations.Where(l => l.IsInSource);
            if (!inSource.Any())
            {
                return Diagnostic.Create(rule, null, args);
            }

            return Diagnostic.Create(rule,
                     location: inSource.First(),
                     additionalLocations: inSource.Skip(1),
                     properties: properties,
                     messageArgs: args);
        }
    }
}
