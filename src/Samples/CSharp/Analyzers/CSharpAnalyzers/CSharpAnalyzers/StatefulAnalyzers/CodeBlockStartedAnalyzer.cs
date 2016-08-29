// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpAnalyzers
{
    /// <summary>
    /// Analyzer to demonstrate code block wide analysis.
    /// It computes and reports diagnostics for unused parameters in methods.
    /// It performs code block wide analysis to detect such unused parameters and reports diagnostics for them in the code block end action.
    /// <para>
    /// The analyzer registers:
    /// (a) A code block start action, which initializes per-code block mutable state. We mark all parameters as unused at start of analysis.
    /// (b) A code block syntax node action, which identifes parameter references and marks the corresponding parameter as used.
    /// (c) A code block end action, which reports diagnostics based on the final state, for all parameters which are unused.
    /// </para>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CodeBlockStartedAnalyzer : DiagnosticAnalyzer
    {
        #region Descriptor fields

        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.CodeBlockStartedAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.CodeBlockStartedAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.CodeBlockStartedAnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.CodeBlockStartedAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateful, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCodeBlockStartAction<SyntaxKind>(startCodeBlockContext =>
            {
                // We only care about method bodies.
                if (startCodeBlockContext.OwningSymbol.Kind != SymbolKind.Method)
                {
                    return;
                }

                // We only care about methods with parameters.
                var method = (IMethodSymbol)startCodeBlockContext.OwningSymbol;
                if (method.Parameters.IsEmpty)
                {
                    return;
                }

                // Initialize local mutable state in the start action.
                var analyzer = new UnusedParametersAnalyzer(method);

                // Register an intermediate non-end action that accesses and modifies the state.
                startCodeBlockContext.RegisterSyntaxNodeAction(analyzer.AnalyzeSyntaxNode, SyntaxKind.IdentifierName);

                // Register an end action to report diagnostics based on the final state.
                startCodeBlockContext.RegisterCodeBlockEndAction(analyzer.CodeBlockEndAction);
            });
        }

        private class UnusedParametersAnalyzer
        {
            #region Per-CodeBlock mutable state

            private readonly HashSet<IParameterSymbol> _unusedParameters;
            private readonly HashSet<string> _unusedParameterNames;

            #endregion

            #region State intialization

            public UnusedParametersAnalyzer(IMethodSymbol method)
            {
                // Initialization: Assume all parameters are unused.
                var parameters = method.Parameters.Where(p => !p.IsImplicitlyDeclared && p.Locations.Length > 0);
                _unusedParameters = new HashSet<IParameterSymbol>(parameters);
                _unusedParameterNames = new HashSet<string>(parameters.Select(p => p.Name));
            }

            #endregion

            #region Intermediate actions

            public void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
            {
                // Check if we have any pending unreferenced parameters.
                if (_unusedParameters.Count == 0)
                {
                    return;
                }

                // Syntactic check to avoid invoking GetSymbolInfo for every identifier.
                var identifier = (IdentifierNameSyntax)context.Node;
                if (!_unusedParameterNames.Contains(identifier.Identifier.ValueText))
                {
                    return;
                }

                // Mark parameter as used.
                var parmeter = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol as IParameterSymbol;
                if (parmeter != null && _unusedParameters.Contains(parmeter))
                {
                    _unusedParameters.Remove(parmeter);
                    _unusedParameterNames.Remove(parmeter.Name);
                }
            }

            #endregion

            #region End action

            public void CodeBlockEndAction(CodeBlockAnalysisContext context)
            {
                // Report diagnostics for unused parameters.
                foreach (var parameter in _unusedParameters)
                {
                    var diagnostic = Diagnostic.Create(Rule, parameter.Locations[0], parameter.Name, parameter.ContainingSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }

            #endregion
        }
    }
}
