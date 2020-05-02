// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp)]
    [Shared]
    public sealed class CSharpAvoidOptSuffixForNullableEnableCodeCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CSharpAvoidOptSuffixForNullableEnableCode.Rule.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var title = RoslynDiagnosticsAnalyzersResources.TestExportsShouldNotBeDiscoverableCodeFix;

            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title,
                        cancellationToken => RemoveOptSuffixOnVariableAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                        equivalenceKey: title),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private static async Task<Document> RemoveOptSuffixOnVariableAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var variable = root.FindNode(sourceSpan, getInnermostNodeForTie: true);
            var variableSymbol = semanticModel.GetDeclaredSymbol(variable, cancellationToken);

            var newSolution = await Renamer.RenameSymbolAsync(
                    document.Project.Solution,
                    variableSymbol,
                    variableSymbol.Name.Substring(0, variableSymbol.Name.Length - CSharpAvoidOptSuffixForNullableEnableCode.OptSuffix.Length),
                    document.Project.Solution.Options,
                    cancellationToken)
                .ConfigureAwait(false);

            return newSolution.GetDocument(document.Id)!;
        }
    }
}
