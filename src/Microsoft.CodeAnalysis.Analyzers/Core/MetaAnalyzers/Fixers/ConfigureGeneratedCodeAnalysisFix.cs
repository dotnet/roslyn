// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(ConfigureGeneratedCodeAnalysisFix))]
    [Shared]
    public sealed class ConfigureGeneratedCodeAnalysisFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ConfigureGeneratedCodeAnalysisAnalyzer.Rule.Id);

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
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var generatedCodeAnalysisFlags = semanticModel.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsGeneratedCodeAnalysisFlags);
            if (generatedCodeAnalysisFlags is null)
            {
                return document;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
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

            var statements = generator.GetStatements(methodDeclaration);
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

            var newStatements = new SyntaxNode[] { newInvocation }.Concat(statements).ToArray();
            var newMethodDeclaration = generator.WithStatements(methodDeclaration, newStatements);
            return document.WithSyntaxRoot(root.ReplaceNode(methodDeclaration, newMethodDeclaration));
        }
    }
}
