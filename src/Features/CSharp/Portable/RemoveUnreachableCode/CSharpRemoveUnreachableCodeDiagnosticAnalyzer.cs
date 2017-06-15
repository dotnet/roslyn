// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnreachableCode
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpRemoveUnreachableCodeDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public const string IsCascadedStatement = nameof(IsCascadedStatement);

        private const string CS0162 = nameof(CS0162); // Unreachable code detected

        public CSharpRemoveUnreachableCodeDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.RemoveUnreachableCodeDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Unreachable_code_detected), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeBlock, ImmutableArray.Create(SyntaxKind.Block));
        }

        private void AnalyzeBlock(SyntaxNodeAnalysisContext context)
        {
            var block = (BlockSyntax)context.Node;
            if (HasOuterBlock(block))
            {
                // Don't bother processing inner blocks.  We'll have already checked then when
                // we processed the outer block
                return;
            }

            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            var root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            var diagnostics = semanticModel.GetDiagnostics(block.Span, cancellationToken);
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (diagnostic.Id == CS0162)
                {
                    ProcessUnreachableDiagnostic(context, root, diagnostic.Location.SourceSpan);
                }
            }
        }

        private bool HasOuterBlock(SyntaxNode block)
        {
            for (var current = block.Parent; current != null; current = current.Parent)
            {
                if (current.Kind() == SyntaxKind.Block)
                {
                    return true;
                }
            }

            return false;
        }

        private void ProcessUnreachableDiagnostic(
            SyntaxNodeAnalysisContext context, SyntaxNode root, TextSpan sourceSpan)
        {
            var node = root.FindNode(sourceSpan);
            var firstUnreachableStatement = node.FirstAncestorOrSelf<StatementSyntax>();
            if (firstUnreachableStatement == null)
            {
                return;
            }

            // Fade out the statement that the unreachable code warning is placed on.
            var firstStatementLocation = root.SyntaxTree.GetLocation(firstUnreachableStatement.FullSpan);

            if (!firstUnreachableStatement.IsParentKind(SyntaxKind.Block) &&
                !firstUnreachableStatement.IsParentKind(SyntaxKind.SwitchSection))
            {
                // Can't actually remove this statement (it's an embedded statement in something 
                // like an 'if-statement'.  Just fade the code out, but don't offer to remove it.
                context.ReportDiagnostic(
                    Diagnostic.Create(UnnecessaryWithoutSuggestionDescriptor, firstStatementLocation));
                return;
            }

            var additionalLocations = SpecializedCollections.SingletonEnumerable(firstStatementLocation);
            context.ReportDiagnostic(
                Diagnostic.Create(UnnecessaryWithSuggestionDescriptor, firstStatementLocation, additionalLocations));

            // Now try to fade out subsequent statements as necessary.
            var subsequentUnreachableStatements = RemoveUnreachableCodeHelpers.GetSubsequentUnreachableStatements(firstUnreachableStatement);

            if (subsequentUnreachableStatements.Length > 0)
            {
                var additionalProperties = ImmutableDictionary<string, string>.Empty.Add(IsCascadedStatement, "");

                foreach (var nextStatement in subsequentUnreachableStatements)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(UnnecessaryWithSuggestionDescriptor,
                        root.SyntaxTree.GetLocation(nextStatement.FullSpan),
                        additionalLocations,
                        additionalProperties));
                }
            }
        }
    }
}