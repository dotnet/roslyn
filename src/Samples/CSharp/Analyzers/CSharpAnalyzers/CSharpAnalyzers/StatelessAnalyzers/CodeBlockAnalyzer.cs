// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CSharpAnalyzers
{
    /// <summary>
    /// Analyzer for reporting code block diagnostics.
    /// It reports diagnostics for all redundant methods which have an empty method body and are not virtual/override.
    /// </summary>
    /// <remarks>
    /// For analyzers that requires analyzing symbols or syntax nodes across a code block, see <see cref="CodeBlockStartedAnalyzer"/>.
    /// </remarks>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CodeBlockAnalyzer : DiagnosticAnalyzer
    {
        #region Descriptor fields

        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.CodeBlockAnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.CodeBlockAnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.CodeBlockAnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIds.CodeBlockAnalyzerRuleId, Title, MessageFormat, DiagnosticCategories.Stateless, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        #endregion

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCodeBlockAction(CodeBlockAction);
        }

        private static void CodeBlockAction(CodeBlockAnalysisContext codeBlockContext)
        {
            // We only care about method bodies.
            if (codeBlockContext.OwningSymbol.Kind != SymbolKind.Method)
            {
                return;
            }

            // Report diagnostic for void non-virtual methods with empty method bodies.
            var method = (IMethodSymbol)codeBlockContext.OwningSymbol;
            var block = (BlockSyntax)codeBlockContext.CodeBlock.ChildNodes().FirstOrDefault(n => n.Kind() == SyntaxKind.Block);
            if (method.ReturnsVoid && !method.IsVirtual && block != null && block.Statements.Count == 0)
            {
                var tree = block.SyntaxTree;
                var location = method.Locations.First(l => tree.Equals(l.SourceTree));
                var diagnostic = Diagnostic.Create(Rule, location, method.Name);
                codeBlockContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
