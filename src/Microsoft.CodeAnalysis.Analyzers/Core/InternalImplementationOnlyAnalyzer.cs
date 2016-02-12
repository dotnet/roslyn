// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class InternalImplementationOnlyAnalyzer : DiagnosticAnalyzer
    {
        private const string InternalImplementationOnlyAttributeName = "InternalImplementationOnlyAttribute";
        private const string InternalImplementationOnlyAttributeFullName = "System.Runtime.CompilerServices.InternalImplementationOnlyAttribute";
        private readonly static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InternalImplementationOnlyTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private readonly static LocalizableString s_localizableMessageFormat = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InternalImplementationOnlyMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private readonly static LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.InternalImplementationOnlyDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
                                                        DiagnosticIds.InternalImplementationOnlyRuleId,
                                                        s_localizableTitle,
                                                        s_localizableMessageFormat,
                                                        AnalyzerDiagnosticCategory.Compatibility,
                                                        DiagnosticSeverity.Error,
                                                        true,
                                                        s_localizableDescription);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);

        private static void AnalyzeSymbol(SymbolAnalysisContext context)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            // If any interface implemented by this type has the attribute and if the interface and this type are not
            // in "internals visible" context, then issue an error.
            foreach (INamedTypeSymbol iface in namedTypeSymbol.AllInterfaces)
            {
                System.Collections.Generic.IEnumerable<AttributeData> attributes = iface.GetApplicableAttributes();

                // We are doing a string comparison of the name here because we don't care where the attribute comes from.
                // CodeAnalysis.dll itself has this attribute and if the user assembly also had it, symbol equality will fail
                // but we should still issue the error.
                if (attributes.Any(a => a.AttributeClass.Name.Equals(InternalImplementationOnlyAttributeName)
                                        && a.AttributeClass.ToDisplayString().Equals(InternalImplementationOnlyAttributeFullName)))
                {
                    if (!iface.ContainingAssembly.GivesAccessTo(namedTypeSymbol.ContainingAssembly))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, namedTypeSymbol.Locations.First(), namedTypeSymbol.Name, iface.Name));
                        break;
                    }
                }
            }
        }
    }
}
