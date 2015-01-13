// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    [DiagnosticAnalyzer]
    public sealed class CA1017DiagnosticAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1017";
        private static LocalizableString localizableTitle = new LocalizableResourceString(nameof(FxCopRulesResources.MarkAllAssembliesWithComVisible), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static LocalizableString localizableDescription = new LocalizableResourceString(nameof(FxCopRulesResources.MarkAllAssembliesWithComVisibleDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                                      localizableTitle,
                                                                                      "{0}",
                                                                                      FxCopDiagnosticCategory.Design,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: false,
                                                                                      description: localizableDescription,
                                                                                      helpLink: "http://msdn.microsoft.com/library/ms182157.aspx",
                                                                                      customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationEndAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationEndAnalysisContext context)
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
                        context.ReportDiagnostic(Diagnostic.Create(Rule, Location.None, string.Format(FxCopRulesResources.CA1017_AttributeTrue, context.Compilation.Assembly.Name)));
                    }
                }
                else
                {
                    // No ComVisible attribute at all.
                    context.ReportDiagnostic(Diagnostic.Create(Rule, Location.None, string.Format(FxCopRulesResources.CA1017_NoAttribute, context.Compilation.Assembly.Name)));
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
