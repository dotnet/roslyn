// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.FxCopAnalyzers.Design
{
    /// <summary>
    /// CA1033: Interface methods should be callable by child types
    /// <para>
    /// Consider a base type that explicitly implements a public interface method.
    /// A type that derives from the base type can access the inherited interface method only through a reference to the current instance ('this' in C#) that is cast to the interface.
    /// If the derived type re-implements (explicitly) the inherited interface method, the base implementation can no longer be accessed.
    /// The call through the current instance reference will invoke the derived implementation; this causes recursion and an eventual stack overflow.
    /// </para>
    /// </summary>
    /// <remarks>
    /// This rule does not report a violation for an explicit implementation of IDisposable.Dispose when an externally visible Close() or System.IDisposable.Dispose(Boolean) method is provided.
    /// </remarks>
    public abstract class InterfaceMethodsShouldBeCallableByChildTypesAnalyzer<TInvocationExpressionSyntax> : DiagnosticAnalyzer
        where TInvocationExpressionSyntax: SyntaxNode
    {
        private const string RuleId = "CA1033";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(FxCopRulesResources.InterfaceMethodsShouldBeCallableByChildTypesTitle), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(FxCopRulesResources.InterfaceMethodsShouldBeCallableByChildTypesMessage), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(FxCopRulesResources.InterfaceMethodsShouldBeCallableByChildTypesDescription), FxCopRulesResources.ResourceManager, typeof(FxCopRulesResources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(RuleId,
                                                                          s_localizableTitle,
                                                                          s_localizableMessage,
                                                                          FxCopDiagnosticCategory.Design,
                                                                          DiagnosticSeverity.Warning,
                                                                          isEnabledByDefault: false,
                                                                          description: s_localizableDescription,
                                                                          helpLinkUri: "https://msdn.microsoft.com/library/ms182153.aspx",
                                                                          customTags: DiagnosticCustomTags.Microsoft);

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public sealed override void Initialize(AnalysisContext analysisContext) =>
            analysisContext.RegisterCodeBlockAction(AnalyzeCodeBlock);

        protected abstract bool ShouldExcludeCodeBlock(SyntaxNode codeBlock);

        private void AnalyzeCodeBlock(CodeBlockAnalysisContext context)
        {
            if (context.OwningSymbol.Kind != SymbolKind.Method)
            {
                return;
            }

            var method = (IMethodSymbol)context.OwningSymbol;

            // We are only intereseted in private explicit interface implementations within a public non-sealed type.
            if (method.ExplicitInterfaceImplementations.Length == 0 ||
                method.GetResultantVisibility() != SymbolVisibility.Private ||
                method.ContainingType.IsSealed ||
                method.ContainingType.GetResultantVisibility() != SymbolVisibility.Public)
            {
                return;
            }

            // Avoid false reports from simple explicit implementations where the deriving type is not expected to access the base implementation.
            if (ShouldExcludeCodeBlock(context.CodeBlock))
            {
                return;
            }

            var hasPublicInterfaceImplementation = false;
            foreach (var interfaceMethod in method.ExplicitInterfaceImplementations)
            {
                // If any one of the explicitly implemented interface methods has a visible alternate, then effectively, they all do.
                if (HasVisibleAlternate(method.ContainingType, interfaceMethod, context.SemanticModel.Compilation))
                {
                    return;
                }

                hasPublicInterfaceImplementation = hasPublicInterfaceImplementation ||
                    interfaceMethod.ContainingType.GetResultantVisibility() == SymbolVisibility.Public;
            }

            // Even if none of the interface methods have alternates, there's only an issue if at least one of the interfaces is public.
            if (hasPublicInterfaceImplementation)
            {
                ReportDiagnostic(context, method.ContainingType.Name, method.Name);
            }
        }

        private static bool HasVisibleAlternate(INamedTypeSymbol namedType, IMethodSymbol interfaceMethod, Compilation compilation)
        {
            foreach (var type in namedType.GetBaseTypesAndThis())
            {
                foreach (var method in type.GetMembers(interfaceMethod.Name).OfType<IMethodSymbol>())
                {
                    if (method.GetResultantVisibility() == SymbolVisibility.Public)
                    {
                        return true;
                    }
                }
            }

            // This rule does not report a violation for an explicit implementation of IDisposable.Dispose when an externally visible Close() or System.IDisposable.Dispose(Boolean) method is provided.
            return interfaceMethod.Name.Equals("Dispose") &&
                interfaceMethod.ContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat).Equals("System.IDisposable") &&
                namedType.GetBaseTypesAndThis().Any(t => 
                    t.GetMembers("Close").OfType<IMethodSymbol>().Any(m => 
                        m.GetResultantVisibility() == SymbolVisibility.Public)) ;
        }

        private static void ReportDiagnostic(CodeBlockAnalysisContext context, params object[] messageArgs)
        {
            var diagnostic = Diagnostic.Create(Rule, context.OwningSymbol.Locations[0], messageArgs);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
