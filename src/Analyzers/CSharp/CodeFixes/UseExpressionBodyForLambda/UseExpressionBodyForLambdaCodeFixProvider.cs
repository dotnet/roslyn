// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeFixes.UseExpressionBodyForLambda;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExpressionBodyForLambda), Shared]
    internal sealed class UseExpressionBodyForLambdaCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseExpressionBodyForLambdaCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics[0];

            var title = diagnostic.GetMessage();
            var codeAction = CodeAction.Create(
                title,
                c => FixWithSyntaxEditorAsync(document, diagnostic, c),
                title);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
            => FixAllAsync(document, diagnostics, editor, cancellationToken);

        private static async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(editor, semanticModel, diagnostic, cancellationToken);
            }
        }

        private static Task<Document> FixWithSyntaxEditorAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
            => FixAllWithEditorAsync(
                document, editor => FixAllAsync(document, ImmutableArray.Create(diagnostic), editor, cancellationToken), cancellationToken);

        private static void AddEdits(
            SyntaxEditor editor, SemanticModel semanticModel,
            Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var declarationLocation = diagnostic.AdditionalLocations[0];
            var originalDeclaration = (LambdaExpressionSyntax)declarationLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);

            editor.ReplaceNode(
                originalDeclaration,
                (current, _) => UseExpressionBodyForLambdaCodeActionHelpers.Update(semanticModel, originalDeclaration, (LambdaExpressionSyntax)current, cancellationToken));
        }
    }
}
