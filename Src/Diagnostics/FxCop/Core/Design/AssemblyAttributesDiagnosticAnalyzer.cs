// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class AssemblyAttributesDiagnosticAnalyzer : ICompilationAnalyzer
    {
        internal const string CA1016RuleName = "CA1016";
        internal const string CA1014RuleName = "CA1014";

        internal static DiagnosticDescriptor CA1016Rule = new DiagnosticDescriptor(CA1016RuleName,
                                                                         FxCopRulesResources.AssembliesShouldBeMarkedWithAssemblyVersionAttribute,
                                                                         FxCopRulesResources.AssembliesShouldBeMarkedWithAssemblyVersionAttribute,
                                                                         FxCopDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        internal static DiagnosticDescriptor CA1014Rule = new DiagnosticDescriptor(CA1014RuleName,
                                                                         FxCopRulesResources.MarkAssembliesWithCLSCompliantAttribute,
                                                                         FxCopRulesResources.MarkAssembliesWithCLSCompliantAttribute,
                                                                         FxCopDiagnosticCategory.Design,
                                                                         DiagnosticSeverity.Warning,
                                                                         isEnabledByDefault: true,
                                                                         customTags: DiagnosticCustomTags.Microsoft);

        private static readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics = ImmutableArray.Create(CA1016Rule, CA1014Rule);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return supportedDiagnostics;
            }
        }

        public void AnalyzeCompilation(Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var assemblyVersionAttributeSymbol = WellKnownTypes.AssemblyVersionAttribute(compilation);
            var assemblyComplianceAttributeSymbol = WellKnownTypes.CLSCompliantAttribute(compilation);

            if (assemblyVersionAttributeSymbol == null && assemblyComplianceAttributeSymbol == null)
            {
                return;
            }

            bool assemblyVersionAttributeFound = false;
            bool assemblyComplianceAttributeFound = false;

            // Check all assembly level attributes for the target attribute
            foreach (var attribute in compilation.Assembly.GetAttributes())
            {
                if (attribute.AttributeClass.Equals(assemblyVersionAttributeSymbol))
                {
                    // Mark the version attribute as found
                    assemblyVersionAttributeFound = true;
                }
                else if (attribute.AttributeClass.Equals(assemblyComplianceAttributeSymbol))
                {
                    // Mark the compliance attribute as found
                    assemblyComplianceAttributeFound = true;
                }
            }

            // Check for the case where we do not have the target attribute defined at all in our metadata references. If so, how can they reference it
            if (assemblyVersionAttributeSymbol == null)
            {
                assemblyVersionAttributeFound = false;
            }

            if (assemblyComplianceAttributeSymbol == null)
            {
                assemblyComplianceAttributeFound = false;
            }

            // If there's at least one diagnostic to report, let's report them
            if (!assemblyComplianceAttributeFound || !assemblyVersionAttributeFound)
            {
                if (!assemblyVersionAttributeFound)
                {
                    addDiagnostic(Diagnostic.Create(CA1016Rule, Location.None));
                }

                if (!assemblyComplianceAttributeFound)
                {
                    addDiagnostic(Diagnostic.Create(CA1014Rule, Location.None));
                }
            }
        }
    }
}
