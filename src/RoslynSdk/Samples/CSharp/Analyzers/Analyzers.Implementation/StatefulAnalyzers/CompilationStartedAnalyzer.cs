// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sample.Analyzers
{
    /// <summary>
    /// Analyzer to demonstrate analysis within a compilation defining certain well-known symbol(s).
    /// It computes and reports diagnostics for all public implementations of an interface, which is only supposed to be implemented internally.
    /// <para>
    /// The analyzer registers:
    /// (a) A compilation start action, which initializes per-compilation immutable state. We fetch and store the type symbol for the interface type in the compilation.
    /// (b) A compilation symbol action, which identifies all named types implementing this interface, and reports diagnostics for all but internal allowed well known types.
    /// </para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CompilationStartedAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Do not implement unsupported interface";
        public const string MessageFormat = "Type '{0}' implements interface '{1}', which is only meant for internal implementation and might change in future. You should avoid implementing this interface.";
        private const string Description = "Do not implement unsupported interface.";

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.CompilationStartedAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.Stateful,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: Description);

        public const string DontInheritInterfaceTypeName = "MyInterfaces.Interface";
        public const string AllowedInternalImplementationTypeName = "MyInterfaces.MyInterfaceImpl";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(compilationContext =>
            {
                // We only care about compilations where interface type "DontInheritInterfaceTypeName" is available.
                INamedTypeSymbol interfaceType = compilationContext.Compilation.GetTypeByMetadataName(DontInheritInterfaceTypeName);
                if (interfaceType == null)
                {
                    return;
                }

                // Register an action that accesses the immutable state and reports diagnostics.
                compilationContext.RegisterSymbolAction(
                    symbolContext => { AnalyzeSymbol(symbolContext, interfaceType); },
                    SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol interfaceType)
        {
            // Check if the symbol implements the interface type
            INamedTypeSymbol namedType = (INamedTypeSymbol)context.Symbol;
            if (namedType.Interfaces.Contains(interfaceType) &&
                !namedType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat).Equals(AllowedInternalImplementationTypeName))
            {
                Diagnostic diagnostic = Diagnostic.Create(Rule, namedType.Locations[0], namedType.Name, DontInheritInterfaceTypeName);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
