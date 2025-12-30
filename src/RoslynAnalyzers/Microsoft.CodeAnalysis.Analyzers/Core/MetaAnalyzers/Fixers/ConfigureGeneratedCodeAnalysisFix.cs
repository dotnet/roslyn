// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    public abstract class ConfigureGeneratedCodeAnalysisFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = [ConfigureGeneratedCodeAnalysisAnalyzer.Rule.Id];

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeAnalysisDiagnosticsResources.ConfigureGeneratedCodeAnalysisFix,
                        cancellationToken => ConfigureGeneratedCodeAnalysisAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                        equivalenceKey: nameof(ConfigureGeneratedCodeAnalysisFix)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private async Task<Document> ConfigureGeneratedCodeAnalysisAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generatedCodeAnalysisFlags = semanticModel.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsGeneratedCodeAnalysisFlags);
            if (generatedCodeAnalysisFlags is null)
            {
                return document;
            }

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var analysisContextParameter = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var parameterDeclaration = generator.TryGetContainingDeclaration(analysisContextParameter, DeclarationKind.Parameter);
            if (parameterDeclaration is null)
            {
                return document;
            }

            var methodDeclaration = generator.TryGetContainingDeclaration(parameterDeclaration.Parent, DeclarationKind.Method);
            if (methodDeclaration is null)
            {
                return document;
            }

            var statements = GetStatements(methodDeclaration);
            var newInvocation = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.IdentifierName(generator.GetName(parameterDeclaration)),
                    nameof(AnalysisContext.ConfigureGeneratedCodeAnalysis)),
                generator.BitwiseOrExpression(
                    generator.MemberAccessExpression(
                        generator.TypeExpressionForStaticMemberAccess(generatedCodeAnalysisFlags),
                        nameof(GeneratedCodeAnalysisFlags.Analyze)),
                    generator.MemberAccessExpression(
                        generator.TypeExpressionForStaticMemberAccess(generatedCodeAnalysisFlags),
                        nameof(GeneratedCodeAnalysisFlags.ReportDiagnostics))));

            var newStatements = new SyntaxNode[] { newInvocation }.Concat(statements);
            var newMethodDeclaration = generator.WithStatements(methodDeclaration, newStatements);
            return document.WithSyntaxRoot(root.ReplaceNode(methodDeclaration, newMethodDeclaration));
        }

        protected abstract IEnumerable<SyntaxNode> GetStatements(SyntaxNode methodDeclaration);
    }
}
