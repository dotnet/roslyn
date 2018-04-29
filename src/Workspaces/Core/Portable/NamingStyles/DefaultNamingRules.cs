using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.NamingStyles;

namespace Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles
{
    internal static class DefaultNamingRules
    {
        public static ImmutableArray<NamingRule> FieledAndPropertyRules { get; } = ImmutableArray.Create(
               new NamingRule(new SymbolSpecification(
                   Guid.NewGuid(), "Property",
                   ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Property))),
                   new NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.PascalCase),
                   enforcementLevel: DiagnosticSeverity.Hidden),
               new NamingRule(new SymbolSpecification(
                   Guid.NewGuid(), "Field",
                   ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field))),
                   new NamingStyle(Guid.NewGuid(), capitalizationScheme: Capitalization.CamelCase),
                   enforcementLevel: DiagnosticSeverity.Hidden),
               new NamingRule(new SymbolSpecification(
                   Guid.NewGuid(), "FieldWithUnderscore",
                   ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field)),
                   ImmutableArray.Create(Accessibility.Private)),
                   new NamingStyle(Guid.NewGuid(), prefix: "_", capitalizationScheme: Capitalization.CamelCase),
                   enforcementLevel: DiagnosticSeverity.Hidden));

        public static ImmutableArray<NamingRule> InterfaceNameRule { get; } = ImmutableArray.Create(
            new NamingRule(new SymbolSpecification(
                   Guid.NewGuid(), "Interface",
                   ImmutableArray.Create(new SymbolSpecification.SymbolKindOrTypeKind(TypeKind.Interface))),
                   new NamingStyle(Guid.NewGuid(), prefix: "I", capitalizationScheme: Capitalization.PascalCase),
                   enforcementLevel: DiagnosticSeverity.Hidden));
    }
}
