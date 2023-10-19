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
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        protected abstract AnalyzerResult Analyze(SelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken);
        protected abstract SyntaxNode GetInsertionPointNode(AnalyzerResult analyzerResult, CancellationToken cancellationToken);
        protected abstract Task<TriviaResult> PreserveTriviaAsync(SelectionResult selectionResult, CancellationToken cancellationToken);
        protected abstract Task<SemanticDocument> ExpandAsync(SelectionResult selection, CancellationToken cancellationToken);

        protected abstract Task<GeneratedCode> GenerateCodeAsync(InsertionPoint insertionPoint, SelectionResult selectionResult, AnalyzerResult analyzeResult, CodeGenerationOptions options, CancellationToken cancellationToken);

        protected abstract SyntaxToken GetMethodNameAtInvocation(IEnumerable<SyntaxNodeOrToken> methodNames);
        protected abstract ImmutableArray<AbstractFormattingRule> GetCustomFormattingRules(Document document);

        protected abstract OperationStatus CheckType(SemanticModel semanticModel, SyntaxNode contextNode, Location location, ITypeSymbol type);

        protected abstract Task<(Document document, SyntaxToken methodName)> InsertNewLineBeforeLocalFunctionIfNecessaryAsync(Document document, SyntaxToken methodName, SyntaxNode methodDefinition, CancellationToken cancellationToken);

        public async Task<ExtractMethodResult> ExtractMethodAsync(CancellationToken cancellationToken)
        {
            var operationStatus = OriginalSelectionResult.Status;

            var originalSemanticDocument = OriginalSelectionResult.SemanticDocument;
            var analyzeResult = Analyze(OriginalSelectionResult, LocalFunction, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            operationStatus = CheckVariableTypes(analyzeResult.Status.With(operationStatus), analyzeResult, cancellationToken);
            if (operationStatus.Failed())
                return new FailedExtractMethodResult(operationStatus);

            var insertionPointNode = GetInsertionPointNode(analyzeResult, cancellationToken);

            if (!CanAddTo(originalSemanticDocument.Document, insertionPointNode, out var canAddStatus))
                return new FailedExtractMethodResult(canAddStatus);

            var insertionPoint = await InsertionPoint.CreateAsync(originalSemanticDocument, insertionPointNode, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var analyzedDocument = await analyzeResult.CreateAnnotatedDocumentAsync(
                insertionPoint.SemanticDocument, cancellationToken).ConfigureAwait(false);

            var triviaResult = await PreserveTriviaAsync(OriginalSelectionResult.With(analyzedDocument), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var expandedDocument = await ExpandAsync(OriginalSelectionResult.With(triviaResult.SemanticDocument), cancellationToken).ConfigureAwait(false);

            var generatedCode = await GenerateCodeAsync(
                insertionPoint.With(expandedDocument),
                OriginalSelectionResult.With(expandedDocument),
                analyzeResult,
                Options.CodeGenerationOptions,
                cancellationToken).ConfigureAwait(false);

            var afterTriviaRestored = await triviaResult.ApplyAsync(generatedCode, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            var documentWithoutFinalFormatting = afterTriviaRestored.Document;

            cancellationToken.ThrowIfCancellationRequested();
            return await CreateExtractMethodResultAsync(
                operationStatus.With(generatedCode.Status),
                await SemanticDocument.CreateAsync(documentWithoutFinalFormatting, cancellationToken).ConfigureAwait(false),
                GetFormattingRules(documentWithoutFinalFormatting),
                generatedCode.MethodNameAnnotation,
                generatedCode.MethodDefinitionAnnotation,
                cancellationToken).ConfigureAwait(false);

            bool CanAddTo(Document document, SyntaxNode insertionPointNode, out OperationStatus status)
            {
                var syntaxKinds = document.GetLanguageService<ISyntaxKindsService>();
                var codeGenService = document.GetLanguageService<ICodeGenerationService>();

                if (insertionPointNode is null)
                {
                    status = OperationStatus.NoValidLocationToInsertMethodCall;
                    return false;
                }

                var destination = insertionPointNode;
                if (!LocalFunction)
                {
                    var mappedPoint = insertionPointNode.RawKind == syntaxKinds.GlobalStatement
                        ? insertionPointNode.Parent
                        : insertionPointNode;
                    destination = mappedPoint.Parent ?? mappedPoint;
                }

                if (!codeGenService.CanAddTo(destination, document, cancellationToken))
                {
                    status = OperationStatus.OverlapsHiddenPosition;
                    return false;
                }

                status = OperationStatus.Succeeded;
                return true;
            }
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

            if (LocalFunction && status.Succeeded())
            {
                var methodDefinition = newRoot.GetAnnotatedNodesAndTokens(methodAnnotation).FirstOrDefault().AsNode();
                var result = await InsertNewLineBeforeLocalFunctionIfNecessaryAsync(semanticDocumentWithoutFinalFormatting.Document, methodName, methodDefinition, cancellationToken).ConfigureAwait(false);
                return new SimpleExtractMethodResult(status, result.document, formattingRules, result.methodName);
            }

            return new SimpleExtractMethodResult(status, semanticDocumentWithoutFinalFormatting.Document, formattingRules, methodName);
        }

        private OperationStatus CheckVariableTypes(
            OperationStatus status,
            AnalyzerResult analyzeResult,
            CancellationToken cancellationToken)
        {
            var semanticModel = OriginalSelectionResult.SemanticDocument.SemanticModel;

            // sync selection result to same semantic data as analyzeResult
            var firstToken = OriginalSelectionResult.GetFirstTokenInSelection();
            var context = firstToken.Parent;

            var result = TryCheckVariableType(semanticModel, context, analyzeResult.GetVariablesToMoveIntoMethodDefinition(cancellationToken), status);
            if (!result.Item1)
            {
                result = TryCheckVariableType(semanticModel, context, analyzeResult.GetVariablesToSplitOrMoveIntoMethodDefinition(cancellationToken), result.Item2);
                if (!result.Item1)
                {
                    result = TryCheckVariableType(semanticModel, context, analyzeResult.MethodParameters, result.Item2);
                    if (!result.Item1)
                    {
                        result = TryCheckVariableType(semanticModel, context, analyzeResult.GetVariablesToMoveOutToCallSite(cancellationToken), result.Item2);
                        if (!result.Item1)
                        {
                            result = TryCheckVariableType(semanticModel, context, analyzeResult.GetVariablesToSplitOrMoveOutToCallSite(cancellationToken), result.Item2);
                            if (!result.Item1)
                            {
                                return result.Item2;
                            }
                        }
                    }
                }
            }

            status = result.Item2;

            var checkedStatus = CheckType(semanticModel, context, context.GetLocation(), analyzeResult.ReturnType);
            return checkedStatus.With(status);
        }

        private Tuple<bool, OperationStatus> TryCheckVariableType(
            SemanticModel semanticModel,
            SyntaxNode contextNode,
            IEnumerable<VariableInfo> variables,
            OperationStatus status)
        {
            if (status.Failed())
                return Tuple.Create(false, status);

            var location = contextNode.GetLocation();

            foreach (var variable in variables)
            {
                var originalType = variable.GetVariableType();
                var result = CheckType(semanticModel, contextNode, location, originalType);
                if (result.Failed())
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

            return char.IsUpper(name[0])
                ? prefix + name
                : prefix + char.ToUpper(name[0]).ToString() + name[1..];
        }
    }
}
