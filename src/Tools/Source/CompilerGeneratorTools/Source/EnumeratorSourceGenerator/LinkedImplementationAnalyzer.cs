// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EnumeratorSourceGenerator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal sealed class LinkedImplementationAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor s_diagnostic = new(EnumDiagnosticIds.MissingImplementation, "Linked implementation expected", "Linked member '{0}' should be implemented on this type", "Correctness", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(s_diagnostic);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(
                static context =>
                {
                    var csSymbolType = context.Compilation.GetTypesByMetadataName("Microsoft.CodeAnalysis.CSharp.Symbol").SingleOrDefault();
                    if (csSymbolType is not null)
                        Initialize(context, csSymbolType);

                    var vbSymbolType = context.Compilation.GetTypesByMetadataName("Microsoft.CodeAnalysis.VisualBasic.Symbol").SingleOrDefault();
                    if (vbSymbolType is not null)
                        Initialize(context, vbSymbolType);
                });
        }

        private static void Initialize(CompilationStartAnalysisContext context, INamedTypeSymbol symbolType)
        {
            context.RegisterSymbolAction(
                context =>
                {
                    var symbol = (INamedTypeSymbol)context.Symbol;
                    if (symbol.TypeKind is not TypeKind.Class)
                        return;

                    if (!IsDerivedFrom(symbol, symbolType))
                        return;

                    var locations = new PropertyMatcher("Locations");
                    var locationsCount = new PropertyMatcher("LocationsCount");
                    var getCurrentLocation = new MethodMatcher("GetCurrentLocation");
                    var moveNextLocation = new MethodMatcher("MoveNextLocation");
                    var moveNextLocationReversed = new MethodMatcher("MoveNextLocationReversed");
                    foreach (var member in symbol.GetMembers())
                    {
                        if (member.Kind == SymbolKind.Property)
                        {
                            locations.Visit(member);
                            locationsCount.Visit(member);
                        }
                        else if (member.Kind == SymbolKind.Method)
                        {
                            getCurrentLocation.Visit(member);
                            moveNextLocation.Visit(member);
                            moveNextLocationReversed.Visit(member);
                        }
                    }

                    var foundCount = (locations.Found ? 1 : 0)
                        + (locationsCount.Found ? 1 : 0)
                        + (getCurrentLocation.Found ? 1 : 0)
                        + (moveNextLocation.Found ? 1 : 0)
                        + (moveNextLocationReversed.Found ? 1 : 0);

                    if (foundCount > 0 && foundCount < 5)
                    {
                        // At least one member was implemented, but not all were. Report a diagnostic for the omitted members.
                        var diagnosticSymbol = locations.Symbol ?? locationsCount.Symbol ?? getCurrentLocation.Symbol ?? moveNextLocation.Symbol ?? moveNextLocationReversed.Symbol!;
                        ReportIfMissing(in context, locations, diagnosticSymbol);
                        ReportIfMissing(in context, locationsCount, diagnosticSymbol);
                        ReportIfMissing(in context, getCurrentLocation, diagnosticSymbol);
                        ReportIfMissing(in context, moveNextLocation, diagnosticSymbol);
                        ReportIfMissing(in context, moveNextLocationReversed, diagnosticSymbol);
                    }
                },
                SymbolKind.NamedType);
        }

        private static bool IsDerivedFrom(INamedTypeSymbol derivedSymbol, INamedTypeSymbol baseSymbol)
        {
            for (var current = derivedSymbol.BaseType; current is not null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseSymbol))
                    return true;
            }

            return false;
        }

        private static void ReportIfMissing(in SymbolAnalysisContext context, SymbolMatcher expectedSymbol, ISymbol diagnosticSymbol)
        {
            if (expectedSymbol.Found)
                return;

            context.ReportDiagnostic(Diagnostic.Create(s_diagnostic, diagnosticSymbol.Locations.First(), expectedSymbol.Name));
        }

        private abstract class SymbolMatcher
        {
            protected SymbolMatcher(string name)
            {
                Name = name;
            }

            public bool Found => Symbol is not null;
            public string Name { get; }
            public ISymbol? Symbol { get; private set; }

            public bool Visit(ISymbol symbol)
            {
                if (Symbol is not null)
                    return true;

                if (VisitCore(symbol))
                {
                    Symbol = symbol;
                    return true;
                }

                return false;
            }

            protected abstract bool VisitCore(ISymbol symbol);
        }

        private sealed class PropertyMatcher : SymbolMatcher
        {
            public PropertyMatcher(string propertyName)
                : base(propertyName)
            {
            }

            protected override bool VisitCore(ISymbol symbol)
            {
                if (symbol is { Kind: SymbolKind.Property, IsOverride: true }
                    && symbol.Name == Name)
                {
                    return true;
                }

                return false;
            }
        }

        private sealed class MethodMatcher : SymbolMatcher
        {
            public MethodMatcher(string methodName)
                : base(methodName)
            {
            }

            protected override bool VisitCore(ISymbol symbol)
            {
                if (symbol is { Kind: SymbolKind.Method, IsOverride: true }
                    && symbol.Name == Name)
                {
                    return true;
                }

                return false;
            }
        }
    }
}
