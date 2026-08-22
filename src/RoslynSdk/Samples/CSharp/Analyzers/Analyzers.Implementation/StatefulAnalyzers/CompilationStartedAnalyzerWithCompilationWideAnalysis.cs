// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sample.Analyzers
{
    /// <summary>
    /// Analyzer to demonstrate compilation-wide analysis.
    /// <para>
    /// Analysis scenario:
    /// (a) You have an interface, which is a well-known secure interface, i.e. it is a marker for all secure types in an assembly.
    /// (b) You have a method level attribute which marks the owning method as unsecure. An interface which has any member with such an attribute, must be considered unsecure.
    /// (c) We want to report diagnostics for types implementing the well-known secure interface that also implement any unsecure interface.
    /// 
    /// Analyzer performs compilation-wide analysis to detect such violating types and reports diagnostics for them in the compilation end action.
    /// </para>
    /// <para>
    /// The analyzer performs this analysis by registering:
    /// (a) A compilation start action, which initializes per-compilation state:
    ///     (i) Immutable state: We fetch and store the type symbols for the well-known secure interface type and unsecure method attribute type in the compilation.
    ///     (ii) Mutable state: We maintain a set of all types implementing well-known secure interface type and set of all interface types with an unsecure method.
    /// (b) A compilation symbol action, which identifies all named types that implement the well-known secure interface, and all method symbols that have the unsecure method attribute.
    /// (c) A compilation end action which reports diagnostics for types implementing the well-known secure interface that also implementing any unsecure interface.
    /// </para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CompilationStartedAnalyzerWithCompilationWideAnalysis : DiagnosticAnalyzer
    {
        private const string Title = "Secure types must not implement interfaces with unsecure methods";
        public const string MessageFormat = "Type '{0}' is a secure type as it implements interface '{1}', but it also implements interface '{2}' which has unsecure method(s).";
        private const string Description = "Secure types must not implement interfaces with unsecure methods.";

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.CompilationStartedAnalyzerWithCompilationWideAnalysisRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.Stateful,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: Description,
                customTags: WellKnownDiagnosticTags.CompilationEnd);

        public const string UnsecureMethodAttributeName = "MyNamespace.UnsecureMethodAttribute";
        public const string SecureTypeInterfaceName = "MyNamespace.ISecureType";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Check if the attribute type marking unsecure methods is defined.
                INamedTypeSymbol unsecureMethodAttributeType = compilationContext.Compilation.GetTypeByMetadataName(UnsecureMethodAttributeName);
                if (unsecureMethodAttributeType == null)
                {
                    return;
                }

                // Check if the interface type marking secure types is defined.
                INamedTypeSymbol secureTypeInterfaceType = compilationContext.Compilation.GetTypeByMetadataName(SecureTypeInterfaceName);
                if (secureTypeInterfaceType == null)
                {
                    return;
                }

                // Initialize state in the start action.
                CompilationAnalyzer analyzer = new CompilationAnalyzer(unsecureMethodAttributeType, secureTypeInterfaceType);

                // Register an intermediate non-end action that accesses and modifies the state.
                compilationContext.RegisterSymbolAction(analyzer.AnalyzeSymbol, SymbolKind.NamedType, SymbolKind.Method);

                // Register an end action to report diagnostics based on the final state.
                compilationContext.RegisterCompilationEndAction(analyzer.CompilationEndAction);
            });
        }

        private class CompilationAnalyzer
        {
            private readonly INamedTypeSymbol _unsecureMethodAttributeType;
            private readonly INamedTypeSymbol _secureTypeInterfaceType;

            /// <summary>
            /// List of secure types in the compilation implementing interface <see cref="SecureTypeInterfaceName"/>.
            /// </summary>
            private readonly List<INamedTypeSymbol> _secureTypes = new List<INamedTypeSymbol>();

            /// <summary>
            /// Set of unsecure interface types in the compilation that have methods with an attribute of <see cref="_unsecureMethodAttributeType"/>.
            /// </summary>
            private readonly HashSet<INamedTypeSymbol> _interfacesWithUnsecureMethods = new HashSet<INamedTypeSymbol>();

            public CompilationAnalyzer(INamedTypeSymbol unsecureMethodAttributeType, INamedTypeSymbol secureTypeInterfaceType)
            {
                _unsecureMethodAttributeType = unsecureMethodAttributeType;
                _secureTypeInterfaceType = secureTypeInterfaceType;
            }

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                switch (context.Symbol.Kind)
                {
                    case SymbolKind.NamedType:
                        // Check if the symbol implements "_secureTypeInterfaceType".
                        INamedTypeSymbol namedType = (INamedTypeSymbol)context.Symbol;
                        if (namedType.AllInterfaces.Contains(_secureTypeInterfaceType))
                        {
                            lock (_secureTypes)
                            {
                                _secureTypes.Add(namedType);
                            }
                        }

                        break;

                    case SymbolKind.Method:
                        // Check if this is an interface method with "_unsecureMethodAttributeType" attribute.
                        IMethodSymbol method = (IMethodSymbol)context.Symbol;
                        if (method.ContainingType.TypeKind == TypeKind.Interface &&
                            method.GetAttributes().Any(a => a.AttributeClass.Equals(_unsecureMethodAttributeType)))
                        {
                            lock (_interfacesWithUnsecureMethods)
                            {
                                _interfacesWithUnsecureMethods.Add(method.ContainingType);
                            }
                        }

                        break;
                }
            }

            public void CompilationEndAction(CompilationAnalysisContext context)
            {
                if (_interfacesWithUnsecureMethods.Count == 0 || _secureTypes.Count == 0)
                {
                    // No violating types.
                    return;
                }

                // Report diagnostic for violating named types.
                foreach (INamedTypeSymbol secureType in _secureTypes)
                {
                    foreach (INamedTypeSymbol unsecureInterface in _interfacesWithUnsecureMethods)
                    {
                        if (secureType.AllInterfaces.Contains(unsecureInterface))
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    Rule,
                                    secureType.Locations[0],
                                    secureType.Name,
                                    SecureTypeInterfaceName,
                                    unsecureInterface.Name));
                            break;
                        }
                    }
                }
            }
        }
    }
}
