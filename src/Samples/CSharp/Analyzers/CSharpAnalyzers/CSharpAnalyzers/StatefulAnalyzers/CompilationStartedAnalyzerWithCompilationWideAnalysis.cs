// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpAnalyzers
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
        #region Descriptor fields

        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.CompilationStartedAnalyzerWithCompilationWideAnalysisTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.CompilationStartedAnalyzerWithCompilationWideAnalysisMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.CompilationStartedAnalyzerWithCompilationWideAnalysisDescription), Resources.ResourceManager, typeof(Resources));
        
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.CompilationStartedAnalyzerWithCompilationWideAnalysisRuleId, Title, MessageFormat, DiagnosticCategories.Stateful, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        #endregion

        public const string UnsecureMethodAttributeName = "MyNamespace.UnsecureMethodAttribute";
        public const string SecureTypeInterfaceName = "MyNamespace.ISecureType";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // Check if the attribute type marking unsecure methods is defined.
                var unsecureMethodAttributeType = compilationContext.Compilation.GetTypeByMetadataName(UnsecureMethodAttributeName);
                if (unsecureMethodAttributeType == null)
                {
                    return;
                }

                // Check if the interface type marking secure types is defined.
                var secureTypeInterfaceType = compilationContext.Compilation.GetTypeByMetadataName(SecureTypeInterfaceName);
                if (secureTypeInterfaceType == null)
                {
                    return;
                }

                // Initialize state in the start action.
                var analyzer = new CompilationAnalyzer(unsecureMethodAttributeType, secureTypeInterfaceType);

                // Register an intermediate non-end action that accesses and modifies the state.
                compilationContext.RegisterSymbolAction(analyzer.AnalyzeSymbol, SymbolKind.NamedType, SymbolKind.Method);

                // Register an end action to report diagnostics based on the final state.
                compilationContext.RegisterCompilationEndAction(analyzer.CompilationEndAction);
            });
        }

        private class CompilationAnalyzer
        {
            #region Per-Compilation immutable state

            private readonly INamedTypeSymbol _unsecureMethodAttributeType;
            private readonly INamedTypeSymbol _secureTypeInterfaceType;

            #endregion

            #region Per-Compilation mutable state

            /// <summary>
            /// List of secure types in the compilation implementing interface <see cref="SecureTypeInterfaceName"/>.
            /// </summary>
            private List<INamedTypeSymbol> _secureTypes;

            /// <summary>
            /// Set of unsecure interface types in the compilation that have methods with an attribute of <see cref="_unsecureMethodAttributeType"/>.
            /// </summary>
            private HashSet<INamedTypeSymbol> _interfacesWithUnsecureMethods;
            
            #endregion

            #region State intialization

            public CompilationAnalyzer(INamedTypeSymbol unsecureMethodAttributeType, INamedTypeSymbol secureTypeInterfaceType)
            {
                _unsecureMethodAttributeType = unsecureMethodAttributeType;
                _secureTypeInterfaceType = secureTypeInterfaceType;

                _secureTypes = null;
                _interfacesWithUnsecureMethods = null;
            }

            #endregion

            #region Intermediate actions

            public void AnalyzeSymbol(SymbolAnalysisContext context)
            {
                switch (context.Symbol.Kind)
                {
                    case SymbolKind.NamedType:
                        // Check if the symbol implements "_secureTypeInterfaceType".
                        var namedType = (INamedTypeSymbol)context.Symbol;
                        if (namedType.AllInterfaces.Contains(_secureTypeInterfaceType))
                        {
                            _secureTypes = _secureTypes ?? new List<INamedTypeSymbol>();
                            _secureTypes.Add(namedType);
                        }

                        break;

                    case SymbolKind.Method:
                        // Check if this is an interface method with "_unsecureMethodAttributeType" attribute.
                        var method = (IMethodSymbol)context.Symbol;
                        if (method.ContainingType.TypeKind == TypeKind.Interface &&
                            method.GetAttributes().Any(a => a.AttributeClass.Equals(_unsecureMethodAttributeType)))
                        {
                            _interfacesWithUnsecureMethods = _interfacesWithUnsecureMethods ?? new HashSet<INamedTypeSymbol>();
                            _interfacesWithUnsecureMethods.Add(method.ContainingType);
                        }

                        break;
                }
            }

            #endregion

            #region End action

            public void CompilationEndAction(CompilationAnalysisContext context)
            {
                if (_interfacesWithUnsecureMethods == null || _secureTypes == null)
                {
                    // No violating types.
                    return;
                }

                // Report diagnostic for violating named types.
                foreach (var secureType in _secureTypes)
                {
                    foreach (var unsecureInterface in _interfacesWithUnsecureMethods)
                    {
                        if (secureType.AllInterfaces.Contains(unsecureInterface))
                        {
                            var diagnostic = Diagnostic.Create(Rule, secureType.Locations[0], secureType.Name, SecureTypeInterfaceName, unsecureInterface.Name);
                            context.ReportDiagnostic(diagnostic);

                            break;
                        }
                    }
                }
            }

            #endregion
        }
    }
}
