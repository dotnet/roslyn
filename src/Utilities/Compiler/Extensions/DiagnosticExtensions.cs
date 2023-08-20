// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer.Utilities.Extensions
{
    internal static class DiagnosticExtensions
    {
        public static Diagnostic CreateDiagnostic(
            this SyntaxNode node,
            DiagnosticDescriptor rule,
            params object[] args)
            => node.CreateDiagnostic(rule, properties: null, args);

        public static Diagnostic CreateDiagnostic(
            this SyntaxNode node,
            DiagnosticDescriptor rule,
            ImmutableDictionary<string, string?>? properties,
            params object[] args)
            => node.CreateDiagnostic(rule, additionalLocations: ImmutableArray<Location>.Empty, properties, args);

        public static Diagnostic CreateDiagnostic(
            this SyntaxNode node,
            DiagnosticDescriptor rule,
            ImmutableArray<Location> additionalLocations,
            ImmutableDictionary<string, string?>? properties,
            params object[] args)
            => node
                .GetLocation()
                .CreateDiagnostic(
                    rule: rule,
                    additionalLocations: additionalLocations,
                    properties: properties,
                    args: args);

        public static Diagnostic CreateDiagnostic(
            this IOperation operation,
            DiagnosticDescriptor rule,
            params object[] args)
            => operation.CreateDiagnostic(rule, properties: null, args);

        public static Diagnostic CreateDiagnostic(
            this IOperation operation,
            DiagnosticDescriptor rule,
            ImmutableDictionary<string, string?>? properties,
            params object[] args)
        {
            return operation.Syntax.CreateDiagnostic(rule, properties, args);
        }

        public static Diagnostic CreateDiagnostic(
            this IOperation operation,
            DiagnosticDescriptor rule,
            ImmutableArray<Location> additionalLocations,
            ImmutableDictionary<string, string?>? properties,
            params object[] args)
        {
            return operation.Syntax.CreateDiagnostic(rule, additionalLocations, properties, args);
        }

        public static Diagnostic CreateDiagnostic(
            this SyntaxToken token,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            return token.GetLocation().CreateDiagnostic(rule, args);
        }

        public static Diagnostic CreateDiagnostic(
            this ISymbol symbol,
            DiagnosticDescriptor rule,
            params object[] args)
        {
            return symbol.Locations.CreateDiagnostic(rule, args);
        }

        public static Diagnostic CreateDiagnostic(
            this ISymbol symbol,
            DiagnosticDescriptor rule,
            ImmutableDictionary<string, string?>? properties,
            params object[] args)
        {
            return symbol.Locations.CreateDiagnostic(rule, properties, args);
        }

        public static Diagnostic CreateDiagnostic(
            this Location location,
            DiagnosticDescriptor rule,
            params object[] args)
            => location
                .CreateDiagnostic(
                    rule: rule,
                    properties: ImmutableDictionary<string, string?>.Empty,
                    args: args);

        public static Diagnostic CreateDiagnostic(
            this Location location,
            DiagnosticDescriptor rule,
            ImmutableDictionary<string, string?>? properties,
            params object[] args)
            => location.CreateDiagnostic(rule, ImmutableArray<Location>.Empty, properties, args);

        public static Diagnostic CreateDiagnostic(
            this Location location,
            DiagnosticDescriptor rule,
            ImmutableArray<Location> additionalLocations,
            ImmutableDictionary<string, string?>? properties,
            params object[] args)
        {
            if (!location.IsInSource)
            {
                location = Location.None;
            }

            return Diagnostic.Create(
                descriptor: rule,
                location: location,
                additionalLocations: additionalLocations,
                properties: properties,
                messageArgs: args);
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
            ImmutableDictionary<string, string?>? properties,
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

        /// <summary>
        /// TODO: Revert this reflection based workaround once we move to Microsoft.CodeAnalysis version 3.0
        /// </summary>
        private static readonly PropertyInfo? s_syntaxTreeDiagnosticOptionsProperty =
            typeof(SyntaxTree).GetTypeInfo().GetDeclaredProperty("DiagnosticOptions");

        private static readonly PropertyInfo? s_compilationOptionsSyntaxTreeOptionsProviderProperty =
            typeof(CompilationOptions).GetTypeInfo().GetDeclaredProperty("SyntaxTreeOptionsProvider");

        public static void ReportNoLocationDiagnostic(
            this CompilationAnalysisContext context,
            DiagnosticDescriptor rule,
            params object[] args)
            => context.Compilation.ReportNoLocationDiagnostic(rule, context.ReportDiagnostic, properties: null, args);

        public static void ReportNoLocationDiagnostic(
            this SyntaxNodeAnalysisContext context,
            DiagnosticDescriptor rule,
            params object[] args)
            => context.Compilation.ReportNoLocationDiagnostic(rule, context.ReportDiagnostic, properties: null, args);

        public static void ReportNoLocationDiagnostic(
            this Compilation compilation,
            DiagnosticDescriptor rule,
            Action<Diagnostic> addDiagnostic,
            ImmutableDictionary<string, string?>? properties,
            params object[] args)
        {
            var effectiveSeverity = GetEffectiveSeverity();
            if (!effectiveSeverity.HasValue)
            {
                // Disabled rule
                return;
            }

            if (effectiveSeverity.Value != rule.DefaultSeverity)
            {
#pragma warning disable RS0030 // The symbol 'DiagnosticDescriptor.DiagnosticDescriptor.#ctor' is banned in this project: Use 'DiagnosticDescriptorHelper.Create' instead
                rule = new DiagnosticDescriptor(rule.Id, rule.Title, rule.MessageFormat, rule.Category,
                    effectiveSeverity.Value, rule.IsEnabledByDefault, rule.Description, rule.HelpLinkUri, rule.CustomTags.ToArray());
#pragma warning restore RS0030
            }

            var diagnostic = Diagnostic.Create(rule, Location.None, properties, args);
            addDiagnostic(diagnostic);
            return;

            DiagnosticSeverity? GetEffectiveSeverity()
            {
                // Microsoft.CodeAnalysis version >= 3.7 exposes options through 'CompilationOptions.SyntaxTreeOptionsProvider.TryGetDiagnosticValue'
                // Microsoft.CodeAnalysis version 3.3 - 3.7 exposes options through 'SyntaxTree.DiagnosticOptions'. This API is deprecated in 3.7.

                var syntaxTreeOptionsProvider = s_compilationOptionsSyntaxTreeOptionsProviderProperty?.GetValue(compilation.Options);
                var syntaxTreeOptionsProviderTryGetDiagnosticValueMethod = syntaxTreeOptionsProvider?.GetType().GetRuntimeMethods().FirstOrDefault(m => m.Name == "TryGetDiagnosticValue");
                if (syntaxTreeOptionsProviderTryGetDiagnosticValueMethod == null && s_syntaxTreeDiagnosticOptionsProperty == null)
                {
                    return rule.DefaultSeverity;
                }

                ReportDiagnostic? overriddenSeverity = null;
                foreach (var tree in compilation.SyntaxTrees)
                {
                    ReportDiagnostic? configuredValue = null;

                    // Prefer 'CompilationOptions.SyntaxTreeOptionsProvider', if available.
                    if (s_compilationOptionsSyntaxTreeOptionsProviderProperty != null)
                    {
                        if (syntaxTreeOptionsProviderTryGetDiagnosticValueMethod != null)
                        {
                            // public abstract bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, out ReportDiagnostic severity);
                            // public abstract bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, CancellationToken cancellationToken, out ReportDiagnostic severity);
                            object?[] parameters;
                            if (syntaxTreeOptionsProviderTryGetDiagnosticValueMethod.GetParameters().Length == 3)
                            {
                                parameters = new object?[] { tree, rule.Id, null };
                            }
                            else
                            {
                                parameters = new object?[] { tree, rule.Id, CancellationToken.None, null };
                            }

                            if (syntaxTreeOptionsProviderTryGetDiagnosticValueMethod.Invoke(syntaxTreeOptionsProvider, parameters) is true &&
                                parameters.Last() is ReportDiagnostic value)
                            {
                                configuredValue = value;
                            }
                        }
                    }
                    else
                    {
                        RoslynDebug.Assert(s_syntaxTreeDiagnosticOptionsProperty != null);
                        var options = (ImmutableDictionary<string, ReportDiagnostic>)s_syntaxTreeDiagnosticOptionsProperty.GetValue(tree)!;
                        if (options.TryGetValue(rule.Id, out var value))
                        {
                            configuredValue = value;
                        }
                    }

                    if (configuredValue == null)
                    {
                        continue;
                    }

                    if (configuredValue == ReportDiagnostic.Suppress)
                    {
                        // Any suppression entry always wins.
                        return null;
                    }

                    if (overriddenSeverity == null)
                    {
                        overriddenSeverity = configuredValue;
                    }
                    else if (overriddenSeverity.Value.IsLessSevereThan(configuredValue.Value))
                    {
                        // Choose the most severe value for conflicts.
                        overriddenSeverity = configuredValue;
                    }
                }

                return overriddenSeverity.HasValue ? overriddenSeverity.Value.ToDiagnosticSeverity() : rule.DefaultSeverity;
            }
        }
    }
}
