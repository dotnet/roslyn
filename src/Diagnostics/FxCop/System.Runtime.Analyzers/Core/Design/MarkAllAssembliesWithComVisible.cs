// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class MarkAllAssembliesWithComVisibleAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1017";
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.MarkAllAssembliesWithComVisible), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));
        private static LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.MarkAllAssembliesWithComVisibleDescription), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                                      s_localizableTitle,
                                                                                      "{0}",
                                                                                      DiagnosticCategory.Design,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: false,
                                                                                      description: s_localizableDescription,
                                                                                      helpLinkUri: "http://msdn.microsoft.com/library/ms182157.aspx",
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
            analysisContext.RegisterCompilationAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            if (AssemblyHasPublicTypes(context.Compilation.Assembly))
            {
                var comVisibleAttributeSymbol = WellKnownTypes.ComVisibleAttribute(context.Compilation);
                if (comVisibleAttributeSymbol == null)
                {
                    return;
                }

                var attributeInstance = context.Compilation.Assembly.GetAttributes().FirstOrDefault(a => a.AttributeClass.Equals(comVisibleAttributeSymbol));

                if (attributeInstance != null)
                {
                    if (attributeInstance.ConstructorArguments.Length > 0 &&
                        attributeInstance.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                        attributeInstance.ConstructorArguments[0].Value != null &
                        attributeInstance.ConstructorArguments[0].Value.Equals(true))
                    {
                        // Has the attribute, with the value 'true'.
                        context.ReportDiagnostic(Diagnostic.Create(Rule, Location.None, string.Format(SystemRuntimeAnalyzersResources.CA1017_AttributeTrue, context.Compilation.Assembly.Name)));
                    }
                }
                else
                {
                    // No ComVisible attribute at all.
                    context.ReportDiagnostic(Diagnostic.Create(Rule, Location.None, string.Format(SystemRuntimeAnalyzersResources.CA1017_NoAttribute, context.Compilation.Assembly.Name)));
                }
            }

            return;
        }

        private static bool AssemblyHasPublicTypes(IAssemblySymbol assembly)
        {
            return assembly
                    .GlobalNamespace
                    .GetMembers()
                    .OfType<INamedTypeSymbol>()
                    .Where(s => s.DeclaredAccessibility == Accessibility.Public)
                    .Any();
        }
    }
}
