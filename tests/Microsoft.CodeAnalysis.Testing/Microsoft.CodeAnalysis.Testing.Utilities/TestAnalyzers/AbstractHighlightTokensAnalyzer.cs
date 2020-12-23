// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing.TestAnalyzers
{
    public abstract class AbstractHighlightTokensAnalyzer : DiagnosticAnalyzer
    {
        protected AbstractHighlightTokensAnalyzer(string id, params int[] tokenKinds)
        {
            Descriptor = new DiagnosticDescriptor(id, "title", "message", "category", DiagnosticSeverity.Warning, isEnabledByDefault: true);
            Tokens = ImmutableHashSet.CreateRange(tokenKinds);
        }

        public DiagnosticDescriptor Descriptor { get; }

        public ImmutableHashSet<int> Tokens { get; }

        protected virtual GeneratedCodeAnalysisFlags GeneratedCodeAnalysisFlags => GeneratedCodeAnalysisFlags.None;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags);

            context.RegisterSyntaxTreeAction(HandleSyntaxTree);
        }

        protected virtual Diagnostic CreateDiagnostic(SyntaxToken token)
        {
            return Diagnostic.Create(Descriptor, token.GetLocation());
        }

        private void HandleSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            foreach (var token in context.Tree.GetRoot(context.CancellationToken).DescendantTokens())
            {
                if (!Tokens.Contains(token.RawKind))
                {
                    continue;
                }

                context.ReportDiagnostic(CreateDiagnostic(token));
            }
        }
    }
}
