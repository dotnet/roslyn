// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeQuality.Analyzers.ApiDesignGuidelines
{
    internal static class C
    {
        public static bool IsAssignableTo(
    [NotNullWhen(returnValue: true)] this ITypeSymbol? fromSymbol,
    [NotNullWhen(returnValue: true)] ITypeSymbol? toSymbol,
    Compilation compilation)
    => fromSymbol != null && toSymbol != null && compilation.ClassifyCommonConversion(fromSymbol, toSymbol).IsImplicit;

        public static bool IsLessSevereThan(this ReportDiagnostic current, ReportDiagnostic other)
        {
            return current switch
            {
                ReportDiagnostic.Error => false,

                ReportDiagnostic.Warn =>
                    other switch
                    {
                        ReportDiagnostic.Error => true,
                        _ => false
                    },

                ReportDiagnostic.Info =>
                    other switch
                    {
                        ReportDiagnostic.Error => true,
                        ReportDiagnostic.Warn => true,
                        _ => false
                    },

                ReportDiagnostic.Hidden =>
                    other switch
                    {
                        ReportDiagnostic.Error => true,
                        ReportDiagnostic.Warn => true,
                        ReportDiagnostic.Info => true,
                        _ => false
                    },

                ReportDiagnostic.Suppress => true,

                _ => false
            };
        }
    }

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


    /// <summary>
    /// CA1019: <inheritdoc cref="DefineAccessorsForAttributeArgumentsTitle"/>
    ///
    /// Cause:
    /// In its constructor, an attribute defines arguments that do not have corresponding properties.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class DefineAccessorsForAttributeArgumentsAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1019";
        internal const string AddAccessorCase = "AddAccessor";
        internal const string MakePublicCase = "MakePublic";
        internal const string RemoveSetterCase = "RemoveSetter";

        private static readonly LocalizableString s_localizableTitle = "DefineAccessorsForAttributeArgumentsTitle";

        internal static readonly DiagnosticDescriptor DefaultRule = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            "DefineAccessorsForAttributeArgumentsTitle",
            "DiagnosticCategory.Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null);

        internal static readonly DiagnosticDescriptor IncreaseVisibilityRule = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            "DefineAccessorsForAttributeArgumentsTitle",
            "DiagnosticCategory.Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null);

        internal static readonly DiagnosticDescriptor RemoveSetterRule = new DiagnosticDescriptor(
            RuleId,
            s_localizableTitle,
            "DefineAccessorsForAttributeArgumentsMessageRemoveSetter",
            "DiagnosticCategory.Design",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: null);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(DefaultRule, IncreaseVisibilityRule, RemoveSetterRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? attributeType = compilationContext.Compilation.GetTypeByMetadataName("System.Attribute");
                if (attributeType == null)
                {
                    return;
                }

                compilationContext.RegisterSymbolAction(context =>
                {
                    AnalyzeSymbol((INamedTypeSymbol)context.Symbol, attributeType, context.Compilation, context.ReportDiagnostic);
                },
                SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbol(INamedTypeSymbol symbol, INamedTypeSymbol attributeType, Compilation compilation, Action<Diagnostic> addDiagnostic)
        {
            if (symbol != null && symbol.GetBaseTypesAndThis().Contains(attributeType) && symbol.DeclaredAccessibility != Accessibility.Private)
            {
                IEnumerable<IParameterSymbol> parametersToCheck = GetAllPublicConstructorParameters(symbol);
                if (parametersToCheck.Any())
                {
                    IDictionary<string, IPropertySymbol> propertiesMap = GetAllPropertiesInTypeChain(symbol);
                    AnalyzeParameters(compilation, parametersToCheck, propertiesMap, symbol, addDiagnostic);
                }
            }
        }

        private static IEnumerable<IParameterSymbol> GetAllPublicConstructorParameters(INamedTypeSymbol attributeType)
        {
            // FxCop compatibility:
            // Only examine parameters of public constructors. Can't use protected
            // constructors to define an attribute so this rule only applies to
            // public constructors.
            IEnumerable<IMethodSymbol> instanceConstructorsToCheck = attributeType.InstanceConstructors.Where(c => c.DeclaredAccessibility == Accessibility.Public);

            if (instanceConstructorsToCheck.Any())
            {
                var uniqueParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (IMethodSymbol constructor in instanceConstructorsToCheck)
                {
                    foreach (IParameterSymbol parameter in constructor.Parameters)
                    {
                        if (uniqueParamNames.Add(parameter.Name))
                        {
                            yield return parameter;
                        }
                    }
                }
            }
        }

        private static IDictionary<string, IPropertySymbol> GetAllPropertiesInTypeChain(INamedTypeSymbol attributeType)
        {
            var propertiesMap = new Dictionary<string, IPropertySymbol>(StringComparer.OrdinalIgnoreCase);
            foreach (INamedTypeSymbol currentType in attributeType.GetBaseTypesAndThis())
            {
                foreach (IPropertySymbol property in currentType.GetMembers().Where(m => m.Kind == SymbolKind.Property).Cast<IPropertySymbol>())
                {
                    if (!propertiesMap.ContainsKey(property.Name))
                    {
                        propertiesMap.Add(property.Name, property);
                    }
                }
            }

            return propertiesMap;
        }

        private static void AnalyzeParameters(Compilation compilation, IEnumerable<IParameterSymbol> parameters, IDictionary<string, IPropertySymbol> propertiesMap, INamedTypeSymbol attributeType, Action<Diagnostic> addDiagnostic)
        {
            foreach (IParameterSymbol parameter in parameters)
            {
                if (parameter.Type.Kind != SymbolKind.ErrorType)
                {
                    if (!propertiesMap.TryGetValue(parameter.Name, out IPropertySymbol property) ||
                        !parameter.Type.IsAssignableTo(property.Type, compilation))
                    {
                        // Add a public read-only property accessor for positional argument '{0}' of attribute '{1}'.
                        addDiagnostic(GetDefaultDiagnostic(parameter, attributeType));
                    }
                    else
                    {
                        if (property.GetMethod == null)
                        {
                            // Add a public read-only property accessor for positional argument '{0}' of attribute '{1}'.
                            addDiagnostic(GetDefaultDiagnostic(parameter, attributeType));
                        }
                        else if (property.DeclaredAccessibility != Accessibility.Public ||
                            property.GetMethod.DeclaredAccessibility != Accessibility.Public)
                        {
                            if (!property.ContainingType.Equals(attributeType))
                            {
                                // A non-public getter exists in one of the base types.
                                // However, we cannot be sure if the user can modify the base type (it could be from a third party library).
                                // So generate the default diagnostic instead of increase visibility here.

                                // Add a public read-only property accessor for positional argument '{0}' of attribute '{1}'.
                                addDiagnostic(GetDefaultDiagnostic(parameter, attributeType));
                            }
                            else
                            {
                                // If '{0}' is the property accessor for positional argument '{1}', make it public.
                                addDiagnostic(GetIncreaseVisibilityDiagnostic(parameter, property));
                            }
                        }

                        if (property.SetMethod != null &&
                            property.SetMethod.DeclaredAccessibility == Accessibility.Public &&
                            Equals(property.ContainingType, attributeType))
                        {
                            // Remove the property setter from '{0}' or reduce its accessibility because it corresponds to positional argument '{1}'.
                            addDiagnostic(GetRemoveSetterDiagnostic(parameter, property));
                        }
                    }
                }
            }
        }

        private static Diagnostic GetDefaultDiagnostic(IParameterSymbol parameter, INamedTypeSymbol attributeType)
        {
            // Add a public read-only property accessor for positional argument '{0}' of attribute '{1}'.
            return parameter.Locations.CreateDiagnostic(DefaultRule, new Dictionary<string, string?> { { "case", AddAccessorCase } }.ToImmutableDictionary(), parameter.Name, attributeType.Name);
        }

        private static Diagnostic GetIncreaseVisibilityDiagnostic(IParameterSymbol parameter, IPropertySymbol property)
        {
            // If '{0}' is the property accessor for positional argument '{1}', make it public.
            return property.GetMethod!.Locations.CreateDiagnostic(IncreaseVisibilityRule, new Dictionary<string, string?> { { "case", MakePublicCase } }.ToImmutableDictionary(), property.Name, parameter.Name);
        }

        private static Diagnostic GetRemoveSetterDiagnostic(IParameterSymbol parameter, IPropertySymbol property)
        {
            // Remove the property setter from '{0}' or reduce its accessibility because it corresponds to positional argument '{1}'.
            return property.SetMethod!.Locations.CreateDiagnostic(RemoveSetterRule, new Dictionary<string, string?> { { "case", RemoveSetterCase } }.ToImmutableDictionary(), property.Name, parameter.Name);
        }
    }
}
