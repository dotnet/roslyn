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
    public sealed class CA1017DiagnosticAnalyzer : ICompilationAnalyzer
    {
        internal const string RuleId = "CA1017";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                                      FxCopRulesResources.MarkAllAssembliesWithComVisible,
                                                                                      "{0}",
                                                                                      FxCopDiagnosticCategory.Design,
                                                                                      DiagnosticSeverity.Warning,
                                                                                      isEnabledByDefault: true,
                                                                                      description: FxCopRulesResources.MarkAllAssembliesWithComVisibleDescription,
                                                                                      helpLink: "http://msdn.microsoft.com/library/ms182157.aspx",
                                                                                      customTags: DiagnosticCustomTags.Microsoft);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public void AnalyzeCompilation(Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            if (AssemblyHasPublicTypes(compilation.Assembly))
            {
                var comVisibleAttributeSymbol = WellKnownTypes.ComVisibleAttribute(compilation);
                if (comVisibleAttributeSymbol == null)
                {
                    return;
                }

                var attributeInstance = compilation.Assembly.GetAttributes().FirstOrDefault(a => a.AttributeClass.Equals(comVisibleAttributeSymbol));

                if (attributeInstance != null)
                {
                    if (attributeInstance.ConstructorArguments.Length > 0 &&
                        attributeInstance.ConstructorArguments[0].Kind == TypedConstantKind.Primitive &&
                        attributeInstance.ConstructorArguments[0].Value != null &
                        attributeInstance.ConstructorArguments[0].Value.Equals(true))
                    {
                        // Has the attribute, with the value 'true'.
                        addDiagnostic(Diagnostic.Create(Rule, Location.None, string.Format(FxCopRulesResources.CA1017_AttributeTrue, compilation.Assembly.Name)));
                    }
                }
                else
                {
                    // No ComVisible attribute at all.
                    addDiagnostic(Diagnostic.Create(Rule, Location.None, string.Format(FxCopRulesResources.CA1017_NoAttribute, compilation.Assembly.Name)));
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
