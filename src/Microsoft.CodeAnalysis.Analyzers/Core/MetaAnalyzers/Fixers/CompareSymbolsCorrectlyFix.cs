// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    public abstract class CompareSymbolsCorrectlyFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CompareSymbolsCorrectlyAnalyzer.EqualityRule.Id);

        protected abstract SyntaxNode CreateConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Descriptor == CompareSymbolsCorrectlyAnalyzer.EqualityRule)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            CodeAnalysisDiagnosticsResources.CompareSymbolsCorrectlyCodeFix,
                            cancellationToken => ConvertToEqualsAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                            equivalenceKey: nameof(CompareSymbolsCorrectlyFix)),
                        diagnostic);
                }
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
                IInvocationOperation invocationOperation => await EnsureEqualsCorrectAsync(document, semanticModel, invocationOperation, cancellationToken).ConfigureAwait(false),
                _ => document
            };
        }

        private async Task<Document> EnsureEqualsCorrectAsync(Document document, SemanticModel semanticModel, IInvocationOperation invocationOperation, CancellationToken cancellationToken)
        {
            if (!CompareSymbolsCorrectlyAnalyzer.UseSymbolEqualityComparer(semanticModel.Compilation))
            {
                return document;
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            // With "a?.b?.c?.Equals(b)", the invocation operation syntax is only ".Equals(b)".
            // Walk-up the tree to find all parts of the conditional access and also the root
            // so that we can replace the whole expression.
            var conditionalAccessMembers = new List<SyntaxNode>();
            IOperation currentOperation = invocationOperation;
            while (currentOperation.Parent is IConditionalAccessOperation conditionalAccess)
            {
                currentOperation = conditionalAccess;
                conditionalAccessMembers.Add(conditionalAccess.Operation.Syntax);
            }

            var arguments = GetNewInvocationArguments(invocationOperation, conditionalAccessMembers);
            var replacement = generator.InvocationExpression(GetEqualityComparerDefaultEquals(generator), arguments);

            var nodeToReplace = currentOperation.Syntax;
            editor.ReplaceNode(nodeToReplace, replacement.WithTriviaFrom(nodeToReplace));

            return editor.GetChangedDocument();
        }

        private IEnumerable<SyntaxNode> GetNewInvocationArguments(IInvocationOperation invocationOperation,
            List<SyntaxNode> conditionalAccessMembers)
        {
            var arguments = invocationOperation.Arguments.Select(argument => argument.Syntax);

            if (invocationOperation.GetInstance() is not IOperation instance)
            {
                return arguments;
            }

            if (instance.Kind != OperationKind.ConditionalAccessInstance)
            {
                return new[] { instance.Syntax }.Concat(arguments);
            }

            // We need to rebuild a new conditional access chain skipping the invocation
            if (conditionalAccessMembers.Count == 0)
            {
                throw new InvalidOperationException("Invocation contains conditional expression but we could not detect the parts.");
            }
            else if (conditionalAccessMembers.Count == 1)
            {
                return new[] { conditionalAccessMembers[0] }.Concat(arguments);
            }
            else
            {
                var currentExpression = conditionalAccessMembers.Count > 2
                    ? CreateConditionalAccessExpression(conditionalAccessMembers[1], conditionalAccessMembers[0])
                    : conditionalAccessMembers[0];

                for (int i = 2; i < conditionalAccessMembers.Count - 1; i++)
                {
                    currentExpression = CreateConditionalAccessExpression(conditionalAccessMembers[i], currentExpression);
                }

                currentExpression = CreateConditionalAccessExpression(conditionalAccessMembers[^1], currentExpression);
                return new[] { currentExpression }.Concat(arguments);
            }
        }

        private static async Task<Document> ConvertToEqualsAsync(Document document, SemanticModel semanticModel, IBinaryOperation binaryOperation, CancellationToken cancellationToken)
        {
            var expression = binaryOperation.Syntax;
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var replacement = CompareSymbolsCorrectlyAnalyzer.UseSymbolEqualityComparer(semanticModel.Compilation) switch
            {
                true =>
                    generator.InvocationExpression(
                        GetEqualityComparerDefaultEquals(generator),
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

        private static SyntaxNode GetEqualityComparerDefaultEquals(SyntaxGenerator generator)
            => generator.MemberAccessExpression(
                    GetEqualityComparerDefault(generator),
                    nameof(object.Equals));

        private static SyntaxNode GetEqualityComparerDefault(SyntaxGenerator generator)
            => generator.MemberAccessExpression(generator.DottedName(CompareSymbolsCorrectlyAnalyzer.SymbolEqualityComparerName), "Default");
    }
}
