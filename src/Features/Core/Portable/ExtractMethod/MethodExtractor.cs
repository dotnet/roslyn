// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod;

internal abstract partial class MethodExtractor<
    TSelectionResult,
    TStatementSyntax,
    TExpressionSyntax>(
        TSelectionResult selectionResult,
        ExtractMethodGenerationOptions options,
        bool localFunction)
    where TSelectionResult : SelectionResult<TStatementSyntax>
    where TStatementSyntax : SyntaxNode
    where TExpressionSyntax : SyntaxNode
{
    protected readonly TSelectionResult OriginalSelectionResult = selectionResult;
    protected readonly ExtractMethodGenerationOptions Options = options;
    protected readonly bool LocalFunction = localFunction;

    protected abstract SyntaxNode ParseTypeName(string name);
    protected abstract AnalyzerResult Analyze(TSelectionResult selectionResult, bool localFunction, CancellationToken cancellationToken);
    protected abstract SyntaxNode GetInsertionPointNode(AnalyzerResult analyzerResult, CancellationToken cancellationToken);
    protected abstract Task<TriviaResult> PreserveTriviaAsync(TSelectionResult selectionResult, CancellationToken cancellationToken);

    protected abstract CodeGenerator CreateCodeGenerator(AnalyzerResult analyzerResult);
    protected abstract Task<GeneratedCode> GenerateCodeAsync(
        InsertionPoint insertionPoint, TSelectionResult selectionResult, AnalyzerResult analyzeResult, ExtractMethodGenerationOptions options, CancellationToken cancellationToken);

    protected abstract SyntaxToken? GetInvocationNameToken(IEnumerable<SyntaxToken> tokens);
    protected abstract AbstractFormattingRule GetCustomFormattingRule(Document document);

    protected abstract Task<(Document document, SyntaxToken? invocationNameToken)> InsertNewLineBeforeLocalFunctionIfNecessaryAsync(
        Document document, SyntaxToken? invocationNameToken, SyntaxNode methodDefinition, CancellationToken cancellationToken);

    public ExtractMethodResult ExtractMethod(OperationStatus initialStatus, CancellationToken cancellationToken)
    {
        var originalSemanticDocument = OriginalSelectionResult.SemanticDocument;
        var analyzeResult = Analyze(OriginalSelectionResult, LocalFunction, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var status = CheckVariableTypes(analyzeResult.Status.With(initialStatus), analyzeResult, cancellationToken);
        if (status.Failed)
            return ExtractMethodResult.Fail(status);

        var insertionPointNode = GetInsertionPointNode(analyzeResult, cancellationToken);

        if (!CanAddTo(originalSemanticDocument.Document, insertionPointNode, out var canAddStatus))
            return ExtractMethodResult.Fail(canAddStatus);

        cancellationToken.ThrowIfCancellationRequested();
        var codeGenerator = this.CreateCodeGenerator(analyzeResult);

        var statements = codeGenerator.GetNewMethodStatements(insertionPointNode, cancellationToken);
        if (statements.Status.Failed)
            return ExtractMethodResult.Fail(statements.Status);

        return ExtractMethodResult.Success(
            status,
            async cancellationToken =>
            {
                var (analyzedDocument, insertionPoint) = await GetAnnotatedDocumentAndInsertionPointAsync(
                    originalSemanticDocument, analyzeResult, insertionPointNode, cancellationToken).ConfigureAwait(false);

                var triviaResult = await PreserveTriviaAsync((TSelectionResult)OriginalSelectionResult.With(analyzedDocument), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                var generatedCode = await GenerateCodeAsync(
                    insertionPoint.With(triviaResult.SemanticDocument),
                    (TSelectionResult)OriginalSelectionResult.With(triviaResult.SemanticDocument),
                    analyzeResult,
                    Options,
                    cancellationToken).ConfigureAwait(false);

                var afterTriviaRestored = await triviaResult.ApplyAsync(generatedCode, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();

                var documentWithoutFinalFormatting = afterTriviaRestored.Document;

                cancellationToken.ThrowIfCancellationRequested();

                var newRoot = afterTriviaRestored.Root;
                var invocationNameToken = GetInvocationNameToken(newRoot.GetAnnotatedTokens(generatedCode.MethodNameAnnotation));

                // Do some final patchups of whitespace when inserting a local function.
                if (LocalFunction)
                {
                    var methodDefinition = newRoot.GetAnnotatedNodesAndTokens(generatedCode.MethodDefinitionAnnotation).FirstOrDefault().AsNode();
                    (documentWithoutFinalFormatting, invocationNameToken) = await InsertNewLineBeforeLocalFunctionIfNecessaryAsync(
                        documentWithoutFinalFormatting, invocationNameToken, methodDefinition, cancellationToken).ConfigureAwait(false);
                }

                return await GetFormattedDocumentAsync(
                    documentWithoutFinalFormatting, invocationNameToken, cancellationToken).ConfigureAwait(false);
            });

        bool CanAddTo(Document document, SyntaxNode insertionPointNode, out OperationStatus status)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var syntaxKinds = syntaxFacts.SyntaxKinds;
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

            if (!codeGenService.CanAddTo(destination, document.Project.Solution, cancellationToken))
            {
                status = OperationStatus.OverlapsHiddenPosition;
                return false;
            }

            status = OperationStatus.SucceededStatus;
            return true;
        }
    }

    private async Task<(Document document, SyntaxToken? invocationNameToken)> GetFormattedDocumentAsync(
        Document document,
        SyntaxToken? invocationNameToken,
        CancellationToken cancellationToken)
    {
        var annotation = new SyntaxAnnotation();

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (invocationNameToken != null)
            root = root.ReplaceToken(invocationNameToken.Value, invocationNameToken.Value.WithAdditionalAnnotations(annotation));

        var annotatedDocument = document.WithSyntaxRoot(root);
        var simplifiedDocument = await Simplifier.ReduceAsync(annotatedDocument, Simplifier.Annotation, this.Options.CodeCleanupOptions.SimplifierOptions, cancellationToken).ConfigureAwait(false);
        var simplifiedRoot = await simplifiedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var services = document.Project.Solution.Services;

        var formattingRules = GetFormattingRules(document);
        var formattedDocument = simplifiedDocument.WithSyntaxRoot(
            Formatter.Format(simplifiedRoot, Formatter.Annotation, services, this.Options.CodeCleanupOptions.FormattingOptions, formattingRules, cancellationToken));

        var formattedRoot = await formattedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var finalInvocationNameToken = formattedRoot.GetAnnotatedTokens(annotation).SingleOrDefault();
        return (formattedDocument, finalInvocationNameToken == default ? null : finalInvocationNameToken);
    }

    private static async Task<(SemanticDocument analyzedDocument, InsertionPoint insertionPoint)> GetAnnotatedDocumentAndInsertionPointAsync(
        SemanticDocument document,
        AnalyzerResult analyzeResult,
        SyntaxNode insertionPointNode,
        CancellationToken cancellationToken)
    {
        var annotations = new List<(SyntaxToken, SyntaxAnnotation)>(analyzeResult.Variables.Length);
        foreach (var variable in analyzeResult.Variables)
            variable.AddIdentifierTokenAnnotationPair(annotations, cancellationToken);

        var tokenMap = annotations.GroupBy(p => p.Item1, p => p.Item2).ToDictionary(g => g.Key, g => g.ToArray());

        var insertionPointAnnotation = new SyntaxAnnotation();

        var finalRoot = document.Root.ReplaceSyntax(
            nodes: [insertionPointNode],
            // intentionally using 'n' (new) here.  We want to see any updated sub tokens that were updated in computeReplacementToken
            computeReplacementNode: (o, n) => n.WithAdditionalAnnotations(insertionPointAnnotation),
            tokens: tokenMap.Keys,
            computeReplacementToken: (o, n) => o.WithAdditionalAnnotations(tokenMap[o]),
            trivia: null,
            computeReplacementTrivia: null);

        var finalDocument = await document.WithSyntaxRootAsync(finalRoot, cancellationToken).ConfigureAwait(false);
        var insertionPoint = new InsertionPoint(finalDocument, insertionPointAnnotation);

        return (finalDocument, insertionPoint);
    }

    private ImmutableArray<AbstractFormattingRule> GetFormattingRules(Document document)
        => [GetCustomFormattingRule(document), .. Formatter.GetDefaultFormattingRules(document)];

    private OperationStatus CheckVariableTypes(
        OperationStatus status,
        AnalyzerResult analyzeResult,
        CancellationToken cancellationToken)
    {
        var semanticModel = OriginalSelectionResult.SemanticDocument.SemanticModel;

        // sync selection result to same semantic data as analyzeResult
        var firstToken = OriginalSelectionResult.GetFirstTokenInSelection();
        var context = firstToken.Parent;

        status = TryCheckVariableType(semanticModel, context, analyzeResult.GetVariablesToMoveIntoMethodDefinition(cancellationToken), status);
        status = TryCheckVariableType(semanticModel, context, analyzeResult.GetVariablesToSplitOrMoveIntoMethodDefinition(cancellationToken), status);
        status = TryCheckVariableType(semanticModel, context, analyzeResult.MethodParameters, status);
        status = TryCheckVariableType(semanticModel, context, analyzeResult.GetVariablesToMoveOutToCallSite(cancellationToken), status);
        status = TryCheckVariableType(semanticModel, context, analyzeResult.GetVariablesToSplitOrMoveOutToCallSite(cancellationToken), status);

        if (status.Failed)
            return status;

        var checkedStatus = CheckType(semanticModel, context, analyzeResult.ReturnType);
        return checkedStatus.With(status);
    }

    private OperationStatus TryCheckVariableType(
        SemanticModel semanticModel,
        SyntaxNode contextNode,
        IEnumerable<VariableInfo> variables,
        OperationStatus status)
    {
        if (status.Succeeded)
        {
            foreach (var variable in variables)
            {
                var originalType = variable.GetVariableType();
                var result = CheckType(semanticModel, contextNode, originalType);
                if (result.Failed)
                    return status.With(result);
            }
        }

        return status;
    }

    private OperationStatus CheckType(
        SemanticModel semanticModel, SyntaxNode contextNode, ITypeSymbol type)
    {
        Contract.ThrowIfNull(type);

        // this happens when there is no return type
        if (type.SpecialType == SpecialType.System_Void)
            return OperationStatus.SucceededStatus;

        if (type.TypeKind is TypeKind.Error or TypeKind.Unknown)
            return OperationStatus.ErrorOrUnknownType;

        // if it is type parameter, make sure we are getting same type parameter
        foreach (var typeParameter in TypeParameterCollector.Collect(type))
        {
            var typeName = ParseTypeName(typeParameter.Name);
            var currentType = semanticModel.GetSpeculativeTypeInfo(contextNode.SpanStart, typeName, SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
            if (currentType == null || !SymbolEqualityComparer.Default.Equals(currentType, semanticModel.ResolveType(typeParameter)))
            {
                return new OperationStatus(succeeded: true,
                    string.Format(FeaturesResources.Type_parameter_0_is_hidden_by_another_type_parameter_1,
                        typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        currentType == null ? string.Empty : currentType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            }
        }

        return OperationStatus.SucceededStatus;
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
