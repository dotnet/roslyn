// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1019: Define accessors for attribute arguments
    /// 
    /// Cause:
    /// In its constructor, an attribute defines arguments that do not have corresponding properties.
    /// </summary>
    public abstract class DefineAccessorsForAttributeArgumentsAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1019";
        internal const string AddAccessorCase = "AddAccessor";
        internal const string MakePublicCase = "MakePublic";
        internal const string RemoveSetterCase = "RemoveSetter";
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArguments), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                         s_localizableTitle,
                                                                         "{0}",
                                                                         DiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: false,
                                                                         helpLinkUri: "http://msdn.microsoft.com/library/ms182136.aspx",
                                                                         customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(compilationContext =>
            {
                var attributeType = WellKnownTypes.IDisposable(compilationContext.Compilation);
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

        private void AnalyzeSymbol(INamedTypeSymbol symbol, INamedTypeSymbol attributeType, Compilation compilation, Action<Diagnostic> addDiagnostic)
        {
            if (symbol != null && symbol.GetBaseTypesAndThis().Contains(WellKnownTypes.Attribute(compilation)) && symbol.DeclaredAccessibility != Accessibility.Private)
            {
                IEnumerable<IParameterSymbol> parametersToCheck = GetAllPublicConstructorParameters(symbol);
                if (parametersToCheck.Any())
                {
                    IDictionary<string, IPropertySymbol> propertiesMap = GetAllPropertiesInTypeChain(symbol);
                    AnalyzeParameters(compilation, parametersToCheck, propertiesMap, symbol, addDiagnostic);
                }
            }
        }

        protected abstract bool IsAssignableTo(ITypeSymbol fromSymbol, ITypeSymbol toSymbol, Compilation compilation);

        private static IEnumerable<IParameterSymbol> GetAllPublicConstructorParameters(INamedTypeSymbol attributeType)
        {
            // FxCop compability:
            // Only examine parameters of public constructors. Can't use protected 
            // constructors to define an attribute so this rule only applies to
            // public constructors.
            var instanceConstructorsToCheck = attributeType.InstanceConstructors.Where(c => c.DeclaredAccessibility == Accessibility.Public);

            if (instanceConstructorsToCheck.Any())
            {
                var uniqueParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var constructor in instanceConstructorsToCheck)
                {
                    foreach (var parameter in constructor.Parameters)
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
            foreach (var currentType in attributeType.GetBaseTypesAndThis())
            {
                foreach (IPropertySymbol property in currentType.GetMembers().Where(m => m.Kind == SymbolKind.Property))
                {
                    if (!propertiesMap.ContainsKey(property.Name))
                    {
                        propertiesMap.Add(property.Name, property);
                    }
                }
            }

            return propertiesMap;
        }

        private void AnalyzeParameters(Compilation compilation, IEnumerable<IParameterSymbol> parameters, IDictionary<string, IPropertySymbol> propertiesMap, INamedTypeSymbol attributeType, Action<Diagnostic> addDiagnostic)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Type.Kind != SymbolKind.ErrorType)
                {
                    IPropertySymbol property;
                    if (!propertiesMap.TryGetValue(parameter.Name, out property) ||
                        !IsAssignableTo(parameter.Type, property.Type, compilation))
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
                            property.ContainingType == attributeType)
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
            var message = string.Format(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArgumentsDefault, parameter.Name, attributeType.Name);
            return parameter.Locations.CreateDiagnostic(Rule, new Dictionary<string, string> { { "case", AddAccessorCase } }.ToImmutableDictionary(), message);
        }

        private static Diagnostic GetIncreaseVisibilityDiagnostic(IParameterSymbol parameter, IPropertySymbol property)
        {
            // If '{0}' is the property accessor for positional argument '{1}', make it public.
            var message = string.Format(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArgumentsIncreaseVisibility, property.Name, parameter.Name);
            return property.GetMethod.Locations.CreateDiagnostic(Rule, new Dictionary<string, string> { { "case", MakePublicCase } }.ToImmutableDictionary(), message);
        }

        private static Diagnostic GetRemoveSetterDiagnostic(IParameterSymbol parameter, IPropertySymbol property)
        {
            // Remove the property setter from '{0}' or reduce its accessibility because it corresponds to positional argument '{1}'.
            var message = string.Format(SystemRuntimeAnalyzersResources.DefineAccessorsForAttributeArgumentsRemoveSetter, property.Name, parameter.Name);
            return property.SetMethod.Locations.CreateDiagnostic(Rule, new Dictionary<string, string> { { "case", RemoveSetterCase } }.ToImmutableDictionary(), message);
        }
    }
}
