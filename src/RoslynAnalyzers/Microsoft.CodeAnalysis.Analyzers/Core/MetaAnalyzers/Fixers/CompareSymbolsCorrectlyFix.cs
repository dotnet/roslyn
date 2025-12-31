// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers.Fixers
{
    public abstract class CompareSymbolsCorrectlyFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            [DiagnosticIds.CompareSymbolsCorrectlyRuleId];

        protected abstract SyntaxNode CreateConditionalAccessExpression(SyntaxNode expression, SyntaxNode whenNotNull);

        protected abstract SyntaxNode GetExpression(IInvocationOperation invocationOperation);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                if (diagnostic.Properties.TryGetValue(CompareSymbolsCorrectlyAnalyzer.RulePropertyName, out var rule))
                {
                    switch (rule)
                    {
                        case CompareSymbolsCorrectlyAnalyzer.EqualityRuleName:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    CodeAnalysisDiagnosticsResources.CompareSymbolsCorrectlyCodeFix,
                                    cancellationToken => ConvertToEqualsAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                                    equivalenceKey: nameof(CompareSymbolsCorrectlyFix)),
                                diagnostic);
                            break;
                        case CompareSymbolsCorrectlyAnalyzer.CollectionRuleName:
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    CodeAnalysisDiagnosticsResources.CompareSymbolsCorrectlyCodeFix,
                                    cancellationToken => CallOverloadWithEqualityComparerAsync(context.Document, diagnostic.Location.SourceSpan, cancellationToken),
                                    equivalenceKey: nameof(CompareSymbolsCorrectlyFix)),
                                diagnostic);
                            break;
                    }
                }
            }
        }

        private async Task<Document> ConvertToEqualsAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var expression = root.FindNode(sourceSpan, getInnermostNodeForTie: true);
            var rawOperation = semanticModel.GetOperation(expression, cancellationToken);

            return rawOperation switch
            {
                IBinaryOperation binaryOperation => await ConvertToEqualsAsync(document, semanticModel, binaryOperation, cancellationToken).ConfigureAwait(false),
                IInvocationOperation invocationOperation => await EnsureEqualsCorrectAsync(document, semanticModel, invocationOperation, cancellationToken).ConfigureAwait(false),
                _ => document
            };
        }

        private async Task<Document> CallOverloadWithEqualityComparerAsync(Document document, TextSpan sourceSpan, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var expression = root.FindNode(sourceSpan, getInnermostNodeForTie: true);
            var rawOperation = semanticModel.GetOperation(expression, cancellationToken);

            if (!CompareSymbolsCorrectlyAnalyzer.UseSymbolEqualityComparer(semanticModel.Compilation) ||
                !semanticModel.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIEqualityComparer1, out var iEqualityComparer))
            {
                return document;
            }

            return rawOperation switch
            {
                IObjectCreationOperation objectCreation when objectCreation.Type != null =>
                    await CallOverloadWithEqualityComparerAsync(
                        document, objectCreation.Syntax, objectCreation.Constructor, objectCreation.Arguments, isUsedAsExtensionMethod: false,
                        (generator, args) => generator.ObjectCreationExpression(objectCreation.Type, args), iEqualityComparer, cancellationToken)
                    .ConfigureAwait(false),

                IInvocationOperation invocation =>
                    await CallOverloadWithEqualityComparerAsync(
                        document, invocation.Syntax, invocation.TargetMethod, invocation.Arguments, IsExtensionMethodUsedAsSuch(invocation),
                        (generator, args) => generator.InvocationExpression(GetExpression(invocation), args), iEqualityComparer, cancellationToken)
                    .ConfigureAwait(false),

                _ => document,
            };

            static bool IsExtensionMethodUsedAsSuch(IInvocationOperation invocation)
                => invocation.IsExtensionMethodAndHasNoInstance()
                && invocation.Arguments.Length > 0
                && invocation.Arguments[0].IsImplicit;
        }

        private static async Task<Document> CallOverloadWithEqualityComparerAsync(Document document, SyntaxNode nodeToReplace, IMethodSymbol? methodSymbol,
            ImmutableArray<IArgumentOperation> arguments, bool isUsedAsExtensionMethod, Func<SyntaxGenerator, IEnumerable<SyntaxNode>, SyntaxNode> getReplacementNode,
            INamedTypeSymbol iEqualityComparer, CancellationToken cancellationToken)
        {
            if (!TryFindSymbolEqualityComparerOverload(methodSymbol, iEqualityComparer, out var symbolEqualityParameterPosition))
            {
                return document;
            }

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var generator = editor.Generator;

            var extensionMethodShift = isUsedAsExtensionMethod ? 1 : 0;
            var syntaxArguments = arguments.Skip(extensionMethodShift).Select(x => x.Syntax).ToList();
            if (symbolEqualityParameterPosition == 0)
            {
                syntaxArguments.Insert(0, GetEqualityComparerDefault(generator));
            }
            else
            {
                syntaxArguments.Add(GetEqualityComparerDefault(generator));
            }

            var replacement = getReplacementNode(generator, syntaxArguments);
            editor.ReplaceNode(nodeToReplace, replacement.WithTriviaFrom(nodeToReplace));

            return editor.GetChangedDocument();
        }

        private static bool TryFindSymbolEqualityComparerOverload(IMethodSymbol? methodSymbol, INamedTypeSymbol iEqualityComparer, out int symbolEqualityParameterPosition)
        {
            symbolEqualityParameterPosition = -1;
            if (methodSymbol == null)
                return false;

            var overloads = methodSymbol.GetOverloads();
            methodSymbol = (methodSymbol.ReducedFrom ?? methodSymbol).ConstructedFrom;
            var methodArgsCount = methodSymbol.Parameters.Length;

            foreach (var overload in overloads)
            {
                if (overload.Parameters.Length != methodArgsCount + 1)
                {
                    continue;
                }

                // We currently only support adding the equality comparer at the beginning or at the end of the arguments list.
                if (SymbolEqualityComparer.Default.Equals(overload.Parameters[0].Type.OriginalDefinition, iEqualityComparer))
                {
                    if (AreCollectionsEqual(overload.Parameters.Skip(1), methodSymbol.Parameters))
                    {
                        symbolEqualityParameterPosition = 0;
                        return true;
                    }
                }
                else if (SymbolEqualityComparer.Default.Equals(overload.Parameters[^1].Type.OriginalDefinition, iEqualityComparer))
                {
                    if (AreCollectionsEqual(overload.Parameters.Take(methodArgsCount), methodSymbol.Parameters))
                    {
                        symbolEqualityParameterPosition = methodArgsCount;
                        return true;
                    }
                }
            }

            return false;

            static bool AreCollectionsEqual(IEnumerable<IParameterSymbol> c1, IEnumerable<IParameterSymbol> c2)
                => c1.Zip(c2, (x, y) => x.ParameterTypesAreSame(y)).All(x => x);
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

                for (var i = 2; i < conditionalAccessMembers.Count - 1; i++)
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
