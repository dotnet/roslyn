// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpAnalyzers
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
        #region Descriptor fields

        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.SyntaxNodeAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.SyntaxNodeAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.SyntaxNodeAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.SyntaxNodeAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateless, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.VariableDeclaration);
        }

        private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
        {
            // Find implicitly typed variable declarations.
            var declaration = (VariableDeclarationSyntax)context.Node;
            if (declaration.Type.IsVar)
            {
                foreach (var variable in declaration.Variables)
                {
                    // For all such locals, report a diagnostic.
                    var diagnostic = Diagnostic.Create(Rule, variable.GetLocation(), variable.Identifier.ValueText);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
