// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        protected readonly SelectionResult OriginalSelectionResult;
        protected readonly ExtractMethodGenerationOptions Options;
        protected readonly bool LocalFunction;

        public MethodExtractor(
            SelectionResult selectionResult,
            ExtractMethodGenerationOptions options,
            bool localFunction)
        {
            Contract.ThrowIfNull(selectionResult);
            OriginalSelectionResult = selectionResult;
            Options = options;
            LocalFunction = localFunction;
        }

        protected abstract Task<AnalyzerResult> AnalyzeAsync(SelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken);
        protected abstract Task<InsertionPoint> GetInsertionPointAsync(SemanticDocument document, CancellationToken cancellationToken);
        protected abstract Task<TriviaResult> PreserveTriviaAsync(SelectionResult selectionResult, CancellationToken cancellationToken);
        protected abstract Task<SemanticDocument> ExpandAsync(SelectionResult selection, CancellationToken cancellationToken);

        protected abstract Task<GeneratedCode> GenerateCodeAsync(InsertionPoint insertionPoint, SelectionResult selectionResult, AnalyzerResult analyzeResult, CodeGenerationOptions options, NamingStylePreferencesProvider namingPreferences, CancellationToken cancellationToken);

        protected abstract SyntaxToken GetMethodNameAtInvocation(IEnumerable<SyntaxNodeOrToken> methodNames);
        protected abstract ImmutableArray<AbstractFormattingRule> GetCustomFormattingRules(Document document);

        protected abstract Task<OperationStatus> CheckTypeAsync(Document document, SyntaxNode contextNode, Location location, ITypeSymbol type, CancellationToken cancellationToken);

        protected abstract Task<(Document document, SyntaxToken methodName, SyntaxNode methodDefinition)> InsertNewLineBeforeLocalFunctionIfNecessaryAsync(Document document, SyntaxToken methodName, SyntaxNode methodDefinition, CancellationToken cancellationToken);

        public async Task<ExtractMethodResult> ExtractMethodAsync(CancellationToken cancellationToken)
        {
            var operationStatus = OriginalSelectionResult.Status;

            var analyzeResult = await AnalyzeAsync(OriginalSelectionResult, LocalFunction, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            operationStatus = await CheckVariableTypesAsync(analyzeResult.Status.With(operationStatus), analyzeResult, cancellationToken).ConfigureAwait(false);
            if (operationStatus.FailedWithNoBestEffortSuggestion())
            {
                return new FailedExtractMethodResult(operationStatus);
            }

            var insertionPoint = await GetInsertionPointAsync(analyzeResult.SemanticDocument, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var triviaResult = await PreserveTriviaAsync(OriginalSelectionResult.With(insertionPoint.SemanticDocument), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var expandedDocument = await ExpandAsync(OriginalSelectionResult.With(triviaResult.SemanticDocument), cancellationToken).ConfigureAwait(false);

            var generatedCode = await GenerateCodeAsync(
                insertionPoint.With(expandedDocument),
                OriginalSelectionResult.With(expandedDocument),
                analyzeResult.With(expandedDocument),
                Options.CodeGenerationOptions,
                Options.NamingPreferences,
                cancellationToken).ConfigureAwait(false);

            var applied = await triviaResult.ApplyAsync(generatedCode, cancellationToken).ConfigureAwait(false);
            var afterTriviaRestored = applied.With(operationStatus);
            cancellationToken.ThrowIfCancellationRequested();

            if (afterTriviaRestored.Status.FailedWithNoBestEffortSuggestion())
            {
                return await CreateExtractMethodResultAsync(
                    operationStatus, generatedCode.SemanticDocument, ImmutableArray<AbstractFormattingRule>.Empty, generatedCode.MethodNameAnnotation, generatedCode.MethodDefinitionAnnotation, cancellationToken).ConfigureAwait(false);
            }

            var documentWithoutFinalFormatting = afterTriviaRestored.Data.Document;

            cancellationToken.ThrowIfCancellationRequested();
            return await CreateExtractMethodResultAsync(
                operationStatus.With(generatedCode.Status),
                await SemanticDocument.CreateAsync(documentWithoutFinalFormatting, cancellationToken).ConfigureAwait(false),
                GetFormattingRules(documentWithoutFinalFormatting),
                generatedCode.MethodNameAnnotation,
                generatedCode.MethodDefinitionAnnotation,
                cancellationToken).ConfigureAwait(false);
        }

        private ImmutableArray<AbstractFormattingRule> GetFormattingRules(Document document)
            => GetCustomFormattingRules(document).AddRange(Formatter.GetDefaultFormattingRules(document));

        private async Task<ExtractMethodResult> CreateExtractMethodResultAsync(
            OperationStatus status, SemanticDocument semanticDocumentWithoutFinalFormatting,
            ImmutableArray<AbstractFormattingRule> formattingRules,
            SyntaxAnnotation invocationAnnotation, SyntaxAnnotation methodAnnotation,
            CancellationToken cancellationToken)
        {
            var newRoot = semanticDocumentWithoutFinalFormatting.Root;
            var methodName = GetMethodNameAtInvocation(newRoot.GetAnnotatedNodesAndTokens(invocationAnnotation));
            var methodDefinition = newRoot.GetAnnotatedNodesAndTokens(methodAnnotation).FirstOrDefault().AsNode();

            if (LocalFunction && status.Succeeded())
            {
                var result = await InsertNewLineBeforeLocalFunctionIfNecessaryAsync(semanticDocumentWithoutFinalFormatting.Document, methodName, methodDefinition, cancellationToken).ConfigureAwait(false);
                return new SimpleExtractMethodResult(status, result.document, formattingRules, result.methodName, result.methodDefinition);
            }

            return new SimpleExtractMethodResult(status, semanticDocumentWithoutFinalFormatting.Document, formattingRules, methodName, methodDefinition);
        }

        private async Task<OperationStatus> CheckVariableTypesAsync(
            OperationStatus status,
            AnalyzerResult analyzeResult,
            CancellationToken cancellationToken)
        {
            var document = analyzeResult.SemanticDocument;

            // sync selection result to same semantic data as analyzeResult
            var firstToken = OriginalSelectionResult.With(document).GetFirstTokenInSelection();
            var context = firstToken.Parent;

            var result = await TryCheckVariableTypeAsync(document, context, analyzeResult.GetVariablesToMoveIntoMethodDefinition(cancellationToken), status, cancellationToken).ConfigureAwait(false);
            if (!result.Item1)
            {
                result = await TryCheckVariableTypeAsync(document, context, analyzeResult.GetVariablesToSplitOrMoveIntoMethodDefinition(cancellationToken), result.Item2, cancellationToken).ConfigureAwait(false);
                if (!result.Item1)
                {
                    result = await TryCheckVariableTypeAsync(document, context, analyzeResult.MethodParameters, result.Item2, cancellationToken).ConfigureAwait(false);
                    if (!result.Item1)
                    {
                        result = await TryCheckVariableTypeAsync(document, context, analyzeResult.GetVariablesToMoveOutToCallSite(cancellationToken), result.Item2, cancellationToken).ConfigureAwait(false);
                        if (!result.Item1)
                        {
                            result = await TryCheckVariableTypeAsync(document, context, analyzeResult.GetVariablesToSplitOrMoveOutToCallSite(cancellationToken), result.Item2, cancellationToken).ConfigureAwait(false);
                            if (!result.Item1)
                            {
                                return result.Item2;
                            }
                        }
                    }
                }
            }

            status = result.Item2;

            var checkedStatus = await CheckTypeAsync(document.Document, context, context.GetLocation(), analyzeResult.ReturnType, cancellationToken).ConfigureAwait(false);
            return checkedStatus.With(status);
        }

        private async Task<Tuple<bool, OperationStatus>> TryCheckVariableTypeAsync(
            SemanticDocument document, SyntaxNode contextNode, IEnumerable<VariableInfo> variables,
            OperationStatus status, CancellationToken cancellationToken)
        {
            if (status.FailedWithNoBestEffortSuggestion())
            {
                return Tuple.Create(false, status);
            }

            var location = contextNode.GetLocation();

            foreach (var variable in variables)
            {
                var originalType = variable.GetVariableType(document);
                var result = await CheckTypeAsync(document.Document, contextNode, location, originalType, cancellationToken).ConfigureAwait(false);
                if (result.FailedWithNoBestEffortSuggestion())
                {
                    status = status.With(result);
                    return Tuple.Create(false, status);
                }
            }

            return Tuple.Create(true, status);
        }

        internal static string MakeMethodName(string prefix, string originalName, bool camelCase)
        {
            var startingWithLetter = originalName.ToCharArray().SkipWhile(c => !char.IsLetter(c)).ToArray();
            var name = startingWithLetter.Length == 0 ? originalName : new string(startingWithLetter);

            if (camelCase && !prefix.IsEmpty())
            {
                prefix = char.ToLowerInvariant(prefix[0]) + prefix[1..];
            }

            return char.IsUpper(name[0]) ?
                prefix + name :
                prefix + char.ToUpper(name[0]).ToString() + name[1..];
        }
    }
}
