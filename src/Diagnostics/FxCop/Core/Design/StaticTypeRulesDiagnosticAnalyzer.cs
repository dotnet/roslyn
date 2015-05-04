// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1025: Static holder types should be sealed
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class StaticTypeRulesDiagnosticAnalyzer : AbstractNamedTypeAnalyzer
    {
        internal const string RuleNameForExportAttribute = "StaticHolderTypeRules";
        internal const string CA1052RuleId = "CA1052";
        internal const string CA1053RuleId = "CA1053";

        private static readonly LocalizableString s_localizableTitleCA1052 = new LocalizableResourceString(nameof(FxCopRulesResources.StaticHolderTypesShouldBeStaticOrNotInheritable), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static readonly LocalizableString s_localizableMessageCA1052 = new LocalizableResourceString(nameof(FxCopRulesResources.StaticHolderTypeIsNotStatic), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        internal static readonly DiagnosticDescriptor CA1052Rule = new DiagnosticDescriptor(CA1052RuleId,
                                                                          s_localizableTitleCA1052,
                                                                          s_localizableMessageCA1052,
                                                                          FxCopDiagnosticCategory.Usage,
                                                                          DiagnosticSeverity.Warning,
                                                                          isEnabledByDefault: false,
                                                                          helpLinkUri: "http://msdn.microsoft.com/library/ms182168.aspx",
                                                                          customTags: DiagnosticCustomTags.Microsoft);

        private static readonly LocalizableString s_localizableTitleCA1053 = new LocalizableResourceString(nameof(FxCopRulesResources.StaticHolderTypesShouldNotHaveConstructors), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static readonly LocalizableString s_localizableMessageCA1053 = new LocalizableResourceString(nameof(FxCopRulesResources.StaticHolderTypesShouldNotHaveConstructorsMessage), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        internal static readonly DiagnosticDescriptor CA1053Rule = new DiagnosticDescriptor(CA1053RuleId,
                                                                          s_localizableTitleCA1053,
                                                                          s_localizableMessageCA1053,
                                                                          FxCopDiagnosticCategory.Usage,
                                                                          DiagnosticSeverity.Warning,
                                                                          isEnabledByDefault: false,
                                                                          helpLinkUri: "http://msdn.microsoft.com/library/ms182169.aspx",
                                                                          customTags: DiagnosticCustomTags.Microsoft);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(CA1052Rule, CA1053Rule);
            }
        }

        protected override void AnalyzeSymbol(INamedTypeSymbol symbol, Compilation compilation, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            // TODO: should this be restricted to class types?

            // static holder types are not already static/sealed and must be public or protected
            if (!symbol.IsStatic && !symbol.IsSealed
                && (symbol.DeclaredAccessibility == Accessibility.Public || symbol.DeclaredAccessibility == Accessibility.Protected))
            {
                // only get the explicitly declared members
                var allMembers = symbol.GetMembers().Where(member => !member.IsImplicitlyDeclared);
                if (!allMembers.Any())
                {
                    return;
                }

                // to be a static holder type, all members must be static and not operator overloads
                if (allMembers.All(member => (member.IsStatic || symbol.InstanceConstructors.Contains(member)) && !IsUserdefinedOperator(member)))
                {
                    // Has a default constructor that is implicitly defined
                    if (!symbol.InstanceConstructors.IsEmpty)
                    {
                        if (symbol.InstanceConstructors.Count() == 1 &&
                        symbol.InstanceConstructors.First().Parameters.IsEmpty)
                        {
                            // If there is just the default constructor,  we can make the type static.
                            // Produce Diagnostic CA1052
                            addDiagnostic(symbol.CreateDiagnostic(CA1052Rule, symbol.Name));
                        }
                        else if (symbol.InstanceConstructors.Count() > 0)
                        {
                            // If there are explicitly defined constructors then we cannot make the type static instead just show a diagnostic.
                            // Instead we show a Diagnostic CA1053 with no fix
                            addDiagnostic(symbol.CreateDiagnostic(CA1053Rule, symbol.Name));
                        }
                    }
                }
            }
        }

        private static bool IsUserdefinedOperator(ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.UserDefinedOperator;
        }
    }
}
