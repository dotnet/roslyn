// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseCompoundCoalesceAssignment), Shared]
    internal class CSharpUseCompoundCoalesceAssignmentCodeFixProvider
        : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpUseCompoundCoalesceAssignmentCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, AnalyzersResources.Use_compound_assignment, nameof(AnalyzersResources.Use_compound_assignment));
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxKinds = syntaxFacts.SyntaxKinds;

            foreach (var diagnostic in diagnostics)
            {
                var coalesce = diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken);

                // changing from `x ?? (x = y)` to `x ??= y` can change the type.  Specifically,
                // with nullable value types (`int?`) it could change from `int?` to `int`.
                //
                // Add an explicit cast to the original type to ensure semantics are preserved. 
                // Simplification engine can then remove it if it's not necessary.
                var type = semanticModel.GetTypeInfo(coalesce, cancellationToken).Type;

                editor.ReplaceNode(coalesce,
                    (currentCoalesceNode, generator) =>
                    {
                        var currentCoalesce = (BinaryExpressionSyntax)currentCoalesceNode;
                        var coalesceRight = (ParenthesizedExpressionSyntax)currentCoalesce.Right;
                        var assignment = (AssignmentExpressionSyntax)coalesceRight.Expression;

                        var compoundOperator = SyntaxFactory.Token(SyntaxKind.QuestionQuestionEqualsToken);
                        var finalAssignment = SyntaxFactory.AssignmentExpression(
                            SyntaxKind.CoalesceAssignmentExpression,
                            assignment.Left,
                            compoundOperator.WithTriviaFrom(assignment.OperatorToken),
                            assignment.Right);

                        return type == null || type.IsErrorType()
                            ? finalAssignment
                            : generator.CastExpression(type, finalAssignment);
                    });
            }
        }
    }
}
