// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sample.Analyzers
{
    /// <summary>
    /// Analyzer for reporting syntax node diagnostics.
    /// It reports diagnostics for implicitly typed local variables, recommending explicit type specification.
    /// </summary>
    /// <remarks>
    /// For analyzers that requires analyzing symbols or syntax nodes across compilation, see <see cref="CompilationStartedAnalyzer"/> and <see cref="CompilationStartedAnalyzerWithCompilationWideAnalysis"/>.
    /// For analyzers that requires analyzing symbols or syntax nodes across a code block, see <see cref="CodeBlockStartedAnalyzer"/>.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SyntaxNodeAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Declare explicit type for local declarations.";
        public const string MessageFormat = "Local '{0}' is implicitly typed. Consider specifying its type explicitly in the declaration.";
        private const string Description = "Declare explicit type for local declarations.";

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.SyntaxNodeAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.Stateless,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.VariableDeclaration);
        }

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            // Find implicitly typed variable declarations.
            VariableDeclarationSyntax declaration = (VariableDeclarationSyntax)context.Node;
            if (declaration.Type.IsVar)
            {
                foreach (VariableDeclaratorSyntax variable in declaration.Variables)
                {
                    // For all such locals, report a diagnostic.
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Rule,
                            variable.GetLocation(),
                            variable.Identifier.ValueText));
                }
            }
        }
    }
}
