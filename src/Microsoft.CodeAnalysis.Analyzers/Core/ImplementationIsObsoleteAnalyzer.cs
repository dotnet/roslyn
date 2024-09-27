// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers
{
    using static CodeAnalysisDiagnosticsResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class ImplementationIsObsoleteAnalyzer : DiagnosticAnalyzer
    {
        private const string ImplementationIsObsoleteAttributeName = "ImplementationIsObsoleteAttribute";
        private const string ImplementationIsObsoleteAttributeFullName = "System.Runtime.CompilerServices.ImplementationIsObsoleteAttribute";

        public static readonly DiagnosticDescriptor Rule = new(
            DiagnosticIds.ImplementationIsObsoleteRuleId,
            CreateLocalizableResourceString(nameof(ImplementationIsObsoleteTitle)),
            CreateLocalizableResourceString(nameof(ImplementationIsObsoleteMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCompatibility,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(ImplementationIsObsoleteDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // If any interface implemented by this type has the attribute and if the interface and this type are not
            // in "internals visible" context, then issue an error.
            foreach (INamedTypeSymbol iface in namedTypeSymbol.AllInterfaces)
            {
                System.Collections.Generic.IEnumerable<AttributeData> attributes = iface.GetAttributes();

                // We are doing a string comparison of the name here because we don't care where the attribute comes from.
                // CodeAnalysis.dll itself has this attribute and if the user assembly also had it, symbol equality will fail
                // but we should still issue the error.
                if (attributes.FirstOrDefault(a => a is { AttributeClass.Name: ImplementationIsObsoleteAttributeName, ConstructorArguments: [{ Value: string }] }
                                                   && a.AttributeClass.ToDisplayString().Equals(ImplementationIsObsoleteAttributeFullName, StringComparison.Ordinal)) is { } attr &&
                    !iface.ContainingAssembly.GivesAccessTo(namedTypeSymbol.ContainingAssembly))
                {
                    context.ReportDiagnostic(namedTypeSymbol.CreateDiagnostic(Rule, namedTypeSymbol.Name, iface.Name, (string)attr.ConstructorArguments[0].Value!));
                    break;
                }
            }
        }
    }
}
