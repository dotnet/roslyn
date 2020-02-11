// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal abstract partial class MethodExtractor
    {
        protected readonly SelectionResult OriginalSelectionResult;
        protected readonly bool LocalFunction;

        public MethodExtractor(SelectionResult selectionResult, bool localFunction)
        {
            Contract.ThrowIfNull(selectionResult);
            OriginalSelectionResult = selectionResult;
            LocalFunction = localFunction;
        }

        protected abstract Task<AnalyzerResult> AnalyzeAsync(SelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken);
        protected abstract Task<InsertionPoint> GetInsertionPointAsync(SemanticDocument document, CancellationToken cancellationToken);
        protected abstract Task<TriviaResult> PreserveTriviaAsync(SelectionResult selectionResult, CancellationToken cancellationToken);
        protected abstract Task<SemanticDocument> ExpandAsync(SelectionResult selection, CancellationToken cancellationToken);

        protected abstract Task<GeneratedCode> GenerateCodeAsync(InsertionPoint insertionPoint, SelectionResult selectionResult, AnalyzerResult analyzeResult, OptionSet options, CancellationToken cancellationToken);

        protected abstract SyntaxToken GetMethodNameAtInvocation(IEnumerable<SyntaxNodeOrToken> methodNames);
        protected abstract IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document);

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
            var options = await analyzeResult.SemanticDocument.Document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var generatedCode = await GenerateCodeAsync(
                insertionPoint.With(expandedDocument),
                OriginalSelectionResult.With(expandedDocument),
                analyzeResult.With(expandedDocument),
                options,
                cancellationToken).ConfigureAwait(false);

            var applied = await triviaResult.ApplyAsync(generatedCode, cancellationToken).ConfigureAwait(false);
            var afterTriviaRestored = applied.With(operationStatus);
            cancellationToken.ThrowIfCancellationRequested();

            if (afterTriviaRestored.Status.FailedWithNoBestEffortSuggestion())
            {
                return await CreateExtractMethodResultAsync(
                    operationStatus, generatedCode.SemanticDocument, generatedCode.MethodNameAnnotation, generatedCode.MethodDefinitionAnnotation, cancellationToken).ConfigureAwait(false);
            }

            var finalDocument = afterTriviaRestored.Data.Document;
            finalDocument = await Formatter.FormatAsync(
                finalDocument,
                Formatter.Annotation,
                options: null,
                rules: GetFormattingRules(finalDocument),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return await CreateExtractMethodResultAsync(
                operationStatus.With(generatedCode.Status),
                await SemanticDocument.CreateAsync(finalDocument, cancellationToken).ConfigureAwait(false),
                generatedCode.MethodNameAnnotation,
                generatedCode.MethodDefinitionAnnotation,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<ExtractMethodResult> CreateExtractMethodResultAsync(
            OperationStatus status, SemanticDocument semanticDocument,
            SyntaxAnnotation invocationAnnotation, SyntaxAnnotation methodAnnotation,
            CancellationToken cancellationToken)
        {
            var newRoot = semanticDocument.Root;
            var methodName = GetMethodNameAtInvocation(newRoot.GetAnnotatedNodesAndTokens(invocationAnnotation));
            var methodDefinition = newRoot.GetAnnotatedNodesAndTokens(methodAnnotation).FirstOrDefault().AsNode();

            if (LocalFunction && status.Succeeded())
            {
                var result = await InsertNewLineBeforeLocalFunctionIfNecessaryAsync(semanticDocument.Document, methodName, methodDefinition, cancellationToken).ConfigureAwait(false);
                return new SimpleExtractMethodResult(status, result.document, result.methodName, result.methodDefinition);
            }

            return new SimpleExtractMethodResult(status, semanticDocument.Document, methodName, methodDefinition);
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
                prefix = char.ToLowerInvariant(prefix[0]) + prefix.Substring(1);
            }

            return char.IsUpper(name[0]) ?
                prefix + name :
                prefix + char.ToUpper(name[0]).ToString() + name.Substring(1);
        }
    }
}
