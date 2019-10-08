// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(EnableConcurrentExecutionFix))]
    [Shared]
    public sealed class EnableConcurrentExecutionFix : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(EnableConcurrentExecutionAnalyzer.Rule.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeAnalysisDiagnosticsResources.EnableConcurrentExecutionFix,
                        cancellationToken => EnableConcurrentExecutionAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                        equivalenceKey: nameof(EnableConcurrentExecutionFix)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private async Task<Document> EnableConcurrentExecutionAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var analysisContextParameter = root.FindNode(sourceSpan, getInnermostNodeForTie: true);

            var generator = SyntaxGenerator.GetGenerator(document);

            var parameterDeclaration = analysisContextParameter;
            var declarationKind = generator.GetDeclarationKind(parameterDeclaration);
            while (declarationKind != DeclarationKind.Parameter)
            {
                parameterDeclaration = generator.GetDeclaration(parameterDeclaration.Parent);
                if (parameterDeclaration is null)
                {
                    return document;
                }

                declarationKind = generator.GetDeclarationKind(parameterDeclaration);
            }

            var methodDeclaration = parameterDeclaration.Parent;
            declarationKind = generator.GetDeclarationKind(methodDeclaration);
            while (declarationKind != DeclarationKind.Method)
            {
                methodDeclaration = generator.GetDeclaration(methodDeclaration.Parent);
                if (methodDeclaration is null)
                {
                    return document;
                }

                declarationKind = generator.GetDeclarationKind(methodDeclaration);
            }

            var statements = generator.GetStatements(methodDeclaration);
            var newInvocation = generator.InvocationExpression(
                generator.MemberAccessExpression(
                    generator.IdentifierName(generator.GetName(parameterDeclaration)),
                    nameof(AnalysisContext.EnableConcurrentExecution)));

            var newStatements = new SyntaxNode[] { newInvocation }.Concat(statements).ToArray();
            var newMethodDeclaration = generator.WithStatements(methodDeclaration, newStatements);
            return document.WithSyntaxRoot(root.ReplaceNode(methodDeclaration, newMethodDeclaration));
        }
    }
}
