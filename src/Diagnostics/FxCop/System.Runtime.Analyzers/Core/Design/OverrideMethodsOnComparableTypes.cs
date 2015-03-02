// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace System.Runtime.Analyzers
{
    /// <summary>
    /// CA1036: A public or protected type implements the System.IComparable interface and 
    /// does not override Object.Equals or does not overload the language-specific operator
    /// for equality, inequality, less than, or greater than. The rule does not report a
    /// violation if the type inherits only an implementation of the interface.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class OverrideMethodsOnComparableTypesAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1036";
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.OverloadOperatorEqualsOnIComparableInterface), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));
        private static LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.OverloadOperatorEqualsOnIComparableInterface), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));
        private static LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(SystemRuntimeAnalyzersResources.OverloadOperatorEqualsOnIComparableInterfaceDescription), SystemRuntimeAnalyzersResources.ResourceManager, typeof(SystemRuntimeAnalyzersResources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                                  s_localizableTitle,
                                                                                  s_localizableMessage,
                                                                                  DiagnosticCategory.Design,
                                                                                  DiagnosticSeverity.Warning,
                                                                                  isEnabledByDefault: true,
                                                                                  description: s_localizableDescription,
                                                                                  helpLinkUri: "http://msdn.microsoft.com/library/ms182163.aspx",
                                                                                  customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterCompilationStartAction(compilationContext =>
            {
                var comparableType = WellKnownTypes.IComparable(compilationContext.Compilation);
                var genericComparableType = WellKnownTypes.GenericIComparable(compilationContext.Compilation);

                // Even if one of them is available, we should continue analysis.
                if (comparableType == null && genericComparableType == null)
                {
                    return;
                }

                compilationContext.RegisterSymbolAction(context =>
                {
                    AnalyzeSymbol((INamedTypeSymbol)context.Symbol, comparableType, genericComparableType, context.ReportDiagnostic);
                },
                SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbol(INamedTypeSymbol namedTypeSymbol, INamedTypeSymbol comparableType, INamedTypeSymbol genericComparableType, Action<Diagnostic> addDiagnostic)
        {
            if (namedTypeSymbol.DeclaredAccessibility == Accessibility.Private || namedTypeSymbol.TypeKind == TypeKind.Interface)
            {
                return;
            }

            if (namedTypeSymbol.AllInterfaces.Any(t => t.Equals(comparableType) ||
                                                      (t.ConstructedFrom?.Equals(genericComparableType) ?? false)))
            {
                if (!(DoesOverrideEquals(namedTypeSymbol) && IsEqualityOperatorImplemented(namedTypeSymbol)))
                {
                    addDiagnostic(namedTypeSymbol.CreateDiagnostic(Rule));
                }
            }
        }

        internal static bool DoesOverrideEquals(INamedTypeSymbol symbol)
        {
            // Does the symbol override Object.Equals?
            return symbol.GetMembers(WellKnownMemberNames.ObjectEquals).OfType<IMethodSymbol>().Where(m => IsEqualsOverride(m)).Any();
        }

        private static bool IsEqualsOverride(IMethodSymbol method)
        {
            return method.IsOverride &&
                   method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                   method.Parameters.Length == 1 &&
                   method.Parameters[0].Type.SpecialType == SpecialType.System_Object;
        }

        internal static bool DoesOverrideGetHashCode(INamedTypeSymbol symbol)
        {
            // Does the symbol override Object.GetHashCode?
            return symbol.GetMembers(WellKnownMemberNames.ObjectGetHashCode).OfType<IMethodSymbol>().Where(m => IsGetHashCodeOverride(m)).Any();
        }

        private static bool IsGetHashCodeOverride(IMethodSymbol method)
        {
            return method.IsOverride &&
                   method.ReturnType.SpecialType == SpecialType.System_Int32 &&
                   method.Parameters.Length == 0;
        }

        private static bool IsEqualityOperatorImplemented(INamedTypeSymbol symbol)
        {
            // Does the symbol overload all of the equality operators?  (All are required per http://msdn.microsoft.com/en-us/library/ms182163.aspx example.)
            return IsOperatorImplemented(symbol, WellKnownMemberNames.EqualityOperatorName) &&
                    IsOperatorImplemented(symbol, WellKnownMemberNames.InequalityOperatorName) &&
                    IsOperatorImplemented(symbol, WellKnownMemberNames.LessThanOperatorName) &&
                    IsOperatorImplemented(symbol, WellKnownMemberNames.GreaterThanOperatorName);
        }

        internal static bool IsOperatorImplemented(INamedTypeSymbol symbol, string op)
        {
            // TODO: should this filter on the right-hand-side operator type?
            return symbol.GetMembers(op).OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.UserDefinedOperator).Any();
        }
    }
}
