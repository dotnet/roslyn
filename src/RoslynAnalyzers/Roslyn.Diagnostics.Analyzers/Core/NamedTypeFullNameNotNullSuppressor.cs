// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Roslyn.Diagnostics.Analyzers
{
    using static RoslynDiagnosticsAnalyzersResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class NamedTypeFullNameNotNullSuppressor : DiagnosticSuppressor
    {
        private const string Id = RoslynDiagnosticIds.NamedTypeFullNameNotNullSuppressionRuleId;

        // CS8600: Converting null literal or possible null value to non-nullable type
        private const string CS8600 = nameof(CS8600);

        // CS8603: Possible null reference return
        private const string CS8603 = nameof(CS8603);

        // CS8604: Possible null reference argument for parameter 'name' in 'method'
        private const string CS8604 = nameof(CS8604);

        private static readonly LocalizableString s_localizableJustification = CreateLocalizableResourceString(nameof(NamedTypeFullNameNotNullSuppressorJustification));

        internal static readonly SuppressionDescriptor CS8600Rule = new(Id, CS8600, s_localizableJustification);
        internal static readonly SuppressionDescriptor CS8603Rule = new(Id, CS8603, s_localizableJustification);
        internal static readonly SuppressionDescriptor CS8604Rule = new(Id, CS8604, s_localizableJustification);

        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = ImmutableArray.Create(CS8600Rule, CS8603Rule, CS8604Rule);

        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                if (diagnostic.Location.SourceTree is not { } tree)
                {
                    continue;
                }

                var root = tree.GetRoot(context.CancellationToken);
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                var semanticModel = context.GetSemanticModel(tree);
                var operation = semanticModel.GetOperation(node, context.CancellationToken);
                if (operation is IPropertyReferenceOperation { Property.Name: nameof(Type.FullName), Instance: ITypeOfOperation { } })
                {
                    context.ReportSuppression(Suppression.Create(GetDescriptor(diagnostic), diagnostic));
                }
            }
        }

        private static SuppressionDescriptor GetDescriptor(Diagnostic diagnostic)
        {
            return diagnostic.Id switch
            {
                CS8600 => CS8600Rule,
                CS8603 => CS8603Rule,
                CS8604 => CS8604Rule,
                _ => throw new NotSupportedException(),
            };
        }
    }
}
