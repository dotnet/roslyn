// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(CompareSymbolsCorrectlyFix))]
    [Shared]
    public class CompareSymbolsCorrectlyFix : CodeFixProvider
    {
        private const string s_equalityComparerIdentifier = "Microsoft.CodeAnalysis.SymbolEqualityComparer";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CompareSymbolsCorrectlyAnalyzer.Rule.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        CodeAnalysisDiagnosticsResources.CompareSymbolsCorrectlyCodeFix,
                        cancellationToken => ConvertToEqualsAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                        equivalenceKey: nameof(CompareSymbolsCorrectlyFix)),
                    diagnostic);
            }

            return Task.CompletedTask;
        }

        private async Task<Document> ConvertToEqualsAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var expression = root.FindNode(sourceSpan, getInnermostNodeForTie: true);
            var rawOperation = semanticModel.GetOperation(expression, cancellationToken);

            return rawOperation switch
            {
                IBinaryOperation binaryOperation => await ConvertToEqualsAsync(document, semanticModel, binaryOperation, cancellationToken).ConfigureAwait(false),
                IMethodReferenceOperation methodReferenceOperation => await EnsureEqualsCorrectAsync(document, semanticModel, methodReferenceOperation, cancellationToken).ConfigureAwait(false),
                _ => document
            };
        }

        private static async Task<Document> EnsureEqualsCorrectAsync(Document document, SemanticModel semanticModel, IMethodReferenceOperation methodReference, CancellationToken cancellationToken)
        {
            if (!UseEqualityComparer(semanticModel.Compilation))
            {
                return document;
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var replacement = generator.AddParameters(methodReference.Syntax, new[] { generator.IdentifierName(s_equalityComparerIdentifier) });
            editor.ReplaceNode(methodReference.Syntax, replacement.WithTriviaFrom(methodReference.Syntax));
            return editor.GetChangedDocument();
        }

        private static async Task<Document> ConvertToEqualsAsync(Document document, SemanticModel semanticModel, IBinaryOperation binaryOperation, CancellationToken cancellationToken)
        {

            var expression = binaryOperation.Syntax;
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var replacement = UseEqualityComparer(semanticModel.Compilation) switch
            {
                true => 
                    generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.MemberAccessExpression(
                                generator.DottedName(s_equalityComparerIdentifier),
                                "Default"),
                            nameof(object.Equals)),
                        binaryOperation.LeftOperand.Syntax.WithoutLeadingTrivia(),
                        binaryOperation.RightOperand.Syntax.WithoutTrailingTrivia()),

                false =>
                    generator.InvocationExpression(
                        generator.MemberAccessExpression(
                            generator.TypeExpression(semanticModel.Compilation.GetSpecialType(SpecialType.System_Object)),
                            nameof(object.Equals)),
                        binaryOperation.LeftOperand.Syntax.WithoutLeadingTrivia(),
                        binaryOperation.RightOperand.Syntax.WithoutTrailingTrivia())
            };

            if (binaryOperation.OperatorKind == BinaryOperatorKind.NotEquals)
            {
                replacement = generator.LogicalNotExpression(replacement);
            }

            editor.ReplaceNode(expression, replacement.WithTriviaFrom(expression));
            return editor.GetChangedDocument();
        }

        private static bool UseEqualityComparer(Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(s_equalityComparerIdentifier) is object;
        }
    }
}
