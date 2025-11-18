// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    using static RoslynDiagnosticsAnalyzersResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class RelaxTestNamingSuppressor : DiagnosticSuppressor
    {
        private const string Id = RoslynDiagnosticIds.RelaxTestNamingSuppressionRuleId;

        // VSTHRD200: Use Async suffix for async methods
        // https://github.com/microsoft/vs-threading/blob/main/doc/analyzers/VSTHRD200.md
        private const string SuppressedDiagnosticId = "VSTHRD200";

        internal static readonly SuppressionDescriptor Rule =
            new(Id, SuppressedDiagnosticId, CreateLocalizableResourceString(nameof(RelaxTestNamingSuppressorJustification)));

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.Create(Rule);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            context.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.XunitFactAttribute, out var factAttribute);
            context.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.BenchmarkDotNetAttributesBenchmarkAttribute, out var benchmarkAttribute);
            if (factAttribute is null && benchmarkAttribute is null)
            {
                return;
            }

            var knownTestAttributes = new ConcurrentDictionary<INamedTypeSymbol, bool>();

            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                // The diagnostic is reported on the test method
                if (diagnostic.Location.SourceTree is not { } tree)
                {
                    continue;
                }

                var root = tree.GetRoot(context.CancellationToken);
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

                var semanticModel = context.GetSemanticModel(tree);
                var declaredSymbol = semanticModel.GetDeclaredSymbol(node, context.CancellationToken);
                if (declaredSymbol is IMethodSymbol method
                    && method.IsBenchmarkOrXUnitTestMethod(knownTestAttributes, benchmarkAttribute, factAttribute))
                {
                    context.ReportSuppression(Suppression.Create(Rule, diagnostic));
                }
            }
        }
    }
}
